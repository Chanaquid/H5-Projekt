using backend.DTOs;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class DisputeService : IDisputeService
    {
        private readonly IDisputeRepository _disputeRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly IFineService _fineService;
        private readonly INotificationService _notificationService;

        private const int ResponseWindowHours = 72; //Dispute will be closed in favor of the initiator if the other party doesnt respond within 72 hours

        public DisputeService(
            IDisputeRepository disputeRepository,
            ILoanRepository loanRepository,
            IFineService fineService,
            INotificationService notificationService)
        {
            _disputeRepository = disputeRepository;
            _loanRepository = loanRepository;
            _fineService = fineService;
            _notificationService = notificationService;
        }


        //Create Dispute
        public async Task<DisputeDTO.DisputeDetailDTO> CreateAsync(string filedById, DisputeDTO.CreateDisputeDTO dto)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(dto.LoanId);
            if (loan == null)
                throw new KeyNotFoundException($"Loan {dto.LoanId} not found.");

            //Only owner or borrower of this loan can file a dispute
            var isOwner = loan.Item.OwnerId == filedById;
            var isBorrower = loan.BorrowerId == filedById;

            if (!isOwner && !isBorrower)
                throw new UnauthorizedAccessException("You are not a party to this loan.");

            //Loan must be Active, Returned, or Late to dispute
            var disputeableStatuses = new[] { LoanStatus.Active, LoanStatus.Returned, LoanStatus.Late };
            if (!disputeableStatuses.Contains(loan.Status))
                throw new InvalidOperationException("A dispute can only be filed on an active, returned, or late loan.");

            //Validate FiledAs matches actual role
            if (!Enum.TryParse<DisputeFiledAs>(dto.FiledAs, out var filedAs))
                throw new ArgumentException("Invalid FiledAs value. Use 'AsOwner' or 'AsBorrower'.");

            if (filedAs == DisputeFiledAs.AsOwner && !isOwner)
                throw new ArgumentException("You are not the owner of this item.");

            if (filedAs == DisputeFiledAs.AsBorrower && !isBorrower)
                throw new ArgumentException("You are not the borrower of this loan.");

            //Block duplicate open disputes on the same loan
            var existingOpen = loan.Disputes?.Any(d => d.Status != DisputeStatus.Resolved) ?? false;
            if (existingOpen)
                throw new InvalidOperationException("There is already an open dispute on this loan.");

            var dispute = new Dispute
            {
                LoanId = dto.LoanId,
                FiledById = filedById,
                FiledAs = filedAs,
                Description = dto.Description.Trim(),
                ResponseDeadline = DateTime.UtcNow.AddHours(ResponseWindowHours),
                Status = DisputeStatus.AwaitingResponse,
                CreatedAt = DateTime.UtcNow
            };

            await _disputeRepository.AddAsync(dispute);
            await _disputeRepository.SaveChangesAsync();

            //Notify the other party that they have 72h to respond
            var otherPartyId = isOwner ? loan.BorrowerId : loan.Item.OwnerId;
            await _notificationService.SendAsync(
                otherPartyId,
                NotificationType.DisputeFiled,
                $"A dispute has been filed on the loan for '{loan.Item.Title}'. You have 72 hours to submit your response.",
                dispute.Id,
                NotificationReferenceType.Dispute
            );

            var created = await _disputeRepository.GetByIdWithDetailsAsync(dispute.Id);
            return MapToDetailDTO(created!);
        }

        //Submit response - only by the other party
        public async Task<DisputeDTO.DisputeDetailDTO> SubmitResponseAsync(int disputeId, string responderId, DisputeDTO.DisputeResponseDTO dto)
        {
            var dispute = await _disputeRepository.GetByIdWithDetailsAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException($"Dispute {disputeId} not found.");

            //Only the other party (not the filer) can submit a response
            var isOwner = dispute.Loan.Item.OwnerId == responderId;
            var isBorrower = dispute.Loan.BorrowerId == responderId;

            if (!isOwner && !isBorrower)
                throw new UnauthorizedAccessException("You are not a party to this dispute.");

            if (dispute.FiledById == responderId)
                throw new InvalidOperationException("You cannot respond to your own dispute.");

            if (dispute.Status == DisputeStatus.Resolved)
                throw new InvalidOperationException("This dispute has already been resolved.");

            if (dispute.Status != DisputeStatus.AwaitingResponse)
                throw new InvalidOperationException("This dispute is no longer awaiting a response.");

            if (DateTime.UtcNow > dispute.ResponseDeadline)
                throw new InvalidOperationException("The 72-hour response window has passed.");

            dispute.ResponseDescription = dto.ResponseDescription.Trim();
            dispute.Status = DisputeStatus.UnderReview;

            _disputeRepository.Update(dispute);
            await _disputeRepository.SaveChangesAsync();

            //Notify admin that both sides have been submitted
            await _notificationService.SendAsync(
                dispute.FiledById,
                NotificationType.DisputeResponse,
                $"The other party has submitted their response on the dispute for '{dispute.Loan.Item.Title}'. An admin will review it shortly.",
                dispute.Id,
                NotificationReferenceType.Dispute
            );

            return MapToDetailDTO(dispute);
        }

        //Get dispute by id
        public async Task<DisputeDTO.DisputeDetailDTO> GetByIdAsync(int disputeId, string requestingUserId)
        {
            var dispute = await _disputeRepository.GetByIdWithDetailsAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException($"Dispute {disputeId} not found.");

            var isOwner = dispute.Loan.Item.OwnerId == requestingUserId;
            var isBorrower = dispute.Loan.BorrowerId == requestingUserId;

            if (!isOwner && !isBorrower)
                throw new UnauthorizedAccessException("You do not have access to this dispute.");

            return MapToDetailDTO(dispute);
        }

        //Get all disputes by user id
        public async Task<List<DisputeDTO.DisputeSummaryDTO>> GetDisputesByUserIdAsync(string userId)
        {
            var disputes = await _disputeRepository.GetByUserIdAsync(userId);
            return disputes.Select(MapToSummaryDTO).ToList();
        }

        //Add pic to the dispute
        public async Task AddPhotoAsync(int disputeId, string submittedById, string photoUrl, string? caption)
        {
            var dispute = await _disputeRepository.GetByIdWithDetailsAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException($"Dispute {disputeId} not found.");

            var isOwner = dispute.Loan.Item.OwnerId == submittedById;
            var isBorrower = dispute.Loan.BorrowerId == submittedById;

            if (!isOwner && !isBorrower)
                throw new UnauthorizedAccessException("You are not a party to this dispute.");

            if (dispute.Status == DisputeStatus.Resolved)
                throw new InvalidOperationException("Cannot add photos to a resolved dispute.");

            var photo = new DisputePhoto
            {
                DisputeId = disputeId,
                SubmittedById = submittedById,
                PhotoUrl = photoUrl.Trim(),
                Caption = caption?.Trim(),
                UploadedAt = DateTime.UtcNow
            };

            await _disputeRepository.AddPhotoAsync(photo);
            await _disputeRepository.SaveChangesAsync();
        }

        //Get all open disputes - admin
        public async Task<List<DisputeDTO.DisputeSummaryDTO>> GetAllOpenAsync()
        {
            var disputes = await _disputeRepository.GetAllOpenAsync();
            return disputes.Select(MapToSummaryDTO).ToList();
        }


        //Admin issues a verdict
        public async Task<DisputeDTO.DisputeDetailDTO> IssueVerdictAsync(int disputeId, string adminId, DisputeDTO.AdminVerdictDTO dto)
        {
            var dispute = await _disputeRepository.GetByIdWithDetailsAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException($"Dispute {disputeId} not found.");

            if (dispute.Status == DisputeStatus.Resolved)
                throw new InvalidOperationException("This dispute has already been resolved.");

            if (!Enum.TryParse<DisputeVerdict>(dto.Verdict, out var verdict))
                throw new ArgumentException("Invalid verdict. Use 'OwnerFavored', 'BorrowerFavored', 'PartialDamage', or 'Inconclusive'.");

            if (verdict == DisputeVerdict.PartialDamage)
            {
                if (!dto.CustomFineAmount.HasValue || dto.CustomFineAmount <= 0)
                    throw new ArgumentException("A custom fine amount is required for a PartialDamage verdict.");
            }

            dispute.AdminVerdict = verdict;
            dispute.CustomFineAmount = dto.CustomFineAmount;
            dispute.AdminNote = dto.AdminNote.Trim();
            dispute.ResolvedByAdminId = adminId;
            dispute.ResolvedAt = DateTime.UtcNow;
            dispute.Status = DisputeStatus.Resolved;

            //Issue fines based on verdict
            switch (verdict)
            {
                case DisputeVerdict.OwnerFavored:
                    //Borrower is at fault — issue appropriate fine based on dispute context
                    //Owner filed as damage -> damage fine. Owner filed as lost -> lost fine.
                    if (dispute.FiledAs == DisputeFiledAs.AsOwner)
                        await _fineService.IssueDamagedFineAsync(dispute.LoanId, dispute.Id);
                    break;

                case DisputeVerdict.PartialDamage:
                    await _fineService.IssueCustomFineAsync(dispute.LoanId, dto.CustomFineAmount!.Value, dispute.Id);
                    break;

                case DisputeVerdict.BorrowerFavored:
                case DisputeVerdict.Inconclusive:
                    //No fine issued
                    break;
            }

            _disputeRepository.Update(dispute);
            await _disputeRepository.SaveChangesAsync();

            //Notify both parties
            var notifyMessage = $"The dispute for '{dispute.Loan.Item.Title}' has been resolved. Verdict: {verdict}.";

            await _notificationService.SendAsync(
                dispute.FiledById,
                NotificationType.DisputeResolved,
                notifyMessage,
                dispute.Id,
                NotificationReferenceType.Dispute
            );

            var otherPartyId = dispute.FiledById == dispute.Loan.BorrowerId
                ? dispute.Loan.Item.OwnerId
                : dispute.Loan.BorrowerId;

            await _notificationService.SendAsync(
                otherPartyId,
                NotificationType.DisputeResolved,
                notifyMessage,
                dispute.Id,
                NotificationReferenceType.Dispute
            );

            return MapToDetailDTO(dispute);
        }


        //Mappers
        private static DisputeDTO.DisputeDetailDTO MapToDetailDTO(Dispute d)
        {
            var filedByPhotos = d.Photos?.Where(p => p.SubmittedById == d.FiledById).ToList() ?? new();
            var responsePhotos = d.Photos?.Where(p => p.SubmittedById != d.FiledById).ToList() ?? new();

            return new DisputeDTO.DisputeDetailDTO
            {
                Id = d.Id,
                LoanId = d.LoanId,
                ItemTitle = d.Loan?.Item?.Title ?? string.Empty,
                FiledByName = d.FiledBy?.FullName ?? string.Empty,
                FiledAs = d.FiledAs.ToString(),
                Description = d.Description,
                ResponseDescription = d.ResponseDescription,
                ResponseDeadline = d.ResponseDeadline,
                Status = d.Status.ToString(),
                AdminVerdict = d.AdminVerdict?.ToString(),
                CustomFineAmount = d.CustomFineAmount,
                AdminNote = d.AdminNote,
                ResolvedAt = d.ResolvedAt,
                FiledByPhotos = filedByPhotos.Select(p => new DisputeDTO.DisputePhotoDTO
                {
                    Id = p.Id,
                    PhotoUrl = p.PhotoUrl,
                    SubmittedByName = p.SubmittedBy?.FullName ?? string.Empty,
                    Caption = p.Caption,
                    UploadedAt = p.UploadedAt
                }).ToList(),
                ResponsePhotos = responsePhotos.Select(p => new DisputeDTO.DisputePhotoDTO
                {
                    Id = p.Id,
                    PhotoUrl = p.PhotoUrl,
                    SubmittedByName = p.SubmittedBy?.FullName ?? string.Empty,
                    Caption = p.Caption,
                    UploadedAt = p.UploadedAt
                }).ToList(),
                SnapshotCondition = d.Loan?.SnapshotCondition.ToString() ?? string.Empty,
                SnapshotPhotos = d.Loan?.SnapshotPhotos?.OrderBy(p => p.DisplayOrder).Select(p => new LoanDTO.LoanSnapshotPhotoDTO
                {
                    Id = p.Id,
                    PhotoUrl = p.PhotoUrl,
                    DisplayOrder = p.DisplayOrder
                }).ToList() ?? new(),
                CreatedAt = d.CreatedAt
            };
        }


        private static DisputeDTO.DisputeSummaryDTO MapToSummaryDTO(Dispute d)
        {
            return new DisputeDTO.DisputeSummaryDTO
            {
                Id = d.Id,
                LoanId = d.LoanId,
                ItemTitle = d.Loan?.Item?.Title ?? string.Empty,
                FiledByName = d.FiledBy?.FullName ?? string.Empty,
                FiledAs = d.FiledAs.ToString(),
                Status = d.Status.ToString(),
                ResponseDeadline = d.ResponseDeadline,
                CreatedAt = d.CreatedAt
            };
        }




    }
}
