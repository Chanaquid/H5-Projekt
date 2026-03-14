using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using Moq;

namespace backend.Tests.Services
{
    public class LoanServiceTests
    {
        private readonly Mock<ILoanRepository> _loanRepoMock;
        private readonly Mock<IItemRepository> _itemRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IFineService> _fineServiceMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly LoanService _service;

        public LoanServiceTests()
        {
            _loanRepoMock = new Mock<ILoanRepository>();
            _itemRepoMock = new Mock<IItemRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _fineServiceMock = new Mock<IFineService>();
            _notificationServiceMock = new Mock<INotificationService>();

            _service = new LoanService(
                _loanRepoMock.Object,
                _itemRepoMock.Object,
                _userRepoMock.Object,
                _fineServiceMock.Object,
                _notificationServiceMock.Object);
        }


        [Fact]
        public async Task CreateAsync_ValidData_HighScore_ReturnsPendingLoan()
        {
            var borrowerId = "borrower-1";
            var item = MakeItem("owner-1");
            var borrower = MakeBorrower(borrowerId, score: 75);
            var dto = new LoanDTO.CreateLoanDTO
            {
                ItemId = 1,
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(5)
            };

            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
            _userRepoMock.Setup(x => x.GetByIdAsync(borrowerId)).ReturnsAsync(borrower);
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<int>())).ReturnsAsync(
                new Loan { Id = 1, Item = item, Borrower = borrower, StartDate = dto.StartDate, EndDate = dto.EndDate, Status = LoanStatus.Pending });

            var result = await _service.CreateAsync(borrowerId, dto);

            Assert.Equal("Pending", result.Status);
            _loanRepoMock.Verify(x => x.AddAsync(It.IsAny<Loan>()), Times.Once);
            //High score (50+) — owner notified immediately
            _notificationServiceMock.Verify(x => x.SendAsync("owner-1", NotificationType.LoanRequested, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<NotificationReferenceType>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ValidData_LowScore_ReturnsAdminPendingLoan()
        {
            var borrowerId = "borrower-1";
            var item = MakeItem("owner-1");
            var borrower = MakeBorrower(borrowerId, score: 30); //20-49 -> AdminPending
            var dto = new LoanDTO.CreateLoanDTO
            {
                ItemId = 1,
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(5)
            };

            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
            _userRepoMock.Setup(x => x.GetByIdAsync(borrowerId)).ReturnsAsync(borrower);
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<int>())).ReturnsAsync(
                new Loan { Id = 1, Item = item, Borrower = borrower, Status = LoanStatus.AdminPending });

            var result = await _service.CreateAsync(borrowerId, dto);

            Assert.Equal("AdminPending", result.Status);
            //AdminPending — owner should NOT be notified yet
            _notificationServiceMock.Verify(x => x.SendAsync("owner-1", NotificationType.LoanRequested, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<NotificationReferenceType>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ScoreTooLow_ThrowsInvalidOperation()
        {
            var borrowerId = "borrower-1";
            var item = MakeItem("owner-1");
            var borrower = MakeBorrower(borrowerId, score: 10);
            var dto = new LoanDTO.CreateLoanDTO
            {
                ItemId = 1,
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(5)
            };

            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
            _userRepoMock.Setup(x => x.GetByIdAsync(borrowerId)).ReturnsAsync(borrower);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(borrowerId, dto));
        }

        [Fact]
        public async Task CreateAsync_BorrowingOwnItem_ThrowsArgumentException()
        {
            var userId = "user-1";
            var item = MakeItem(userId); //owner == borrower
            var dto = new LoanDTO.CreateLoanDTO
            {
                ItemId = 1,
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(5)
            };

            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(userId, dto));
        }

        [Fact]
        public async Task CreateAsync_StartDateInPast_ThrowsArgumentException()
        {
            var borrowerId = "borrower-1";
            var item = MakeItem("owner-1");
            var dto = new LoanDTO.CreateLoanDTO
            {
                ItemId = 1,
                StartDate = DateTime.UtcNow.Date.AddDays(-1), //past
                EndDate = DateTime.UtcNow.Date.AddDays(5)
            };

            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(borrowerId, dto));
        }

        [Fact]
        public async Task CreateAsync_EndDateBeforeStartDate_ThrowsArgumentException()
        {
            var borrowerId = "borrower-1";
            var item = MakeItem("owner-1");
            var dto = new LoanDTO.CreateLoanDTO
            {
                ItemId = 1,
                StartDate = DateTime.UtcNow.Date.AddDays(5),
                EndDate = DateTime.UtcNow.Date.AddDays(1) //before start
            };

            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(borrowerId, dto));
        }

        [Fact]
        public async Task CreateAsync_ItemAlreadyOnActiveLoan_ThrowsInvalidOperation()
        {
            var borrowerId = "borrower-2";
            var item = MakeItem("owner-1");
            item.Loans = new List<Loan> { new Loan { Status = LoanStatus.Active } };
            var borrower = MakeBorrower(borrowerId, score: 75);
            var dto = new LoanDTO.CreateLoanDTO
            {
                ItemId = 1,
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(5)
            };

            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
            _userRepoMock.Setup(x => x.GetByIdAsync(borrowerId)).ReturnsAsync(borrower);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(borrowerId, dto));
        }

        [Fact]
        public async Task CreateAsync_ItemRequiresVerification_UnverifiedBorrower_ThrowsInvalidOperation()
        {
            var borrowerId = "borrower-1";
            var item = MakeItem("owner-1");
            item.RequiresVerification = true;
            var borrower = MakeBorrower(borrowerId, score: 75);
            borrower.IsVerified = false;
            var dto = new LoanDTO.CreateLoanDTO
            {
                ItemId = 1,
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(5)
            };

            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
            _userRepoMock.Setup(x => x.GetByIdAsync(borrowerId)).ReturnsAsync(borrower);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(borrowerId, dto));
        }

        [Fact]
        public async Task CreateAsync_BelowMinLoanDays_ThrowsArgumentException()
        {
            var borrowerId = "borrower-1";
            var item = MakeItem("owner-1");
            item.MinLoanDays = 7;
            var borrower = MakeBorrower(borrowerId, score: 75);
            var dto = new LoanDTO.CreateLoanDTO
            {
                ItemId = 1,
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(3) //only 2 days — below min
            };

            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);
            _userRepoMock.Setup(x => x.GetByIdAsync(borrowerId)).ReturnsAsync(borrower);

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(borrowerId, dto));
        }


        [Fact]
        public async Task CancelAsync_PendingLoan_CancelsSuccessfully()
        {
            var borrowerId = "borrower-1";
            var loan = MakeLoan(borrowerId, LoanStatus.Pending);
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            var result = await _service.CancelAsync(1, borrowerId, new LoanDTO.CancelLoanDTO());

            Assert.Equal("Cancelled", result.Status);
        }

        [Fact]
        public async Task CancelAsync_ActiveLoan_ThrowsInvalidOperation()
        {
            var borrowerId = "borrower-1";
            var loan = MakeLoan(borrowerId, LoanStatus.Active);
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CancelAsync(1, borrowerId, new LoanDTO.CancelLoanDTO()));
        }

        [Fact]
        public async Task CancelAsync_WrongUser_ThrowsUnauthorized()
        {
            var loan = MakeLoan("borrower-1", LoanStatus.Pending);
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.CancelAsync(1, "wrong-user", new LoanDTO.CancelLoanDTO()));
        }


        [Fact]
        public async Task DecideAsync_Approve_ChangesStatusToApproved()
        {
            var ownerId = "owner-1";
            var loan = MakeLoan("borrower-1", LoanStatus.Pending, ownerId);
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            var result = await _service.DecideAsync(1, ownerId, new LoanDTO.LoanDecisionDTO { IsApproved = true });

            Assert.Equal("Approved", result.Status);
        }

        [Fact]
        public async Task DecideAsync_Reject_ChangesStatusToRejected()
        {
            var ownerId = "owner-1";
            var loan = MakeLoan("borrower-1", LoanStatus.Pending, ownerId);
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            var result = await _service.DecideAsync(1, ownerId, new LoanDTO.LoanDecisionDTO { IsApproved = false });

            Assert.Equal("Rejected", result.Status);
        }

        [Fact]
        public async Task DecideAsync_WrongOwner_ThrowsUnauthorized()
        {
            var loan = MakeLoan("borrower-1", LoanStatus.Pending, "owner-1");
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.DecideAsync(1, "wrong-owner", new LoanDTO.LoanDecisionDTO { IsApproved = true }));
        }

        [Fact]
        public async Task DecideAsync_NotPendingLoan_ThrowsInvalidOperation()
        {
            var ownerId = "owner-1";
            var loan = MakeLoan("borrower-1", LoanStatus.Approved, ownerId);
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DecideAsync(1, ownerId, new LoanDTO.LoanDecisionDTO { IsApproved = true }));
        }


        [Fact]
        public async Task HandleLoanReturnAsync_OnTime_SetsReturnedAndAddsScore()
        {
            var borrower = MakeBorrower("borrower-1", score: 50);
            var loan = new Loan
            {
                Id = 1,
                BorrowerId = borrower.Id,
                Borrower = borrower,
                EndDate = DateTime.UtcNow.Date.AddDays(2), //not yet due — on time
                Item = new Item { Id = 1, Title = "Drill", OwnerId = "owner-1", Owner = new ApplicationUser { Id = "owner-1" } }
            };

            await _service.HandleLoanReturnAsync(loan);

            Assert.Equal(LoanStatus.Returned, loan.Status);
            Assert.NotNull(loan.ActualReturnDate);
            //+5 score added
            _userRepoMock.Verify(x => x.AddScoreHistoryAsync(It.Is<ScoreHistory>(s =>
                s.PointsChanged == LoanService.OnTimeReturnScore &&
                s.Reason == ScoreChangeReason.OnTimeReturn)), Times.Once);
            _userRepoMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.Score == 55)), Times.Once);
        }

        [Fact]
        public async Task HandleLoanReturnAsync_Late_SetsReturnedNoScoreAdded()
        {
            var borrower = MakeBorrower("borrower-1", score: 50);
            var loan = new Loan
            {
                Id = 1,
                BorrowerId = borrower.Id,
                Borrower = borrower,
                EndDate = DateTime.UtcNow.Date.AddDays(-3), //3 days overdue
                Item = new Item { Id = 1, Title = "Drill", OwnerId = "owner-1", Owner = new ApplicationUser { Id = "owner-1" } }
            };

            await _service.HandleLoanReturnAsync(loan);

            Assert.Equal(LoanStatus.Returned, loan.Status);
            Assert.NotNull(loan.ActualReturnDate);
            // No score bonus for late return
            _userRepoMock.Verify(x => x.AddScoreHistoryAsync(It.IsAny<ScoreHistory>()), Times.Never);
            _userRepoMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task HandleLoanReturnAsync_OnTime_NotifiesBorrowerAndOwner()
        {
            var borrower = MakeBorrower("borrower-1", score: 50);
            var loan = new Loan
            {
                Id = 1,
                BorrowerId = borrower.Id,
                Borrower = borrower,
                EndDate = DateTime.UtcNow.Date.AddDays(1),
                Item = new Item { Id = 1, Title = "Drill", OwnerId = "owner-1", Owner = new ApplicationUser { Id = "owner-1" } }
            };

            await _service.HandleLoanReturnAsync(loan);

            _notificationServiceMock.Verify(x => x.SendAsync("borrower-1", NotificationType.LoanReturned, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<NotificationReferenceType>()), Times.Once);
            _notificationServiceMock.Verify(x => x.SendAsync("owner-1", NotificationType.LoanReturned, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<NotificationReferenceType>()), Times.Once);
        }

        [Fact]
        public async Task HandleLoanReturnAsync_Late_StillNotifiesBorrowerAndOwner()
        {
            var borrower = MakeBorrower("borrower-1", score: 50);
            var loan = new Loan
            {
                Id = 1,
                BorrowerId = borrower.Id,
                Borrower = borrower,
                EndDate = DateTime.UtcNow.Date.AddDays(-5),
                Item = new Item { Id = 1, Title = "Drill", OwnerId = "owner-1", Owner = new ApplicationUser { Id = "owner-1" } }
            };

            await _service.HandleLoanReturnAsync(loan);

            _notificationServiceMock.Verify(x => x.SendAsync("borrower-1", NotificationType.LoanReturned, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<NotificationReferenceType>()), Times.Once);
            _notificationServiceMock.Verify(x => x.SendAsync("owner-1", NotificationType.LoanReturned, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<NotificationReferenceType>()), Times.Once);
        }

 
        [Fact]
        public async Task RequestExtensionAsync_ValidRequest_SetsExtensionPending()
        {
            var borrowerId = "borrower-1";
            var loan = MakeLoan(borrowerId, LoanStatus.Active);
            loan.EndDate = DateTime.UtcNow.Date.AddDays(3);
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            var dto = new LoanDTO.RequestExtensionDTO
            {
                RequestedExtensionDate = DateTime.UtcNow.Date.AddDays(10)
            };

            var result = await _service.RequestExtensionAsync(1, borrowerId, dto);

            Assert.Equal("Pending", result.ExtensionRequestStatus);
        }

        [Fact]
        public async Task RequestExtensionAsync_NotActiveLoan_ThrowsInvalidOperation()
        {
            var borrowerId = "borrower-1";
            var loan = MakeLoan(borrowerId, LoanStatus.Pending);
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RequestExtensionAsync(1, borrowerId, new LoanDTO.RequestExtensionDTO
                {
                    RequestedExtensionDate = DateTime.UtcNow.AddDays(10)
                }));
        }

        [Fact]
        public async Task RequestExtensionAsync_DateBeforeEndDate_ThrowsArgumentException()
        {
            var borrowerId = "borrower-1";
            var loan = MakeLoan(borrowerId, LoanStatus.Active);
            loan.EndDate = DateTime.UtcNow.Date.AddDays(5);
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.RequestExtensionAsync(1, borrowerId, new LoanDTO.RequestExtensionDTO
                {
                    RequestedExtensionDate = DateTime.UtcNow.Date.AddDays(2) //before end date
                }));
        }

        [Fact]
        public async Task RequestExtensionAsync_AlreadyPending_ThrowsInvalidOperation()
        {
            var borrowerId = "borrower-1";
            var loan = MakeLoan(borrowerId, LoanStatus.Active);
            loan.ExtensionRequestStatus = ExtensionStatus.Pending;
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RequestExtensionAsync(1, borrowerId, new LoanDTO.RequestExtensionDTO
                {
                    RequestedExtensionDate = DateTime.UtcNow.Date.AddDays(10)
                }));
        }

        [Fact]
        public async Task DecideExtensionAsync_Approve_UpdatesEndDate()
        {
            var ownerId = "owner-1";
            var newDate = DateTime.UtcNow.Date.AddDays(10);
            var loan = MakeLoan("borrower-1", LoanStatus.Active, ownerId);
            loan.ExtensionRequestStatus = ExtensionStatus.Pending;
            loan.RequestedExtensionDate = newDate;
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            var result = await _service.DecideExtensionAsync(1, ownerId, new LoanDTO.ExtensionDecisionDTO { IsApproved = true });

            Assert.Equal("Approved", result.ExtensionRequestStatus);
            Assert.Equal(newDate, loan.EndDate);
        }

        [Fact]
        public async Task DecideExtensionAsync_Reject_DoesNotUpdateEndDate()
        {
            var ownerId = "owner-1";
            var originalEnd = DateTime.UtcNow.Date.AddDays(5);
            var loan = MakeLoan("borrower-1", LoanStatus.Active, ownerId);
            loan.EndDate = originalEnd;
            loan.ExtensionRequestStatus = ExtensionStatus.Pending;
            loan.RequestedExtensionDate = DateTime.UtcNow.Date.AddDays(10);
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await _service.DecideExtensionAsync(1, ownerId, new LoanDTO.ExtensionDecisionDTO { IsApproved = false });

            Assert.Equal(originalEnd, loan.EndDate); //unchanged
        }

        private static Item MakeItem(string ownerId) => new Item
        {
            Id = 1,
            OwnerId = ownerId,
            Status = ItemStatus.Approved,
            IsActive = true,
            Condition = ItemCondition.Good,
            AvailableFrom = DateTime.UtcNow.Date,
            AvailableUntil = DateTime.UtcNow.Date.AddDays(30),
            Owner = new ApplicationUser { Id = ownerId },
            Loans = new List<Loan>(),
            Photos = new List<ItemPhoto>()
        };

        private static ApplicationUser MakeBorrower(string id, int score) => new ApplicationUser
        {
            Id = id,
            FullName = "Test Borrower",
            Score = score,
            IsVerified = true,
            ScoreHistory = new List<ScoreHistory>()
        };

        private static Loan MakeLoan(string borrowerId, LoanStatus status, string ownerId = "owner-1") => new Loan
        {
            Id = 1,
            BorrowerId = borrowerId,
            Status = status,
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(5),
            Borrower = new ApplicationUser { Id = borrowerId, FullName = "Borrower" },
            Item = new Item
            {
                Id = 1,
                Title = "Test Item",
                OwnerId = ownerId,
                AvailableUntil = DateTime.UtcNow.Date.AddDays(30),
                Owner = new ApplicationUser { Id = ownerId, FullName = "Owner" }
            }
        };
    }
}