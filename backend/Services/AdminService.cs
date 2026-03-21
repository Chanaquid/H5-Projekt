using backend.DTOs;
using backend.Interfaces;

namespace backend.Services
{
    public class AdminService : IAdminService
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly IFineRepository _fineRepository;
        private readonly IDisputeRepository _disputeRepository;
        private readonly IAppealRepository _appealRepository;
        private readonly IVerificationRepository _verificationRepository;
        private readonly IUserRepository _userRepository;

        public AdminService(
            IItemRepository itemRepository,
            ILoanRepository loanRepository,
            IFineRepository fineRepository,
            IDisputeRepository disputeRepository,
            IAppealRepository appealRepository,
            IVerificationRepository verificationRepository,
            IUserRepository userRepository)
        {
            _itemRepository = itemRepository;
            _loanRepository = loanRepository;
            _fineRepository = fineRepository;
            _disputeRepository = disputeRepository;
            _appealRepository = appealRepository;
            _verificationRepository = verificationRepository;
            _userRepository = userRepository;
        }

        //Dashboard
        public async Task<AdminDTO.AdminDashboardDTO> GetDashboardAsync()
        {
            var pendingItems = await _itemRepository.GetPendingApprovalsAsync();
            var pendingLoans = await _loanRepository.GetPendingAdminApprovalsAsync();
            var openDisputes = await _disputeRepository.GetAllOpenAsync();
            var pendingAppeals = await _appealRepository.GetAllPendingAsync();
            var pendingUserVerifications = await _verificationRepository.GetAllPendingAsync();
            var pendingPaymentVerifications = await _fineRepository.GetPendingVerificationAsync();
            var allUsers = await _userRepository.GetAllAsync();
            var unpaidFines = await _fineRepository.GetAllUnpaidAsync();
            var allItems = await _itemRepository.GetAllApprovedAsync();
            var allLoans = await _loanRepository.GetAllAsync();

            var activeStatuses = new[] { Models.LoanStatus.Approved, Models.LoanStatus.Active };

            return new AdminDTO.AdminDashboardDTO
            {
                PendingItemApprovals = pendingItems.Count,
                PendingLoanApprovals = pendingLoans.Count,
                OpenDisputes = openDisputes.Count,
                PendingAppeals = pendingAppeals.Count,
                PendingUserVerifications = pendingUserVerifications.Count,
                PendingPaymentVerifications = pendingPaymentVerifications.Count,
                TotalUsers = allUsers.Count,
                TotalActiveItems = allItems.Count,
                TotalActiveLoans = allLoans.Count(l => l.Status == Models.LoanStatus.Active),
                TotalUnpaidFines = unpaidFines.Count,
                TotalUnpaidFinesAmount = unpaidFines.Sum(f => f.Amount)
            };
        }
        //Item history
        public async Task<AdminDTO.ItemHistoryDTO> GetItemHistoryAsync(int itemId)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} not found.");

            var loans = await _loanRepository.GetAllAsync();
            var itemLoans = loans
                .Where(l => l.ItemId == itemId)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();

            var loanHistory = new List<AdminDTO.LoanHistoryEntryDTO>();

            foreach (var loan in itemLoans)
            {
                var detailed = await _loanRepository.GetByIdWithDetailsAsync(loan.Id);
                if (detailed == null) continue;

                loanHistory.Add(new AdminDTO.LoanHistoryEntryDTO
                {
                    LoanId = detailed.Id,
                    BorrowerName = detailed.Borrower?.FullName ?? string.Empty,
                    StartDate = detailed.StartDate,
                    EndDate = detailed.EndDate,
                    ActualReturnDate = detailed.ActualReturnDate,
                    Status = detailed.Status.ToString(),
                    SnapshotCondition = detailed.SnapshotCondition.ToString(),
                    SnapshotPhotos = detailed.SnapshotPhotos?.OrderBy(p => p.DisplayOrder).Select(p => new LoanDTO.LoanSnapshotPhotoDTO
                    {
                        Id = p.Id,
                        PhotoUrl = p.PhotoUrl,
                        DisplayOrder = p.DisplayOrder
                    }).ToList() ?? new(),
                    Fines = detailed.Fines?.Select(f => new FineDTO.FineResponseDTO
                    {
                        Id = f.Id,
                        LoanId = f.LoanId,
                        ItemTitle = item.Title,
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
                    Disputes = detailed.Disputes?.Select(d => new DisputeDTO.DisputeSummaryDTO
                    {
                        Id = d.Id,
                        LoanId = d.LoanId,
                        ItemTitle = item.Title,
                        FiledById = d.FiledById ?? string.Empty,
                        FiledByName = d.FiledBy?.FullName ?? string.Empty,
                        FiledByUsername = d.FiledBy?.UserName ?? string.Empty,
                        FiledAs = d.FiledAs.ToString(),
                        Status = d.Status.ToString(),
                        ResponseDeadline = d.ResponseDeadline,
                        CreatedAt = d.CreatedAt
                    }).ToList() ?? new()
                });
            }

            var reviews = item.Reviews?.ToList() ?? new();

            var sortedReviews = reviews
                .OrderByDescending(r => r.IsAdminReview)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();

            return new AdminDTO.ItemHistoryDTO
            {
                ItemId = item.Id,
                ItemTitle = item.Title,
                OwnerName = item.Owner?.FullName ?? string.Empty,
                AverageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0,
                ReviewCount = reviews.Count,
                Reviews = sortedReviews.Select(r => new AdminDTO.ItemReviewEntryDTO
                {
                    Id = r.Id,
                    ReviewerId = r.ReviewerId,
                    ReviewerName = r.IsAdminReview ? "Admin" : (r.Reviewer?.FullName ?? string.Empty),
                    ReviewerAvatarUrl = r.Reviewer?.AvatarUrl,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    IsAdminReview = r.IsAdminReview,
                    CreatedAt = r.CreatedAt
                }).ToList(),
                Loans = loanHistory
            };
        }





    }
}
