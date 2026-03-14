using backend.DTOs;
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
    public class FineServiceTests
    {

        private readonly Mock<IFineRepository> _fineRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<ILoanRepository> _loanRepoMock;
        private readonly Mock<INotificationService> _notifMock;
        private readonly FineService _service;

        public FineServiceTests()
        {
            _fineRepoMock = new Mock<IFineRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _loanRepoMock = new Mock<ILoanRepository>();
            _notifMock = new Mock<INotificationService>();

            _service = new FineService(
                _fineRepoMock.Object,
                _userRepoMock.Object,
                _loanRepoMock.Object,
                _notifMock.Object
            );
        }

        [Fact]
        public async Task MarkAsPaidAsync_ValidProof_UpdatesStatusToPending()
        {
            var userId = "user123";
            var fine = new Fine { Id = 1, UserId = userId, Status = FineStatus.Unpaid, Amount = 100 };
            _fineRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(fine);

            var dto = new FineDTO.PayFineDTO
            {
                FineId = 1,
                PaymentProofImageUrl = "http://proof.jpg",
                PaymentDescription = "Paid via MobilePay"
            };

            await _service.MarkAsPaidAsync(userId, dto);

            // Assert
            Assert.Equal(FineStatus.PendingVerification, fine.Status);
            Assert.Equal("Paid via MobilePay", fine.PaymentDescription);
            _fineRepoMock.Verify(r => r.Update(fine), Times.Once);
            _notifMock.Verify(n => n.SendAsync(userId, NotificationType.FineIssued, It.IsAny<string>(), 1, NotificationReferenceType.Fine), Times.Once);
        }


        [Fact]
        public async Task AdminConfirmPayment_Approved_DeductsFromUserTotal()
        {
            var user = new ApplicationUser { Id = "u1", UnpaidFinesTotal = 500 };
            var fine = new Fine { Id = 1, UserId = "u1", Amount = 100, Status = FineStatus.PendingVerification };

            _fineRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(fine);
            _userRepoMock.Setup(r => r.GetByIdAsync("u1")).ReturnsAsync(user);

            var dto = new FineDTO.AdminFineVerificationDTO { FineId = 1, IsApproved = true };

            await _service.AdminConfirmPaymentAsync("admin1", dto);

            Assert.Equal(FineStatus.Paid, fine.Status);
            Assert.Equal(400, user.UnpaidFinesTotal); // 500 - 100
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task AdminConfirmPayment_Rejected_RequiresReason()
        {
            // Arrange
            var fine = new Fine { Id = 1, Status = FineStatus.PendingVerification };
            _fineRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(fine);

            var dto = new FineDTO.AdminFineVerificationDTO { FineId = 1, IsApproved = false, RejectionReason = "" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AdminConfirmPaymentAsync("admin1", dto));
        }

        [Fact]
        public async Task IssueLateReturnFineAsync_CalculatesCorrectAmount_AndIncreasesUserTotal()
        {
            var user = new ApplicationUser { Id = "b1", UnpaidFinesTotal = 0 };
            var loan = new Loan
            {
                Id = 50,
                BorrowerId = "b1",
                Item = new Item { Title = "Drill", CurrentValue = 1000 }
            };

            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(50)).ReturnsAsync(loan);
            _userRepoMock.Setup(r => r.GetByIdAsync("b1")).ReturnsAsync(user);

            await _service.IssueLateReturnFineAsync(50);

            _fineRepoMock.Verify(r => r.AddAsync(It.Is<Fine>(f =>
                f.Amount == 100m &&
                f.Type == FineType.Late)), Times.Once);

            Assert.Equal(100m, user.UnpaidFinesTotal);
        }

        [Fact]
        public async Task IssueDamagedFineAsync_Calculates50PercentOfValue()
        {
            var user = new ApplicationUser { Id = "b1" };
            var loan = new Loan { Id = 1, BorrowerId = "b1", Item = new Item { CurrentValue = 600 } };

            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _userRepoMock.Setup(r => r.GetByIdAsync("b1")).ReturnsAsync(user);

            await _service.IssueDamagedFineAsync(1);

            _fineRepoMock.Verify(r => r.AddAsync(It.Is<Fine>(f => f.Amount == 300m)), Times.Once);
        }


        [Fact]
        public async Task AdminUpdateFineAsync_ChangingAmount_AdjustsUserTotalCorrect()
        {
            var user = new ApplicationUser { Id = "u1", UnpaidFinesTotal = 100 };
            var fine = new Fine { Id = 1, UserId = "u1", Amount = 100, Status = FineStatus.Unpaid };

            _fineRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(fine);
            _userRepoMock.Setup(r => r.GetByIdAsync("u1")).ReturnsAsync(user);

            var dto = new FineDTO.AdminUpdateFineDTO { Amount = 150 }; //Increasing fine by 50

            await _service.AdminUpdateFineAsync(1, dto);

            Assert.Equal(150, user.UnpaidFinesTotal); 
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }


        [Fact]
        public async Task AdminUpdateFineAsync_StatusToPaid_DeductsFromTotal()
        {
            var user = new ApplicationUser { Id = "1234", UnpaidFinesTotal = 100 };

            var fine = new Fine { Id = 1, UserId = user.Id, Amount = 100, Status = FineStatus.Unpaid };

            var dto = new FineDTO.AdminUpdateFineDTO { Status = FineStatus.Paid };


            _fineRepoMock.Setup(x => x.GetByIdWithDetailsAsync(fine.Id))
                .ReturnsAsync(fine);

            _userRepoMock.Setup(x => x.GetByIdAsync(user.Id)) .ReturnsAsync(user);


            var res = await _service.AdminUpdateFineAsync(fine.Id, dto);



            Assert.NotNull(res);
            Assert.Equal(0, user.UnpaidFinesTotal);
            Assert.NotNull(fine.VerifiedAt);



        }


        [Fact]
        public async Task AdminUpdateFineAsync_StatusToUnpaid_AddsBackToTotal()
        {
            var userId = "user1";
            var user = new ApplicationUser { Id = userId, UnpaidFinesTotal = 50 };
            var fine = new Fine { Id = 1, UserId = userId, Amount = 100, Status = FineStatus.Paid, VerifiedAt = DateTime.UtcNow };

            _fineRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(fine);
            _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

            var dto = new FineDTO.AdminUpdateFineDTO { Status = FineStatus.Unpaid };

            await _service.AdminUpdateFineAsync(1, dto);

            Assert.Equal(150, user.UnpaidFinesTotal); //50 + 100
            Assert.Null(fine.VerifiedAt); //Verification timestamp should be cleared
            _userRepoMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }



    }
}
