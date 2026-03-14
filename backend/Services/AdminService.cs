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
            //Run all queries in parallel — none depend on each other
            var pendingItemsTask = _itemRepository.GetPendingApprovalsAsync();
            var pendingLoansTask = _loanRepository.GetPendingAdminApprovalsAsync();
            var openDisputesTask = _disputeRepository.GetAllOpenAsync();
            var pendingAppealsTask = _appealRepository.GetAllPendingAsync();
            var pendingVerificationsTask = _verificationRepository.GetAllPendingAsync();
            var allUsersTask = _userRepository.GetAllAsync();
            var allUnpaidFinesTask = _fineRepository.GetAllUnpaidAsync();

            await Task.WhenAll(
                pendingItemsTask,
                pendingLoansTask,
                openDisputesTask,
                pendingAppealsTask,
                pendingVerificationsTask,
                allUsersTask,
                allUnpaidFinesTask
            );

            var allItems = await _itemRepository.GetAllApprovedAsync();
            var allLoans = await _loanRepository.GetAllAsync();
            var unpaidFines = allUnpaidFinesTask.Result;

            var activeStatuses = new[] { Models.LoanStatus.Approved, Models.LoanStatus.Active };

            return new AdminDTO.AdminDashboardDTO
            {
                //Action queues — drive the red badge counts in the sidebar
                PendingItemApprovals = pendingItemsTask.Result.Count,
                PendingLoanApprovals = pendingLoansTask.Result.Count,
                OpenDisputes = openDisputesTask.Result.Count,
                PendingAppeals = pendingAppealsTask.Result.Count,
                PendingVerifications = pendingVerificationsTask.Result.Count,

                //Platform stats
                TotalUsers = allUsersTask.Result.Count,
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
                        FiledByName = d.FiledBy?.FullName ?? string.Empty,
                        FiledAs = d.FiledAs.ToString(),
                        Status = d.Status.ToString(),
                        ResponseDeadline = d.ResponseDeadline,
                        CreatedAt = d.CreatedAt
                    }).ToList() ?? new()
                });
            }

            return new AdminDTO.ItemHistoryDTO
            {
                ItemId = item.Id,
                ItemTitle = item.Title,
                OwnerName = item.Owner?.FullName ?? string.Empty,
                Loans = loanHistory
            };
        }





    }
}
