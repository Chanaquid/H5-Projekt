using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace backend.Tests.Services
{
    public class LoanServiceTests
    {
        private readonly Mock<ILoanRepository> _loanRepoMock;
        private readonly Mock<IItemRepository> _itemRepoMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IFineService> _fineServiceMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly LoanService _service;

        public LoanServiceTests()
        {
            _loanRepoMock = new Mock<ILoanRepository>();
            _itemRepoMock = new Mock<IItemRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _fineServiceMock = new Mock<IFineService>();
            _notificationServiceMock = new Mock<INotificationService>();

            _service = new LoanService(
                _loanRepoMock.Object,
                _itemRepoMock.Object,
                _userRepositoryMock.Object,
                _fineServiceMock.Object,
                _notificationServiceMock.Object
            );
        }


        [Fact]
        public async Task CreateAsync_LowScoreBorrower_SetsStatusToAdminPending()
        {
            var borrowerId = "user-low-score";
            var owner = new ApplicationUser { Id = "owner", FullName = "Owner Name" };

            var item = new Item
            {
                Id = 1,
                OwnerId = "owner",
                Owner = owner, 
                Status = ItemStatus.Approved,
                IsActive = true,
                AvailableFrom = DateTime.UtcNow.AddDays(-1), 
                AvailableUntil = DateTime.UtcNow.AddDays(10),
                Photos = new List<ItemPhoto>(), 
                Condition = ItemCondition.Good
            };

            var borrower = new ApplicationUser
            {
                Id = borrowerId,
                FullName = "Borrower Name",
                Score = 30,
                UnpaidFinesTotal = 0
            };

            var dto = new LoanDTO.CreateLoanDTO
            {
                ItemId = 1,
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(3)
            };

            var savedLoan = new Loan
            {
                Id = 1,
                Item = item,
                Borrower = borrower,
                Status = LoanStatus.AdminPending,
                SnapshotPhotos = new List<LoanSnapshotPhoto>()
            };

            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(borrowerId)).ReturnsAsync(borrower);

            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<int>())).ReturnsAsync(savedLoan);

            var result = await _service.CreateAsync(borrowerId, dto);

            result.Status.Should().Be(LoanStatus.AdminPending.ToString());
            _notificationServiceMock.Verify(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<NotificationReferenceType>()),
            Times.Never);
        }

        [Fact]
        public async Task CreateAsync_OutsideAvailabilityWindow_ThrowsArgumentException()
        {
            var item = new Item
            {
                Id = 1,
                Status = ItemStatus.Approved,
                IsActive = true,
                AvailableFrom = DateTime.UtcNow.AddDays(5),
                AvailableUntil = DateTime.UtcNow.AddDays(10)
            };
            var dto = new LoanDTO.CreateLoanDTO { ItemId = 1, StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(3) };

            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync("any-id", dto));
        }


        [Theory]
        [InlineData(30, -20, 10, -5)]   // borrower 30, 4 prior days (-20), 10 days overdue, expect -5 more applied
        [InlineData(50, 0, 5, -25)]     // borrower 50, no prior penalties, 5 days overdue, expect -25 (max cap)
        [InlineData(40, -15, 3, -10)]   // borrower 40, -15 points already applied, 3 days overdue, expect remaining cap applied (-10)
        [InlineData(10, -25, 5, 0)]     // borrower 10, already at max cap, no more penalty
        [InlineData(5, 0, 3, -5)]
        [InlineData(0, 0, 3, 0)]
        public async Task ProcessLateLoansAsync_TheoryTests(int initialScore, int priorPoints, int daysOverdue, int expectedPenalty)
        {
            var loanId = 100;
            var borrower = new ApplicationUser
            {
                Id = "b1",
                Score = initialScore,
                ScoreHistory = priorPoints != 0
                    ? new List<ScoreHistory>
                    {
                new ScoreHistory
                {
                    LoanId = loanId,
                    Reason = ScoreChangeReason.LateReturn,
                    PointsChanged = priorPoints
                }
                    }
                    : new List<ScoreHistory>()
            };

            var loan = new Loan
            {
                Id = loanId,
                BorrowerId = borrower.Id,
                Borrower = borrower,
                Status = LoanStatus.Active,
                EndDate = DateTime.UtcNow.AddDays(-daysOverdue),
                Item = new Item { Title = "Test Item", OwnerId = "owner-1" },
                Fines = new List<Fine>()
            };

            _loanRepoMock.Setup(x => x.GetActiveAndOverdueAsync()).ReturnsAsync(new List<Loan> { loan });

            await _service.ProcessLateLoansAsync();

            // Expected score change
            var expectedScore = Math.Clamp(initialScore + expectedPenalty, 0, 100);
            borrower.Score.Should().Be(expectedScore);

            if (expectedPenalty != 0)
            {
                _userRepositoryMock.Verify(x => x.AddScoreHistoryAsync(It.Is<ScoreHistory>(s => s.PointsChanged == expectedPenalty)), Times.Once);
            }
            else
            {
                _userRepositoryMock.Verify(x => x.AddScoreHistoryAsync(It.IsAny<ScoreHistory>()), Times.Never);
            }

            _fineServiceMock.Verify(x => x.IssueLateReturnFineAsync(loanId), Times.AtLeastOnce);
            _loanRepoMock.Verify(x => x.Update(It.IsAny<Loan>()), Times.AtLeastOnce);
        }



        [Fact]
        public async Task AdminDecideAsync_OnApproval_MovesToPendingAndNotifiesOwner()
        {
            var loan = new Loan
            {
                Id = 1,
                Status = LoanStatus.AdminPending,
                Item = new Item
                {
                    OwnerId = "owner-id",
                    Title = "Drill",
                    Owner = new ApplicationUser { Id = "owner-id", FullName = "Alice" } 
                },
                Borrower = new ApplicationUser { FullName = "John" }
            };
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await _service.AdminDecideAsync(1, "admin", new LoanDTO.LoanDecisionDTO { IsApproved = true });

            loan.Status.Should().Be(LoanStatus.Pending);
            _notificationServiceMock.Verify(x => x.SendAsync(
                "owner-id",
                NotificationType.LoanRequested,
                It.Is<string>(s => s.Contains("John")),
                1,
                NotificationReferenceType.Loan), Times.Once);
        }




    }
}
