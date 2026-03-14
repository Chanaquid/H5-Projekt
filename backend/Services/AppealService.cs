using backend.DTOs;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class AppealService : IAppealService
    {
        private readonly IAppealRepository _appealRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFineRepository _fineRepository;
        private readonly INotificationService _notificationService;

        //Score restored on appeal approval — reset to just above the hard block threshold
        private const int DefaultRestoredScore = 20;

        public AppealService(
                  IAppealRepository appealRepository,
                  IUserRepository userRepository,
                  IFineRepository fineRepository,
                  INotificationService notificationService)
        {
            _appealRepository = appealRepository;
            _userRepository = userRepository;
            _fineRepository = fineRepository;
            _notificationService = notificationService;
        }

        //Create score appeal
        public async Task<AppealDTO.AppealResponseDTO> CreateScoreAppealAsync(string userId, AppealDTO.CreateScoreAppealDTO dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            if (user.Score >= 20)
                throw new InvalidOperationException("Your score is 20 or above. You do not need a score appeal.");

            var existing = await _appealRepository.GetPendingByUserIdAsync(userId);
            if (existing != null)
                throw new InvalidOperationException("You already have a pending appeal.");

            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new ArgumentException("Appeal message cannot be empty.");

            var appeal = new Appeal
            {
                UserId = userId,
                AppealType = AppealType.Score,
                Message = dto.Message.Trim(),
                Status = AppealStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _appealRepository.AddAsync(appeal);
            await _appealRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                userId,
                NotificationType.AppealSubmitted,
                "Your score appeal has been submitted and is under review.",
                appeal.Id,
                NotificationReferenceType.Appeal
            );

            var created = await _appealRepository.GetByIdWithDetailsAsync(appeal.Id);
            return MapToAppealDTO(created!);
        }

        //Create fine appeal
        public async Task<AppealDTO.AppealResponseDTO> CreateFineAppealAsync(string userId, AppealDTO.CreateFineAppealDTO dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new ArgumentException("Appeal message cannot be empty.");

            var fine = await _fineRepository.GetByIdWithDetailsAsync(dto.FineId);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found.");

            if (fine.UserId != userId)
                throw new UnauthorizedAccessException("You can only appeal your own fines.");

            if (fine.Status == FineStatus.Paid || fine.Status == FineStatus.Waived)
                throw new InvalidOperationException("This fine is already closed and cannot be appealed.");

            //Only one pending fine appeal per fine at a time
            var existingFineAppeal = await _appealRepository.GetPendingFineAppealByFineIdAsync(dto.FineId);
            if (existingFineAppeal != null)
                throw new InvalidOperationException("This fine already has a pending appeal.");

            var appeal = new Appeal
            {
                UserId = userId,
                AppealType = AppealType.Fine,
                FineId = dto.FineId,
                Message = dto.Message.Trim(),
                Status = AppealStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _appealRepository.AddAsync(appeal);
            await _appealRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                userId,
                NotificationType.AppealSubmitted,
                "Your fine appeal has been submitted and is under review.",
                appeal.Id,
                NotificationReferenceType.Appeal
            );

            var created = await _appealRepository.GetByIdWithDetailsAsync(appeal.Id);
            return MapToAppealDTO(created!);
        }

        //Get my appeals
        public async Task<List<AppealDTO.AppealResponseDTO>> GetMyAppealsAsync(string userId)
        {
            var appeals = await _appealRepository.GetAllByUserIdAsync(userId);
            return appeals.Select(MapToAppealDTO).ToList();
        }

        //Admin gets all pending
        public async Task<List<AppealDTO.AppealResponseDTO>> GetAllPendingAsync()
        {
            var appeals = await _appealRepository.GetAllPendingAsync();
            return appeals.Select(MapToAppealDTO).ToList();
        }

        //Admin decides score appeal
        public async Task<AppealDTO.AppealResponseDTO> DecideScoreAppealAsync(int appealId, string adminId, AppealDTO.AdminScoreAppealDecisionDTO dto)
        {
            var appeal = await _appealRepository.GetByIdWithDetailsAsync(appealId);
            if (appeal == null)
                throw new KeyNotFoundException($"Appeal {appealId} not found.");

            if (appeal.AppealType != AppealType.Score)
                throw new InvalidOperationException("This is not a score appeal.");

            if (appeal.Status != AppealStatus.Pending)
                throw new InvalidOperationException("This appeal has already been resolved.");

            if (!dto.IsApproved && string.IsNullOrWhiteSpace(dto.AdminNote))
                throw new ArgumentException("A reason is required when rejecting an appeal.");

            appeal.Status = dto.IsApproved ? AppealStatus.Approved : AppealStatus.Rejected;
            appeal.AdminNote = dto.AdminNote?.Trim();
            appeal.ResolvedByAdminId = adminId;
            appeal.ResolvedAt = DateTime.UtcNow;

            if (dto.IsApproved)
            {
                var targetScore = dto.NewScore ?? DefaultRestoredScore;

                if (targetScore < 1 || targetScore > 100)
                    throw new ArgumentException("Score must be between 1 and 100.");

                var user = await _userRepository.GetByIdAsync(appeal.UserId);
                if (user != null)
                {
                    var pointsChanged = targetScore - user.Score;

                    await _userRepository.AddScoreHistoryAsync(new ScoreHistory
                    {
                        UserId = user.Id,
                        PointsChanged = pointsChanged,
                        ScoreAfterChange = targetScore,
                        Reason = ScoreChangeReason.AdminAdjustment,
                        Note = $"Score appeal approved. Score set to {targetScore}.",
                        CreatedAt = DateTime.UtcNow
                    });

                    user.Score = targetScore;
                    await _userRepository.UpdateAsync(user);
                }

                appeal.RestoredScore = targetScore;
            }

            _appealRepository.Update(appeal);
            await _appealRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                appeal.UserId,
                dto.IsApproved ? NotificationType.AppealApproved : NotificationType.AppealRejected,
                dto.IsApproved
                    ? $"Your score appeal has been approved. Your score has been set to {appeal.RestoredScore}."
                    : $"Your score appeal has been rejected. Reason: {dto.AdminNote}",
                appeal.Id,
                NotificationReferenceType.Appeal
            );

            return MapToAppealDTO(appeal);
        }

        //Admin decides fine appeal
        public async Task<AppealDTO.AppealResponseDTO> DecideFineAppealAsync(int appealId, string adminId, AppealDTO.AdminFineAppealDecisionDTO dto)
        {
            var appeal = await _appealRepository.GetByIdWithDetailsAsync(appealId);
            if (appeal == null)
                throw new KeyNotFoundException($"Appeal {appealId} not found.");

            if (appeal.AppealType != AppealType.Fine)
                throw new InvalidOperationException("This is not a fine appeal.");

            if (appeal.Status != AppealStatus.Pending)
                throw new InvalidOperationException("This appeal has already been resolved.");

            if (!dto.IsApproved && string.IsNullOrWhiteSpace(dto.AdminNote))
                throw new ArgumentException("A reason is required when rejecting an appeal.");

            if (dto.IsApproved && dto.Resolution == null)
                throw new ArgumentException("A resolution is required when approving a fine appeal.");

            if (dto.IsApproved && dto.Resolution == FineAppealResolution.Custom && (dto.CustomFineAmount == null || dto.CustomFineAmount < 0))
                throw new ArgumentException("A valid custom fine amount is required.");

            appeal.Status = dto.IsApproved ? AppealStatus.Approved : AppealStatus.Rejected;
            appeal.AdminNote = dto.AdminNote?.Trim();
            appeal.ResolvedByAdminId = adminId;
            appeal.ResolvedAt = DateTime.UtcNow;
            appeal.FineResolution = dto.Resolution;
            appeal.CustomFineAmount = dto.CustomFineAmount;

            if (dto.IsApproved && appeal.FineId.HasValue)
            {
                var fine = await _fineRepository.GetByIdWithDetailsAsync(appeal.FineId.Value);
                if (fine != null)
                {
                    var originalAmount = fine.Amount;

                    var newAmount = dto.Resolution switch
                    {
                        FineAppealResolution.Waive => 0m,
                        FineAppealResolution.HalfDamage => fine.Loan?.Item != null ? Math.Round(fine.Loan.Item.CurrentValue * 0.5m, 2) : fine.Amount,
                        FineAppealResolution.FullLost => fine.Loan?.Item != null ? Math.Round(fine.Loan.Item.CurrentValue, 2) : fine.Amount,
                        FineAppealResolution.Custom => Math.Round(dto.CustomFineAmount!.Value, 2),
                        _ => fine.Amount
                    };

                    //Adjust user's unpaid total — diff between original and new amount
                    var user = await _userRepository.GetByIdAsync(fine.UserId);
                    if (user != null)
                    {
                        user.UnpaidFinesTotal = Math.Max(0, user.UnpaidFinesTotal - originalAmount + newAmount);
                        await _userRepository.UpdateAsync(user);
                    }

                    fine.Amount = newAmount;

                    //Waived = fully closed; anything else stays Unpaid — user pays the new amount
                    fine.Status = dto.Resolution == FineAppealResolution.Waive
                        ? FineStatus.Waived
                        : FineStatus.Unpaid;

                    if (fine.Status == FineStatus.Waived)
                        fine.VerifiedAt = DateTime.UtcNow;

                    _fineRepository.Update(fine);
                }
            }

            _appealRepository.Update(appeal);
            await _appealRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                appeal.UserId,
                dto.IsApproved ? NotificationType.AppealApproved : NotificationType.AppealRejected,
                dto.IsApproved
                    ? $"Your fine appeal has been approved. Resolution: {dto.Resolution}."
                    : $"Your fine appeal has been rejected. Reason: {dto.AdminNote}",
                appeal.Id,
                NotificationReferenceType.Appeal
            );

            return MapToAppealDTO(appeal);
        }

        //Mapper
        private static AppealDTO.AppealResponseDTO MapToAppealDTO(Appeal a)
        {
            return new AppealDTO.AppealResponseDTO
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User?.FullName ?? string.Empty,
                UserScore = a.User?.Score ?? 0,
                AppealType = a.AppealType.ToString(),
                FineId = a.FineId,
                FineAmount = a.Fine?.Amount,
                Message = a.Message,
                Status = a.Status.ToString(),
                AdminNote = a.AdminNote,
                RestoredScore = a.RestoredScore,
                FineResolution = a.FineResolution?.ToString(),
                CustomFineAmount = a.CustomFineAmount,
                ResolvedByAdminName = a.ResolvedByAdmin?.FullName,
                CreatedAt = a.CreatedAt,
                ResolvedAt = a.ResolvedAt
            };
        }
    }
}