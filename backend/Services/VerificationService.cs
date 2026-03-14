using backend.DTOs;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class VerificationService : IVerificationService
    {

        private readonly IVerificationRepository _verificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;

        public VerificationService(
            IVerificationRepository verificationRepository,
            IUserRepository userRepository,
            INotificationService notificationService)
        {
            _verificationRepository = verificationRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
        }


        //submit verification req
        public async Task<VerificationDTO.VerificationRequestResponseDTO> SubmitRequestAsync(string userId, VerificationDTO.CreateVerificationRequestDTO dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            //Already verified — no need to submit again
            if (user.IsVerified)
                throw new InvalidOperationException("Your account is already verified.");

            //Only one pending request at a time
            var existing = await _verificationRepository.GetPendingByUserIdAsync(userId);
            if (existing != null)
                throw new InvalidOperationException("You already have a pending verification request. Please wait for it to be reviewed.");

            if (string.IsNullOrWhiteSpace(dto.DocumentUrl))
                throw new ArgumentException("Document URL is required.");

            if (!Enum.TryParse<VerificationDocumentType>(dto.DocumentType, out var documentType))
                throw new ArgumentException("Invalid document type. Use 'Passport', 'NationalId', or 'DrivingLicense'.");

            var request = new VerificationRequest
            {
                UserId = userId,
                DocumentUrl = dto.DocumentUrl.Trim(),
                DocumentType = documentType,
                Status = VerificationStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            await _verificationRepository.AddAsync(request);
            await _verificationRepository.SaveChangesAsync();

            var created = await _verificationRepository.GetByIdWithDetailsAsync(request.Id);
            return MapToDTO(created!);
        }

        //Get user req
        public async Task<VerificationDTO.VerificationRequestResponseDTO> GetUserRequestAsync(string userId)
        {
            //Return latest request regardless of status so user can see if they were rejected
            var request = await _verificationRepository.GetLatestByUserIdAsync(userId);
            if (request == null)
                throw new KeyNotFoundException("No verification request found.");

            //Load user if not already included
            var detailed = await _verificationRepository.GetByIdWithDetailsAsync(request.Id);
            return MapToDTO(detailed!);
        }

        //Get all pending - admin
        public async Task<List<VerificationDTO.VerificationRequestResponseDTO>> GetAllPendingAsync()
        {
            var requests = await _verificationRepository.GetAllPendingAsync();
            return requests.Select(MapToDTO).ToList();
        }


        //Admin decides
        public async Task<VerificationDTO.VerificationRequestResponseDTO> DecideAsync(int requestId, string adminId, VerificationDTO.AdminVerificationDecisionDTO dto)
        {
            var request = await _verificationRepository.GetByIdWithDetailsAsync(requestId);
            if (request == null)
                throw new KeyNotFoundException($"Verification request {requestId} not found.");

            if (request.Status != VerificationStatus.Pending)
                throw new InvalidOperationException("This verification request has already been reviewed.");

            if (!dto.IsApproved && string.IsNullOrWhiteSpace(dto.AdminNote))
                throw new ArgumentException("A reason is required when rejecting a verification request.");

            request.Status = dto.IsApproved ? VerificationStatus.Approved : VerificationStatus.Rejected;
            request.AdminNote = dto.AdminNote?.Trim();
            request.ReviewedByAdminId = adminId;
            request.ReviewedAt = DateTime.UtcNow;

            if (dto.IsApproved)
            {
                //Mark the user as verified
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                    throw new KeyNotFoundException("User not found.");

                user.IsVerified = true;
                await _userRepository.UpdateAsync(user);
            }

            _verificationRepository.Update(request);
            await _verificationRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                request.UserId,
                dto.IsApproved ? NotificationType.VerificationApproved : NotificationType.VerificationRejected,
                dto.IsApproved
                    ? "Your identity has been verified. You can now borrow items that require verification."
                    : $"Your verification request was rejected. Reason: {dto.AdminNote}",
                request.Id,
                NotificationReferenceType.Verification 
            );

            return MapToDTO(request);
        }

        //Mapper
        private static VerificationDTO.VerificationRequestResponseDTO MapToDTO(VerificationRequest v)
        {
            return new VerificationDTO.VerificationRequestResponseDTO
            {
                Id = v.Id,
                UserId = v.UserId,
                UserName = v.User?.FullName ?? string.Empty,
                UserEmail = v.User?.Email ?? string.Empty,
                DocumentUrl = v.DocumentUrl,
                DocumentType = v.DocumentType.ToString(),
                Status = v.Status.ToString(),
                AdminNote = v.AdminNote,
                ReviewedByAdminName = v.ReviewedByAdmin?.FullName,
                SubmittedAt = v.SubmittedAt,
                ReviewedAt = v.ReviewedAt
            };
        }



    }
}
