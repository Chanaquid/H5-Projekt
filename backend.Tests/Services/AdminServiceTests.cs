using backend.Interfaces;
using backend.Models;
using backend.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace backend.Tests.Services
{
    public class AdminServiceTests
    {
        private readonly Mock<IItemRepository> _mockItemRepo = new();
        private readonly Mock<ILoanRepository> _mockLoanRepo = new();
        private readonly Mock<IFineRepository> _mockFineRepo = new();
        private readonly Mock<IDisputeRepository> _mockDisputeRepo = new();
        private readonly Mock<IAppealRepository> _mockAppealRepo = new();
        private readonly Mock<IVerificationRepository> _mockVerificationRepo = new();
        private readonly Mock<IUserRepository> _mockUserRepo = new();

        private readonly AdminService _adminService;

        public AdminServiceTests()
        {
            _adminService = new AdminService(
                _mockItemRepo.Object,
                _mockLoanRepo.Object,
                _mockFineRepo.Object,
                _mockDisputeRepo.Object,
                _mockAppealRepo.Object,
                _mockVerificationRepo.Object,
                _mockUserRepo.Object
            );
        }


        [Fact]
        public async Task GetDashboardAsync_ReturnsCorrectCounts()
        {
            _mockItemRepo.Setup(x => x.GetPendingApprovalsAsync())
                .ReturnsAsync(new List<Item> { new Item(), new Item() }); 
            _mockLoanRepo.Setup(x => x.GetPendingAdminApprovalsAsync())
                .ReturnsAsync(new List<Loan> { new Loan() }); 
            _mockDisputeRepo.Setup(x => x.GetAllOpenAsync())
                .ReturnsAsync(new List<Dispute>());
            _mockAppealRepo.Setup(x => x.GetAllPendingAsync())
                .ReturnsAsync(new List<Appeal> { new Appeal(), new Appeal(), new Appeal() }); 
            _mockVerificationRepo.Setup(x => x.GetAllPendingAsync())
                .ReturnsAsync(new List<VerificationRequest> { new VerificationRequest() }); 
            _mockUserRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<ApplicationUser> { new ApplicationUser(), new ApplicationUser(), new ApplicationUser() });
            _mockItemRepo.Setup(x => x.GetAllApprovedAsync())
                .ReturnsAsync(new List<Item> { new Item(), new Item(), new Item(), new Item() }); 
            _mockLoanRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Models.Loan>
                {
                new Models.Loan { Status = LoanStatus.Approved },
                new Models.Loan { Status = LoanStatus.Active },
                new Models.Loan { Status = LoanStatus.Returned }
                });
            _mockFineRepo.Setup(x => x.GetAllUnpaidAsync())
                .ReturnsAsync(new List<Models.Fine>
                {
                new Models.Fine { Amount = 10 },
                new Models.Fine { Amount = 5 }
                });

            var result = await _adminService.GetDashboardAsync();

            Assert.Equal(2, result.PendingItemApprovals);
            Assert.Equal(1, result.PendingLoanApprovals);
            Assert.Equal(0, result.OpenDisputes);
            Assert.Equal(3, result.PendingAppeals);
            Assert.Equal(1, result.PendingVerifications);

            Assert.Equal(3, result.TotalUsers);
            Assert.Equal(4, result.TotalActiveItems);
            Assert.Equal(1, result.TotalActiveLoans); 
            Assert.Equal(2, result.TotalUnpaidFines);
            Assert.Equal(15, result.TotalUnpaidFinesAmount);
        }


        [Fact]
        public async Task GetItemHistoryAsync_ReturnsCorrectLoanHistory()
        {
            // Arrange
            var itemId = 1;
            var item = new Item
            {
                Id = itemId,
                Title = "Test Item",
                Owner = new ApplicationUser { FullName = "Owner Name" }
            };

            _mockItemRepo.Setup(x => x.GetByIdWithDetailsAsync(itemId))
                .ReturnsAsync(item);

            var loan = new Loan
            {
                Id = 10,
                ItemId = itemId,
                Borrower = new ApplicationUser { FullName = "Borrower Name" },
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow,
                Status = LoanStatus.Active,
                SnapshotCondition = ItemCondition.Good,
                SnapshotPhotos = new List<LoanSnapshotPhoto>
                {
                    new() { Id = 1, PhotoUrl = "url1", DisplayOrder = 1 },
                    new() { Id = 2, PhotoUrl = "url2", DisplayOrder = 2 }
                },
                Fines = new List<Fine>
                {
                    new() { Id = 100, LoanId = 10, Amount = 50, Status = FineStatus.Unpaid }
                },
                Disputes = new List<Dispute>
                {
                    new()
                    {
                        Id = 200,
                        LoanId = 10,
                        Status = DisputeStatus.Open,
                        FiledBy = new ApplicationUser { FullName = "User A" },
                        FiledAs =DisputeFiledAs.AsOwner
                    }
                }
            };

            _mockLoanRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Loan> { loan });

            _mockLoanRepo.Setup(x => x.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            var result = await _adminService.GetItemHistoryAsync(itemId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(itemId, result.ItemId);
            Assert.Equal("Test Item", result.ItemTitle);
            Assert.Equal("Owner Name", result.OwnerName);
            Assert.Single(result.Loans);

            var loanDto = result.Loans.First();
            Assert.Equal(loan.Id, loanDto.LoanId);
            Assert.Equal("Borrower Name", loanDto.BorrowerName);
            Assert.Equal(loan.Status.ToString(), loanDto.Status);
            Assert.Equal(loan.SnapshotCondition.ToString(), loanDto.SnapshotCondition);

            // Snapshot photos
            Assert.Equal(2, loanDto.SnapshotPhotos.Count);
            Assert.Equal(1, loanDto.SnapshotPhotos[0].Id);

            // Fines
            Assert.Single(loanDto.Fines);
            Assert.Equal(50, loanDto.Fines[0].Amount);

            // Disputes
            Assert.Single(loanDto.Disputes);
            Assert.Equal("User A", loanDto.Disputes[0].FiledByName);
            Assert.Equal("AsOwner", loanDto.Disputes[0].FiledAs);
        }
    }


}



