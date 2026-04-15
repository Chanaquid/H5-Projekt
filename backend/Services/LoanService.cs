using backend.DTOs;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFineService _fineService;
        private readonly INotificationService _notificationService;
        private readonly IUserFavoriteRepository _favoriteRepository;

        //Score change amounts — mirrors business rules
        public const int OnTimeReturnScore = 5; //+5 on time
        private const int LatePenaltyPerDay = -5; //-5 per day late
        private const int MaxLatePenalty = -25; //Cap at -25 per item

        public LoanService(
            ILoanRepository loanRepository,
            IItemRepository itemRepository,
            IUserRepository userRepository,
            IFineService fineService,
            INotificationService notificationService,
            IUserFavoriteRepository favoriteRepository)
        {
            _loanRepository = loanRepository;
            _itemRepository = itemRepository;
            _userRepository = userRepository;
            _fineService = fineService;
            _notificationService = notificationService;
            _favoriteRepository = favoriteRepository;
        }

        //Craate loan
        public async Task<LoanDTO.LoanDetailDTO> CreateAsync(string borrowerId, LoanDTO.CreateLoanDTO dto)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(dto.ItemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {dto.ItemId} not found.");

            if (item.Status != ItemStatus.Approved || !item.IsActive)
                throw new InvalidOperationException("This item is not available for borrowing.");

            if (item.OwnerId == borrowerId)
                throw new ArgumentException("You cannot borrow your own item.");

            //Date validations
            if (dto.StartDate.Date < DateTime.UtcNow.Date)
                throw new ArgumentException("Start date cannot be in the past.");

            // Start/End ordering
            if (dto.StartDate.Date >= dto.EndDate.Date)
                throw new ArgumentException("End date must be after start date.");

            //EndDate is capped till item's AvailableUntil
            var endDate = dto.EndDate > item.AvailableUntil ? item.AvailableUntil : dto.EndDate;

            if (dto.StartDate.Date < item.AvailableFrom.Date || dto.StartDate.Date > item.AvailableUntil.Date)
                throw new ArgumentException("Start date must fall within the item's availability window.");

            //Mininum loan days check
            if (item.MinLoanDays.HasValue)
            {
                var requestedDays = (endDate - dto.StartDate).Days;
                if (requestedDays < item.MinLoanDays.Value)
                    throw new ArgumentException($"This item requires a minimum loan period of {item.MinLoanDays.Value} days.");
            }

            //Verification check
            var borrower = await _userRepository.GetByIdAsync(borrowerId);
            if (borrower == null)
                throw new KeyNotFoundException("Borrower not found.");

            if (item.RequiresVerification && !borrower.IsVerified)
                throw new InvalidOperationException("This item requires a verified account. Please submit your verification documents.");

            //Score-based routing
            if (borrower.Score < 20)
                throw new InvalidOperationException("Your score is too low to borrow items. Please file an appeal to restore your borrowing privileges.");

            var status = borrower.Score >= 50
                ? LoanStatus.Pending //Goes straight to owner
                : LoanStatus.AdminPending; //Score 20-49 — admin reviews first and then its send to owner

            //Check item isn't already on an active loan or pending
            var hasActiveLoans = item.Loans?.Any(l =>
                l.Status == LoanStatus.Pending ||
                l.Status == LoanStatus.AdminPending ||
                l.Status == LoanStatus.Approved ||
                l.Status == LoanStatus.Active) ?? false;

            if (hasActiveLoans)
                throw new InvalidOperationException("This item already has a pending or active loan request.");

            var loan = new Loan
            {
                ItemId = dto.ItemId,
                BorrowerId = borrowerId,
                StartDate = dto.StartDate,
                EndDate = endDate,
                Status = status,
                SnapshotCondition = item.Condition,
                CreatedAt = DateTime.UtcNow,
                //Snapshot item photos at loan creation — frozen for dispute resolution
                SnapshotPhotos = item.Photos?.Select(p => new LoanSnapshotPhoto
                {
                    PhotoUrl = p.PhotoUrl,
                    DisplayOrder = p.DisplayOrder,
                    SnapshotTakenAt = DateTime.UtcNow
                }).ToList() ?? new List<LoanSnapshotPhoto>()
            };

            await _loanRepository.AddAsync(loan);
            await _loanRepository.SaveChangesAsync();

            //Notify the right party depending on score-based routing
            if (status == LoanStatus.Pending)
            {
                await _notificationService.SendAsync(
                    item.OwnerId,
                    NotificationType.LoanRequested,
                    $"{borrower.FullName} has requested to borrow '{item.Title}'.",
                    loan.Id,
                    NotificationReferenceType.Loan
                );
            }
            //AdminPending — no notification to owner yet since admin reviews first
            var created = await _loanRepository.GetByIdWithDetailsAsync(loan.Id);
            return MapToDetailDTO(created!, borrowerId);
        }

        //Cancel loan
        public async Task<LoanDTO.LoanDetailDTO> CancelAsync(int loanId, string borrowerId, LoanDTO.CancelLoanDTO dto)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {loanId} not found.");

            if (loan.BorrowerId != borrowerId)
                throw new UnauthorizedAccessException("You can only cancel your own loans.");

            //Loan can only be cancelled in these status
            var cancellableStatuses = new[] { LoanStatus.Pending, LoanStatus.AdminPending, LoanStatus.Approved };
            if (!cancellableStatuses.Contains(loan.Status))
                throw new InvalidOperationException("This loan cannot be cancelled at its current status.");

            loan.Status = LoanStatus.Cancelled;
            loan.DecisionNote = dto.Reason?.Trim();
            loan.UpdatedAt = DateTime.UtcNow;

            _loanRepository.Update(loan);
            await _loanRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                loan.Item.OwnerId,
                NotificationType.LoanCancelled,
                $"The loan request for '{loan.Item.Title}' has been cancelled by the borrower.",
                loan.Id,
                NotificationReferenceType.Loan
            );

            return MapToDetailDTO(loan, borrowerId);
        }

        //request loan extension
        public async Task<LoanDTO.LoanDetailDTO> RequestExtensionAsync(int loanId, string borrowerId, LoanDTO.RequestExtensionDTO dto)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {loanId} not found.");

            if (loan.BorrowerId != borrowerId)
                throw new UnauthorizedAccessException("You can only request extensions for your own loans.");

            if (loan.Status != LoanStatus.Active)
                throw new InvalidOperationException("Extensions can only be requested on active loans.");

            if (loan.ExtensionRequestStatus == ExtensionStatus.Pending)
                throw new InvalidOperationException("You already have a pending extension request.");

            if (dto.RequestedExtensionDate <= loan.EndDate)
                throw new ArgumentException("Extension date must be after the current end date.");

            if (dto.RequestedExtensionDate > loan.Item.AvailableUntil)
                throw new ArgumentException("Extension date cannot exceed the item's availability window.");

            loan.RequestedExtensionDate = dto.RequestedExtensionDate;
            loan.ExtensionRequestStatus = ExtensionStatus.Pending;
            loan.UpdatedAt = DateTime.UtcNow;

            _loanRepository.Update(loan);
            await _loanRepository.SaveChangesAsync();

            //send notification to owner
            await _notificationService.SendAsync(
                loan.Item.OwnerId,
                NotificationType.LoanRequested,
                $"The borrower has requested an extension for '{loan.Item.Title}' until {dto.RequestedExtensionDate:dd MMM yyyy}.",
                loan.Id,
                NotificationReferenceType.Loan
            );

            return MapToDetailDTO(loan, borrowerId);
        }

        //Get borrowed loans
        public async Task<List<LoanDTO.LoanSummaryDTO>> GetBorrowedLoansAsync(string borrowerId)
        {
            var loans = await _loanRepository.GetByBorrowerIdAsync(borrowerId);
            return loans.Select(l => MapToSummaryDTO(l, borrowerId)).ToList();
        }


        //Owner decides the loan request
        public async Task<LoanDTO.LoanDetailDTO> DecideAsync(int loanId, string ownerId, LoanDTO.LoanDecisionDTO dto)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {loanId} not found.");

            if (loan.Item.OwnerId != ownerId)
                throw new UnauthorizedAccessException("You can only decide on loans for your own items.");

            if (loan.Status != LoanStatus.Pending)
                throw new InvalidOperationException("Only pending loans can be approved or rejected.");

            loan.Status = dto.IsApproved ? LoanStatus.Approved : LoanStatus.Rejected;
            loan.DecisionNote = dto.DecisionNote?.Trim();
            loan.UpdatedAt = DateTime.UtcNow;

            _loanRepository.Update(loan);
            await _loanRepository.SaveChangesAsync();

            //send notificaiton to borrower
            await _notificationService.SendAsync(
                loan.BorrowerId,
                dto.IsApproved ? NotificationType.LoanApproved : NotificationType.LoanRejected,
                dto.IsApproved
                    ? $"Your loan request for '{loan.Item.Title}' has been approved. You can pick it up from {loan.StartDate:dd MMM yyyy}."
                    : $"Your loan request for '{loan.Item.Title}' was declined. {dto.DecisionNote}",
                loan.Id,
                NotificationReferenceType.Loan
            );

            return MapToDetailDTO(loan, ownerId);
        }


        //Owner decides borrowers extension request
        public async Task<LoanDTO.LoanDetailDTO> DecideExtensionAsync(int loanId, string ownerId, LoanDTO.ExtensionDecisionDTO dto)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {loanId} not found.");

            if (loan.Item.OwnerId != ownerId)
                throw new UnauthorizedAccessException("You can only decide extensions for your own items.");

            if (loan.ExtensionRequestStatus != ExtensionStatus.Pending)
                throw new InvalidOperationException("No pending extension request found.");

            loan.ExtensionRequestStatus = dto.IsApproved ? ExtensionStatus.Approved : ExtensionStatus.Rejected;

            if (dto.IsApproved && loan.RequestedExtensionDate.HasValue)
                loan.EndDate = loan.RequestedExtensionDate.Value;

            loan.UpdatedAt = DateTime.UtcNow;

            _loanRepository.Update(loan);
            await _loanRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                loan.BorrowerId,
                dto.IsApproved ? NotificationType.LoanApproved : NotificationType.LoanRejected,
                dto.IsApproved
                    ? $"Your extension request for '{loan.Item.Title}' has been approved. New return date: {loan.EndDate:dd MMM yyyy}."
                    : $"Your extension request for '{loan.Item.Title}' was declined.",
                loan.Id,
                NotificationReferenceType.Loan
            );

            return MapToDetailDTO(loan, ownerId);
        }

        //Get all owned loans
        public async Task<List<LoanDTO.LoanSummaryDTO>> GetOwnedLoansAsync(string ownerId)
        {
            var loans = await _loanRepository.GetByOwnerIdAsync(ownerId);
            return loans.Select(l => MapToSummaryDTO(l, ownerId)).ToList();
        }

        public async Task<List<LoanDTO.LoanSummaryDTO>> GetLoanHistoryByItemIdAsync(int itemId, string requestingUserId, bool isAdmin = false)
        {
            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} not found.");

            if (!isAdmin && item.OwnerId != requestingUserId)
                throw new UnauthorizedAccessException("You do not have access to this item's loan history.");

            var loans = await _loanRepository.GetLoanHistoryByItemIdAsync(itemId);
            return loans.Select(l => MapToSummaryDTO(l, requestingUserId)).ToList();
        }

        //Get loan by id
        public async Task<LoanDTO.LoanDetailDTO> GetByIdAsync(int loanId, string requestingUserId, bool isAdmin = false)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {loanId} not found.");

            //Only the owner or borrower can view a loan
            if (!isAdmin && loan.BorrowerId != requestingUserId && loan.Item.OwnerId != requestingUserId)
                throw new UnauthorizedAccessException("You do not have access to this loan.");

            return MapToDetailDTO(loan, requestingUserId);
        }

        //get pending admin approvals
        public async Task<List<LoanDTO.AdminPendingLoanDTO>> GetPendingApprovalsAsync()
        {
            var loans = await _loanRepository.GetPendingAdminApprovalsAsync();
            return loans.Select(l => new LoanDTO.AdminPendingLoanDTO
            {
                Id = l.Id,
                ItemTitle = l.Item.Title,
                OwnerName = l.Item.Owner.FullName,
                BorrowerName = l.Borrower.FullName,
                BorrowerEmail = l.Borrower.Email!,
                BorrowerScore = l.Borrower.Score,
                BorrowerUnpaidFines = l.Borrower.UnpaidFinesTotal,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                CreatedAt = l.CreatedAt
            }).ToList();
        }

        //admin decides on adminpending loan
        public async Task<LoanDTO.LoanDetailDTO> AdminDecideAsync(int loanId, string adminId, LoanDTO.LoanDecisionDTO dto)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {loanId} not found.");

            if (loan.Status != LoanStatus.AdminPending)
                throw new InvalidOperationException("Only admin-pending loans can be decided here.");

            if (dto.IsApproved)
            {
                //Admin approves — now routes to owner for final decision
                loan.Status = LoanStatus.Pending;

                await _notificationService.SendAsync(
                    loan.Item.OwnerId,
                    NotificationType.LoanRequested,
                    $"{loan.Borrower.FullName} has requested to borrow '{loan.Item.Title}'.",
                    loan.Id,
                    NotificationReferenceType.Loan
                );
            }
            else
            {
                loan.Status = LoanStatus.Rejected;

                await _notificationService.SendAsync(
                    loan.BorrowerId,
                    NotificationType.LoanRejected,
                    $"Your loan request for '{loan.Item.Title}' was rejected by admin. {dto.DecisionNote}",
                    loan.Id,
                    NotificationReferenceType.Loan
                );
            }

            loan.DecisionNote = dto.DecisionNote?.Trim();
            loan.UpdatedAt = DateTime.UtcNow;

            _loanRepository.Update(loan);
            await _loanRepository.SaveChangesAsync();

            return MapToDetailDTO(loan, adminId);
        }

        //get all loans - admin request
        public async Task<List<LoanDTO.LoanSummaryDTO>> GetAllLoansAsync()
        {
            var loans = await _loanRepository.GetAllAsync();
            return loans.Select(l => MapToSummaryDTO(l, null)).ToList();
        }

        public async Task HandleLoanReturnAsync(Loan loan)
        {
            var borrower = loan.Borrower;
            var item = loan.Item;

            loan.Status = LoanStatus.Returned;
            loan.ActualReturnDate = DateTime.UtcNow;
            loan.UpdatedAt = DateTime.UtcNow;
            _loanRepository.Update(loan);

            var returnedOnTime = DateTime.UtcNow.Date <= loan.EndDate.Date;

            if (returnedOnTime && borrower != null)
            {
                var newScore = Math.Min(borrower.Score + OnTimeReturnScore, 100);
                var actualPointsAdded = newScore - borrower.Score;

                if (actualPointsAdded > 0)
                {
                    await _userRepository.AddScoreHistoryAsync(new ScoreHistory
                    {
                        UserId = borrower.Id,
                        PointsChanged = actualPointsAdded,
                        ScoreAfterChange = newScore,
                        Reason = ScoreChangeReason.OnTimeReturn,
                        LoanId = loan.Id,
                        Note = $"On-time return of '{item.Title}'.",
                        CreatedAt = DateTime.UtcNow
                    });

                    borrower.Score = newScore;
                    await _userRepository.UpdateAsync(borrower);

                    await _notificationService.SendAsync(
                        borrower.Id,
                        NotificationType.LoanReturned,
                        $"Your return of '{item.Title}' has been confirmed. +{actualPointsAdded} points for returning on time!",
                        loan.Id,
                        NotificationReferenceType.Loan
                    );
                }
                else
                {
                    //Score already at 100 — just notify, no scorehistory
                    await _notificationService.SendAsync(
                        borrower.Id,
                        NotificationType.LoanReturned,
                        $"Your return of '{item.Title}' has been confirmed. Your score is already at its maximum!",
                        loan.Id,
                        NotificationReferenceType.Loan
                    );
                }
            }
            else if (borrower != null)
            {
                await _notificationService.SendAsync(
                    borrower.Id,
                    NotificationType.LoanReturned,
                    $"Your return of '{item.Title}' has been confirmed.",
                    loan.Id,
                    NotificationReferenceType.Loan
                );
            }

            await _notificationService.SendAsync(
                item.OwnerId,
                NotificationType.LoanReturned,
                $"'{item.Title}' has been returned by the borrower.",
                loan.Id,
                NotificationReferenceType.Loan
            );

            //Notify users who favorited this item
            var usersToNotify = await _favoriteRepository.GetUsersToNotifyAsync(item.Id);
            foreach (var userId in usersToNotify)
            {
                //Don't notify the borrower who just returned it or the owner
                if (userId == borrower?.Id || userId == item.OwnerId)
                    continue;

                await _notificationService.SendAsync(
                    userId,
                    NotificationType.ItemAvailable,
                    $"'{item.Title}' is now available to borrow!",
                    item.Id,
                    NotificationReferenceType.Item
                );
            }

            await _loanRepository.SaveChangesAsync();
        }


        //process late loans - background job
        public async Task ProcessLateLoansAsync()
        {
            var overdueLoans = await _loanRepository.GetActiveAndOverdueAsync();

            foreach (var loan in overdueLoans)
            {
                try
                {
                    var daysLate = (DateTime.UtcNow.Date - loan.EndDate.Date).Days;
                    if (daysLate <= 0) continue;

                    var borrower = loan.Borrower;
                    if (borrower == null) continue;

                    // --- 1. Score penalty ---
                    var totalPointsTaken = borrower.ScoreHistory
                         .Where(s => s.Reason == ScoreChangeReason.LateReturn && s.LoanId == loan.Id)
                         .Sum(s => s.PointsChanged); // negative numbers

                    var remainingPenaltyBudget = MaxLatePenalty - totalPointsTaken; // negative minus negative = negative

                    if (remainingPenaltyBudget < 0) // still room to penalize
                    {
                        var potentialPenalty = LatePenaltyPerDay * daysLate; // negative
                        var todayPenalty = Math.Max(potentialPenalty, remainingPenaltyBudget); // cap at max penalty
                        todayPenalty = Math.Max(todayPenalty, -borrower.Score); // ensure score >= 0

                        if (todayPenalty < 0)
                        {
                            var newScore = borrower.Score + todayPenalty;

                            await _userRepository.AddScoreHistoryAsync(new ScoreHistory
                            {
                                UserId = borrower.Id,
                                PointsChanged = todayPenalty,
                                ScoreAfterChange = newScore,
                                Reason = ScoreChangeReason.LateReturn,
                                LoanId = loan.Id,
                                Note = $"Loan {loan.Id} overdue {daysLate} day(s). Applied {todayPenalty} points penalty.",
                                CreatedAt = DateTime.UtcNow
                            });

                            borrower.Score = newScore;
                            await _userRepository.UpdateAsync(borrower);
                        }
                    }

                    // --- 2. Late fine ---
                    var alreadyFined = loan.Fines?.Any(f => f.Type == FineType.Late) ?? false;
                    if (!alreadyFined)
                        await _fineService.IssueLateReturnFineAsync(loan.Id);

                    // --- 3. Mark loan as Late ---
                    if (loan.Status == LoanStatus.Active)
                    {
                        loan.Status = LoanStatus.Late;
                        loan.UpdatedAt = DateTime.UtcNow;
                        _loanRepository.Update(loan);
                    }

                    // --- 4. Notify borrower ---
                    if (remainingPenaltyBudget < 0 || !alreadyFined)
                    {
                        await _notificationService.SendAsync(
                            loan.BorrowerId,
                            NotificationType.Overdue,
                            $"Your loan for '{loan.Item.Title}' is {daysLate} day(s) overdue.",
                            loan.Id,
                            NotificationReferenceType.Loan
                        );
                    }
                }
                catch (Exception ex)
                {
                    //_logger.LogError(ex, "Error processing late loan {LoanId}", loan.Id);
                }
            }

            await _loanRepository.SaveChangesAsync();
        }

        private static LoanDTO.LoanDetailDTO MapToDetailDTO(Loan l, string? requestingUserId)
        {
            var daysOverdue = l.Status == LoanStatus.Late || (l.Status == LoanStatus.Active && l.EndDate < DateTime.UtcNow)
                ? (int?)(DateTime.UtcNow.Date - l.EndDate.Date).Days
                : null;

            return new LoanDTO.LoanDetailDTO
            {
                Id = l.Id,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                ActualReturnDate = l.ActualReturnDate,
                Status = l.Status.ToString(),
                SnapshotCondition = l.SnapshotCondition.ToString(),
                DecisionNote = l.DecisionNote,
                RequestedExtensionDate = l.RequestedExtensionDate,
                ExtensionRequestStatus = l.ExtensionRequestStatus?.ToString(),
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
                Item = new ItemDTO.ItemSummaryDTO
                {
                    Id = l.Item.Id,
                    Title = l.Item.Title,
                    Description = l.Item.Description,
                    Condition = l.Item.Condition.ToString(),
                    PickupAddress = l.Item.PickupAddress,
                    PickupLatitude = l.Item.PickupLatitude,
                    PickupLongitude = l.Item.PickupLongitude,
                    AvailableFrom = l.Item.AvailableFrom,
                    AvailableUntil = l.Item.AvailableUntil,
                    PrimaryPhotoUrl = l.SnapshotPhotos?.OrderBy(p => p.DisplayOrder).FirstOrDefault()?.PhotoUrl,
                    OwnerId = l.Item.OwnerId,
                    OwnerName = l.Item.Owner?.FullName ?? string.Empty,
                    OwnerUsername = l.Item.Owner?.UserName ?? string.Empty,
                    OwnerAvatarUrl = l.Item.Owner?.AvatarUrl ?? string.Empty
                },
                Owner = new UserDTO.UserSummaryDTO
                {
                    Id = l.Item.Owner.Id,
                    FullName = l.Item.Owner.FullName,
                    Username = l.Item.Owner.UserName ?? string.Empty,
                    Score = l.Item.Owner.Score,
                    IsVerified = l.Item.Owner.IsVerified,
                    AvatarUrl = l.Item.Owner.AvatarUrl
                },
                Borrower = new UserDTO.UserSummaryDTO
                {
                    Id = l.Borrower.Id,
                    FullName = l.Borrower.FullName,
                    Username = l.Borrower.UserName ?? string.Empty,
                    Score = l.Borrower.Score,
                    IsVerified = l.Borrower.IsVerified,
                    AvatarUrl = l.Borrower.AvatarUrl
                },
                SnapshotPhotos = l.SnapshotPhotos?.OrderBy(p => p.DisplayOrder).Select(p => new LoanDTO.LoanSnapshotPhotoDTO
                {
                    Id = p.Id,
                    PhotoUrl = p.PhotoUrl,
                    DisplayOrder = p.DisplayOrder
                }).ToList() ?? new(),
                Fines = l.Fines?.Select(f => new FineDTO.FineResponseDTO
                {
                    Id = f.Id,
                    LoanId = f.LoanId,
                    ItemTitle = l.Item.Title,
                    Type = f.Type.ToString(),
                    Status = f.Status.ToString(),
                    Amount = f.Amount,
                    ItemValueAtTimeOfFine = f.ItemValueAtTimeOfFine,
                    PaymentProofImageUrl = f.PaymentProofImageUrl,
                    PaymentDescription = f.PaymentDescription,
                    RejectionReason = f.RejectionReason,
                    PaidAt = f.PaidAt,
                    VerifiedAt = f.VerifiedAt,
                    DisputeId = f.DisputeId,
                    CreatedAt = f.CreatedAt
                }).ToList() ?? new(),
                HasOpenDispute = l.Disputes?.Any(d => d.Status != DisputeStatus.Resolved) ?? false,
                DaysOverdue = daysOverdue,
                HasUnreadMessages = false
            };
        }

        private static LoanDTO.LoanSummaryDTO MapToSummaryDTO(Loan l, string? requestingUserId)
        {
            var daysOverdue = l.Status == LoanStatus.Late || (l.Status == LoanStatus.Active && l.EndDate < DateTime.UtcNow)
                ? (int?)(DateTime.UtcNow.Date - l.EndDate.Date).Days
                : null;

            var isViewingAsBorrower = requestingUserId == l.BorrowerId;

            return new LoanDTO.LoanSummaryDTO
            {
                Id = l.Id,
                ItemTitle = l.Item?.Title ?? string.Empty,
                ItemPrimaryPhoto = l.Item?.Photos?.FirstOrDefault(p => p.IsPrimary)?.PhotoUrl ?? l.SnapshotPhotos?.OrderBy(p => p.DisplayOrder).FirstOrDefault()?.PhotoUrl,
                OtherPartyName = isViewingAsBorrower
                    ? l.Item?.Owner?.FullName ?? string.Empty
                    : l.Borrower?.FullName ?? string.Empty,
                OtherPartyUsername = isViewingAsBorrower
                    ? l.Item?.Owner?.UserName ?? string.Empty
                    : l.Borrower?.UserName ?? string.Empty,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                ActualReturnDate = l.ActualReturnDate,
                Status = l.Status.ToString(),
                HasUnreadMessages = false,  //Wired up when MessageService is built
                DaysOverdue = daysOverdue
            };
        }

    }
}