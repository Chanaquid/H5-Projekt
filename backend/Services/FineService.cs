using backend.DTOs;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class FineService : IFineService
    {
        private readonly IFineRepository _fineRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly INotificationService _notificationService;

        //Fine amounts — defined as constants so they're easy to find and change
        private const decimal LateFineAmount = 100m; //Direct 100kr per late return
        private const decimal DamagedFineRatio = 0.5m; //50% of item's current value
        private const decimal LostFineRatio = 1.0m; //100% of items current value

        public FineService(
            IFineRepository fineRepository,
            IUserRepository userRepository,
            ILoanRepository loanRepository,
            INotificationService notificationService)
        {
            _fineRepository = fineRepository;
            _userRepository = userRepository;
            _loanRepository = loanRepository;
            _notificationService = notificationService;
        }

        //Get fines by userid
        public async Task<List<FineDTO.FineResponseDTO>> GetUserFinesAsync(string userId)
        {
            var fines = await _fineRepository.GetByUserIdAsync(userId);
            return fines.Select(MapToFineDTO).ToList();
        }

        //User submits payment proof
        public async Task<FineDTO.FineResponseDTO> MarkAsPaidAsync(string userId, FineDTO.PayFineDTO dto)
        {
            var fine = await _fineRepository.GetByIdWithDetailsAsync(dto.FineId);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found.");

            if (fine.UserId != userId)
                throw new UnauthorizedAccessException("You can only pay your own fines.");

            if (fine.Status == FineStatus.Paid)
                throw new InvalidOperationException("This fine has already been paid.");

            if (fine.Status == FineStatus.PendingVerification)
                throw new InvalidOperationException("Your payment proof is already under review.");

            if (string.IsNullOrWhiteSpace(dto.PaymentProofImageUrl))
                throw new ArgumentException("A payment proof image is required.");

            if (string.IsNullOrWhiteSpace(dto.PaymentDescription))
                throw new ArgumentException("A payment description is required.");

            fine.PaymentProofImageUrl = dto.PaymentProofImageUrl.Trim();
            fine.PaymentDescription = dto.PaymentDescription.Trim();
            fine.Status = FineStatus.PendingVerification;
            fine.PaidAt = DateTime.UtcNow;
            fine.RejectionReason = null; //Clear any previous rejection

            _fineRepository.Update(fine);
            await _fineRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                fine.UserId,
                NotificationType.FineIssued,
                fine.Loan != null
                    ? $"Your payment proof for the fine on '{fine.Loan.Item.Title}' has been submitted and is under review."
                    : "Your payment proof has been submitted and is under review.",
                fine.Id,
                NotificationReferenceType.Fine
            );

            return MapToFineDTO(fine);
        }

        //Get all unpaid fines
        public async Task<List<FineDTO.FineResponseDTO>> GetAllUnpaidAsync()
        {
            var fines = await _fineRepository.GetAllUnpaidAsync();
            return fines.Select(MapToFineDTO).ToList();
        }

        public async Task<List<FineDTO.FineResponseDTO>> GetByDisputeIdAsync(int disputeId)
        {
            var fines = await _fineRepository.GetByDisputeIdAsync(disputeId);
            return fines.Select(MapToFineDTO).ToList();
        }

        //Get all pending verification fines
        public async Task<List<FineDTO.FineResponseDTO>> GetPendingVerificationAsync()
        {
            var fines = await _fineRepository.GetPendingVerificationAsync();
            return fines.Select(MapToFineDTO).ToList();
        }

        //CUSTOM FINE BY ADMIN - no dispute link
        public async Task<FineDTO.FineResponseDTO> AdminIssueFineAsync(FineDTO.AdminIssueFineDTO dto)
        {
            if (dto.Amount <= 0)
                throw new ArgumentException("Fine amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("A reason is required for admin-issued fines.");

            string itemTitle = "Custom Fine";
            decimal itemValue = 0;

            if (dto.LoanId.HasValue)
            {
                var loan = await _loanRepository.GetByIdWithDetailsAsync(dto.LoanId.Value);
                if (loan == null)
                    throw new KeyNotFoundException($"Loan {dto.LoanId} not found.");

                if (loan.BorrowerId != dto.UserId)
                    throw new ArgumentException("The specified user is not the borrower of this loan.");

                itemTitle = loan.Item.Title;
                itemValue = loan.Item.CurrentValue;
            }

            var fine = new Fine
            {
                LoanId = dto.LoanId,
                UserId = dto.UserId,
                Type = FineType.Custom, //Custom admin fine maps to Damaged type
                Amount = Math.Round(dto.Amount, 2),
                ItemValueAtTimeOfFine = itemValue,
                AdminNote = dto.Reason.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _fineRepository.AddAsync(fine);
            await UpdateUnpaidTotalAsync(dto.UserId, fine.Amount);
            await _fineRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                    dto.UserId,
                    NotificationType.FineIssued,
                    dto.LoanId.HasValue
                        ? $"A fine of {fine.Amount} kr has been issued for '{itemTitle}'. Reason: {dto.Reason}"
                        : $"A fine of {fine.Amount} kr has been issued. Reason: {dto.Reason}",
                    fine.Id,
                    NotificationReferenceType.Fine
                );


            return MapToFineDTO(fine);
        }

        //Admin approves or rejects payment proof
        public async Task<FineDTO.FineResponseDTO> AdminConfirmPaymentAsync(string adminId, FineDTO.AdminFineVerificationDTO dto)
        {
            var fine = await _fineRepository.GetByIdWithDetailsAsync(dto.FineId);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found.");

            if (fine.Status != FineStatus.PendingVerification)
                throw new InvalidOperationException("This fine is not pending verification.");

            if (!dto.IsApproved && string.IsNullOrWhiteSpace(dto.RejectionReason))
                throw new ArgumentException("A rejection reason is required.");

            if (dto.IsApproved)
            {
                fine.Status = FineStatus.Paid;
                fine.VerifiedAt = DateTime.UtcNow;

                //Deduct from user's unpaid total
                var user = await _userRepository.GetByIdAsync(fine.UserId);
                if (user != null)
                {
                    user.UnpaidFinesTotal = Math.Max(0, user.UnpaidFinesTotal - fine.Amount);
                    await _userRepository.UpdateAsync(user);
                }

                await _notificationService.SendAsync(
                    fine.UserId,
                    NotificationType.FinePaid,
                    fine.Loan != null
                        ? $"Your payment for the fine on '{fine.Loan.Item.Title}' has been confirmed. The fine is now closed."
                        : "Your payment has been confirmed. The fine is now closed.",
                    fine.Id,
                    NotificationReferenceType.Fine
                );
            }
            else
            {
                fine.Status = FineStatus.Rejected;
                fine.RejectionReason = dto.RejectionReason!.Trim();

                await _notificationService.SendAsync(
                    fine.UserId,
                    NotificationType.FineIssued,
                    fine.Loan != null
                        ? $"Your payment proof for the fine on '{fine.Loan.Item.Title}' was rejected. Reason: {dto.RejectionReason}. Please resubmit."
                        : $"Your payment proof was rejected. Reason: {dto.RejectionReason}. Please resubmit.",
                    fine.Id,
                    NotificationReferenceType.Fine
                );
            }

            _fineRepository.Update(fine);
            await _fineRepository.SaveChangesAsync();

            return MapToFineDTO(fine);
        }

        //Issue late return fine
        public async Task<FineDTO.FineResponseDTO> IssueLateReturnFineAsync(int loanId)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {loanId} not found.");

            var fine = new Fine
            {
                LoanId = loanId,
                UserId = loan.BorrowerId,
                Type = FineType.Late,
                Amount = LateFineAmount,
                ItemValueAtTimeOfFine = loan.Item.CurrentValue,
                CreatedAt = DateTime.UtcNow
            };

            await _fineRepository.AddAsync(fine);
            await UpdateUnpaidTotalAsync(loan.BorrowerId, LateFineAmount);
            await _fineRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                loan.BorrowerId,
                NotificationType.FineIssued,
                $"A late return fine of {LateFineAmount} kr has been issued for '{loan.Item.Title}'.",
                fine.Id,
                NotificationReferenceType.Fine
            );

            return MapToFineDTO(fine);
        }

        //Issue damaged fine to borrower
        public async Task<FineDTO.FineResponseDTO> IssueDamagedFineAsync(int loanId, int? disputeId = null)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {loanId} not found.");

            var amount = Math.Round(loan.Item.CurrentValue * DamagedFineRatio, 2);

            var fine = new Fine
            {
                LoanId = loanId,
                UserId = loan.BorrowerId,
                Type = FineType.Damaged,
                Amount = amount,
                ItemValueAtTimeOfFine = loan.Item.CurrentValue,
                DisputeId = disputeId,
                CreatedAt = DateTime.UtcNow
            };

            await _fineRepository.AddAsync(fine);
            await UpdateUnpaidTotalAsync(loan.BorrowerId, amount);
            await _fineRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                loan.BorrowerId,
                NotificationType.FineIssued,
                $"A damage fine of {amount} kr has been issued for '{loan.Item.Title}'.",
                fine.Id,
                NotificationReferenceType.Fine
            );

            return MapToFineDTO(fine);
        }

        //Issue lost fine to borrower
        public async Task<FineDTO.FineResponseDTO> IssueLostFineAsync(int loanId, int? disputeId = null)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {loanId} not found.");

            var amount = Math.Round(loan.Item.CurrentValue * LostFineRatio, 2);

            var fine = new Fine
            {
                LoanId = loanId,
                UserId = loan.BorrowerId,
                Type = FineType.Lost,
                Amount = amount,
                ItemValueAtTimeOfFine = loan.Item.CurrentValue,
                DisputeId = disputeId,
                CreatedAt = DateTime.UtcNow
            };

            await _fineRepository.AddAsync(fine);
            await UpdateUnpaidTotalAsync(loan.BorrowerId, amount);
            await _fineRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                loan.BorrowerId,
                NotificationType.FineIssued,
                $"A lost item fine of {amount} kr has been issued for '{loan.Item.Title}'.",
                fine.Id,
                NotificationReferenceType.Fine
            );

            return MapToFineDTO(fine);
        }

        //Issue custom fine — for admin via dispute verdict
        public async Task<FineDTO.FineResponseDTO?> IssueCustomFineAsync(int loanId, int disputeId, string userId, decimal? amount = null, int? scoreAdjustment = null)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {loanId} not found.");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User {userId} not found.");

            Fine? fine = null;

            if (amount.HasValue && amount > 0)
            {
                fine = new Fine
                {
                    LoanId = loanId,
                    UserId = userId,
                    Type = FineType.Custom,
                    Amount = Math.Round(amount.Value, 2),
                    ItemValueAtTimeOfFine = loan.Item.CurrentValue,
                    DisputeId = disputeId,
                    CreatedAt = DateTime.UtcNow
                };

                await _fineRepository.AddAsync(fine);
                await UpdateUnpaidTotalAsync(userId, amount.Value);
            }

            if (scoreAdjustment.HasValue && scoreAdjustment != 0)
            {               
                var newScore = Math.Clamp(user.Score + scoreAdjustment.Value, 0, 100);
                var actualChange = newScore - user.Score;

                user.Score = newScore;

                await _userRepository.AddScoreHistoryAsync(new ScoreHistory
                {
                    UserId = userId,
                    PointsChanged = actualChange,
                    ScoreAfterChange = newScore,
                    Reason = ScoreChangeReason.AdminAdjustment,
                    LoanId = loanId,
                    Note = $"Dispute #{disputeId} verdict: score adjusted by {scoreAdjustment.Value} points.",
                    CreatedAt = DateTime.UtcNow
                });

                await _userRepository.SaveChangesAsync();


            }

            // Notifications
            if (amount.HasValue && amount > 0 && scoreAdjustment.HasValue && scoreAdjustment != 0)
            {
                var scoreText = scoreAdjustment.Value < 0
                    ? $"reduced by {Math.Abs(scoreAdjustment.Value)} points"
                    : $"increased by {scoreAdjustment.Value} points";

                await _notificationService.SendAsync(
                    userId,
                    NotificationType.FineIssued,
                    $"A fine of {amount.Value} kr has been issued and your score has been {scoreText} for '{loan.Item.Title}' (Dispute #{disputeId}).",
                    fine!.Id,
                    NotificationReferenceType.Fine
                );
            }
            else if (amount.HasValue && amount > 0)
            {
                await _notificationService.SendAsync(
                    userId,
                    NotificationType.FineIssued,
                    $"A fine of {amount.Value} kr has been issued for '{loan.Item.Title}'.",
                    fine!.Id,
                    NotificationReferenceType.Fine
                );
            }
            else if (scoreAdjustment.HasValue && scoreAdjustment != 0)
            {
                var scoreText = scoreAdjustment.Value < 0
                    ? $"reduced by {Math.Abs(scoreAdjustment.Value)} points"
                    : $"increased by {scoreAdjustment.Value} points";

                await _notificationService.SendAsync(
                    userId,
                    NotificationType.FineIssued,
                    $"Your score has been {scoreText} due to dispute #{disputeId}.",
                    disputeId,
                    NotificationReferenceType.Dispute
                );
            }

            await _fineRepository.SaveChangesAsync();

            return fine != null ? MapToFineDTO(fine) : null;
        }

        //Admin update fine
        public async Task<FineDTO.FineResponseDTO> AdminUpdateFineAsync(int fineId, FineDTO.AdminUpdateFineDTO dto)
        {
            var fine = await _fineRepository.GetByIdWithDetailsAsync(fineId);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found.");

            if (dto.Amount.HasValue)
            {
                if (dto.Amount.Value <= 0)
                    throw new ArgumentException("Fine amount must be greater than zero.");

                //Adjust unpaid total for non-paid fines
                if (fine.Status == FineStatus.Unpaid || fine.Status == FineStatus.Rejected || fine.Status == FineStatus.PendingVerification)
                {
                    var user = await _userRepository.GetByIdAsync(fine.UserId);
                    if (user != null)
                    {
                        user.UnpaidFinesTotal = Math.Max(0, user.UnpaidFinesTotal - fine.Amount + dto.Amount.Value);
                        await _userRepository.UpdateAsync(user);
                    }
                }

                fine.Amount = Math.Round(dto.Amount.Value, 2);
            }

            if (!string.IsNullOrWhiteSpace(dto.Reason))
                fine.AdminNote = dto.Reason.Trim();

            if (dto.Status.HasValue)
            {
                var wasUnpaid = fine.Status != FineStatus.Paid;
                var isNowPaid = dto.Status.Value == FineStatus.Paid;

                if (wasUnpaid && isNowPaid)
                {
                    //Admin manually marking as paid — deduct from unpaid total
                    var user = await _userRepository.GetByIdAsync(fine.UserId);
                    if (user != null)
                    {
                        user.UnpaidFinesTotal = Math.Max(0, user.UnpaidFinesTotal - fine.Amount);
                        await _userRepository.UpdateAsync(user);
                    }
                    fine.VerifiedAt = DateTime.UtcNow;
                }
                else if (!wasUnpaid && !isNowPaid)
                {
                    //Admin un-paying a fine — add back to unpaid total
                    var user = await _userRepository.GetByIdAsync(fine.UserId);
                    if (user != null)
                    {
                        user.UnpaidFinesTotal += fine.Amount;
                        await _userRepository.UpdateAsync(user);
                    }
                    fine.VerifiedAt = null;
                }

                fine.Status = dto.Status.Value;
            }

            _fineRepository.Update(fine);
            await _fineRepository.SaveChangesAsync();

            return MapToFineDTO(fine);
        }

        //Helpers
        private async Task UpdateUnpaidTotalAsync(string userId, decimal amount)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return;

            user.UnpaidFinesTotal += amount;
            await _userRepository.UpdateAsync(user);
            //Caller is responsible for SaveChangesAsync
        }

        //Mapper
        private static FineDTO.FineResponseDTO MapToFineDTO(Fine f)
        {
            return new FineDTO.FineResponseDTO
            {
                Id = f.Id,
                LoanId = f.LoanId,
                ItemTitle = f.Loan?.Item?.Title ?? string.Empty,
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
            };
        }
    }
}