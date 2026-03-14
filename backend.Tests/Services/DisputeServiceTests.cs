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
    public class DisputeServiceTests
    {
        private readonly Mock<IDisputeRepository> _disputeRepoMock;
        private readonly Mock<ILoanRepository> _loanRepoMock;
        private readonly Mock<IFineService> _fineServiceMock;
        private readonly Mock<INotificationService> _notifMock;
        private readonly DisputeService _service;

        public DisputeServiceTests()
        {
            _disputeRepoMock = new Mock<IDisputeRepository>();
            _loanRepoMock = new Mock<ILoanRepository>();
            _fineServiceMock = new Mock<IFineService>();
            _notifMock = new Mock<INotificationService>();

            _service = new DisputeService(
                _disputeRepoMock.Object,
                _loanRepoMock.Object,
                _fineServiceMock.Object,
                _notifMock.Object
            );
        }

        [Fact]
        public async Task CreateAsync_WhenUserNotPartOfLoan_ThrowsUnauthorized()
        {
            var loan = new Loan { Id = 1, BorrowerId = "borrower1", Item = new Item { OwnerId = "owner1" }, Status = LoanStatus.Returned };
            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            var dto = new DisputeDTO.CreateDisputeDTO { LoanId = 1, FiledAs = "AsOwner", Description = "Test" };

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CreateAsync("random_user", dto));
        
            Assert.Equal("You are not a party to this loan.", ex.Message);

        
        }


        [Fact]
        public async Task CreateAsync_WhenStatusNotDisputable_ThrowsInvalidOperation()
        {
            var loan = new Loan { Id = 1, BorrowerId = "borrower1", Item = new Item { OwnerId = "owner1" }, Status = LoanStatus.Approved };
            var dto = new DisputeDTO.CreateDisputeDTO { LoanId = 1, FiledAs = "AsOwner", Description = "Test" };

            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync("owner1", dto));

            Assert.Equal("A dispute can only be filed on an active, returned, or late loan.", ex.Message);
        
        }


        [Fact]
        public async Task CreateAsync_ValidRequest_Sets72HourDeadlineAndNotifiesOtherParty()
        {
            var loan = new Loan { Id = 1, BorrowerId = "b1", Item = new Item { OwnerId = "o1", Title = "Drill" }, Status = LoanStatus.Returned };
            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            _disputeRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>())).ReturnsAsync(new Dispute { Loan = loan });

            var dto = new DisputeDTO.CreateDisputeDTO { LoanId = 1, FiledAs = "AsOwner", Description = "Broken" };

            await _service.CreateAsync("o1", dto);

            _disputeRepoMock.Verify(r => r.AddAsync(It.Is<Dispute>(d =>
                d.Status == DisputeStatus.AwaitingResponse &&
                d.ResponseDeadline > DateTime.UtcNow.AddHours(71))), Times.Once);

            _notifMock.Verify(n => n.SendAsync("b1", NotificationType.DisputeFiled, It.IsAny<string>(), It.IsAny<int>(), NotificationReferenceType.Dispute), Times.Once);
        }


        [Fact]
        public async Task SubmitResponseAsync_WhenDeadlinePassed_ThrowsInvalidOperation()
        {
            var dispute = new Dispute
            {
                Id = 10,
                ResponseDeadline = DateTime.UtcNow.AddHours(-1), 
                Status = DisputeStatus.AwaitingResponse,
                FiledById = "o1",
                Loan = new Loan { BorrowerId = "b1", Item = new Item { OwnerId = "o1" } }
            };
            _disputeRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(dispute);

            var dto = new DisputeDTO.DisputeResponseDTO { ResponseDescription = "My side" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitResponseAsync(10, "b1", dto));
        }

        [Fact]
        public async Task SubmitResponseAsync_ValidResponse_MovesStatusToUnderReview()
        {
            var dispute = new Dispute
            {
                Id = 10,
                ResponseDeadline = DateTime.UtcNow.AddHours(24),
                Status = DisputeStatus.AwaitingResponse,
                FiledById = "o1",
                Loan = new Loan { BorrowerId = "b1", Item = new Item { OwnerId = "o1", Title = "Item" } }
            };
            _disputeRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(dispute);

            await _service.SubmitResponseAsync(10, "b1", new DisputeDTO.DisputeResponseDTO { ResponseDescription = "I didn't break it" });

            Assert.Equal(DisputeStatus.UnderReview, dispute.Status);
            _disputeRepoMock.Verify(r => r.Update(dispute), Times.Once);
            _notifMock.Verify(n => n.SendAsync("o1", NotificationType.DisputeResponse, It.IsAny<string>(), 10, NotificationReferenceType.Dispute), Times.Once);
        }


        [Fact]
        public async Task IssueVerdictAsync_OwnerFavored_TriggersDamagedFine()
        {
            var dispute = new Dispute
            {
                Id = 5,
                LoanId = 1,
                FiledAs = DisputeFiledAs.AsOwner, 
                FiledById = "o1",
                Loan = new Loan { BorrowerId = "b1", Item = new Item { OwnerId = "o1", Title = "Bike" } }
            };
            _disputeRepoMock.Setup(r => r.GetByIdWithDetailsAsync(5)).ReturnsAsync(dispute);

            var verdictDto = new DisputeDTO.AdminVerdictDTO { Verdict = "OwnerFavored", AdminNote = "Clearly damaged" };

            await _service.IssueVerdictAsync(5, "admin1", verdictDto);

            _fineServiceMock.Verify(f => f.IssueDamagedFineAsync(1, 5), Times.Once);
            Assert.Equal(DisputeStatus.Resolved, dispute.Status);
        }


        [Fact]
        public async Task IssueVerdictAsync_PartialDamage_RequiresCustomAmount()
        {
            var dispute = new Dispute { Id = 5, Status = DisputeStatus.UnderReview };
            _disputeRepoMock.Setup(r => r.GetByIdWithDetailsAsync(5)).ReturnsAsync(dispute);

            var verdictDto = new DisputeDTO.AdminVerdictDTO { Verdict = "PartialDamage", CustomFineAmount = null }; // Missing amount

            await Assert.ThrowsAsync<ArgumentException>(() => _service.IssueVerdictAsync(5, "admin1", verdictDto));
        }






    }
}
