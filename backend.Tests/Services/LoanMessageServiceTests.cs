using backend.DTOs;
using backend.Hubs;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;


namespace backend.Tests.Services
{
    public class LoanMessageServiceTests
    {
        private readonly Mock<ILoanMessageRepository> _messageRepoMock;
        private readonly Mock<ILoanRepository> _loanRepoMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IHubContext<ChatHub>> _hubContextMock;
        private readonly Mock<IOnlineTracker> _onlineTrackerMock;
        private readonly LoanMessageService _service;

        public LoanMessageServiceTests()
        {
            _messageRepoMock = new Mock<ILoanMessageRepository>();
            _loanRepoMock = new Mock<ILoanRepository>();
            _notificationServiceMock = new Mock<INotificationService>();
            _hubContextMock = new Mock<IHubContext<ChatHub>>();
            _onlineTrackerMock = new Mock<IOnlineTracker>();

            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

            _service = new LoanMessageService(
                _messageRepoMock.Object,
                _loanRepoMock.Object,
                _hubContextMock.Object,
                _notificationServiceMock.Object,
                _onlineTrackerMock.Object);
        }


        [Fact]
        public async Task SendAsync_ValidParty_SavesAndPushesMessage()
        {
            var senderId = "user-borrower";
            var loan = new Loan
            {
                Id = 1,
                BorrowerId = senderId,
                Item = new Item { OwnerId = "user-owner", Title = "Camera" }
            };
            var dto = new ChatDTO.LoanMessageDTO.SendLoanMessageDTO { LoanId = 1, Content = "Hello!" };

            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _onlineTrackerMock.Setup(x => x.IsUserInLoanGroup(It.IsAny<string>(), 1)).Returns(true); 

            var result = await _service.SendAsync(senderId, dto);

            result.Content.Should().Be("Hello!");
            _messageRepoMock.Verify(x => x.AddAsync(It.IsAny<LoanMessage>()), Times.Once);
            _hubContextMock.Verify(x => x.Clients.Group("loan_1"), Times.Once);
        }


        [Fact]
        public async Task SendAsync_RecipientOffline_SendsNotificationAndToast()
        {
            var senderId = "user-owner";
            var loan = new Loan
            {
                Id = 1,
                BorrowerId = "user-borrower",
                Item = new Item { OwnerId = senderId, Title = "Camera" }
            };
            var dto = new ChatDTO.LoanMessageDTO.SendLoanMessageDTO { LoanId = 1, Content = "Pick it up tomorrow" };

            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _onlineTrackerMock.Setup(x => x.IsUserInLoanGroup("user-borrower", 1)).Returns(false); 

            await _service.SendAsync(senderId, dto);

            _notificationServiceMock.Verify(x => x.SendAsync(
                "user-borrower",
                NotificationType.MessageReceived,
                It.IsAny<string>(),
                1,
                NotificationReferenceType.Loan), Times.Once);

            _hubContextMock.Verify(x => x.Clients.Group("user_user-borrower"), Times.Once); 
        }


        [Fact]
        public async Task SendAsync_StrangerTriesToMessage_ThrowsUnauthorized()
        {
            var loan = new Loan
            {
                Id = 1,
                BorrowerId = "b1",
                Item = new Item { OwnerId = "o1" }
            };
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.SendAsync("stranger-id", new ChatDTO.LoanMessageDTO.SendLoanMessageDTO { LoanId = 1, Content = "Hey" }));
        }


        [Fact]
        public async Task GetThreadAsync_MarksIncomingMessagesAsRead()
        {
            var userId = "user-borrower";
            var otherUserId = "owner-id";

            var borrowerUser = new ApplicationUser { FullName = "Borrower Name" };
            var ownerUser = new ApplicationUser { FullName = "Owner Name" };

            var loan = new Loan
            {
                Id = 1,
                BorrowerId = userId,
                Borrower = borrowerUser,
                Item = new Item
                {
                    OwnerId = otherUserId,
                    Owner = ownerUser, 
                    Title = "Camera"
                }
            };

            var messages = new List<LoanMessage> {
                new LoanMessage {
                    SenderId = otherUserId,
                    Sender = ownerUser, 
                    IsRead = false
                },
                new LoanMessage {
                    SenderId = userId,
                    Sender = borrowerUser, 
                    IsRead = false
                }
            };

            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _messageRepoMock.Setup(x => x.GetByLoanIdAsync(1)).ReturnsAsync(messages);

            await _service.GetThreadAsync(1, userId);

            messages[0].IsRead.Should().BeTrue();
            messages[1].IsRead.Should().BeFalse(); 
            _messageRepoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }


        [Fact]
        public async Task MarkThreadAsReadAsync_PushesSignalRReceipt()
        {
            var userId = "u1";
            var loan = new Loan
            {
                Id = 1,
                BorrowerId = userId,
                Item = new Item { OwnerId = "u2" }
            };
            var unreadMessages = new List<LoanMessage> {
                new LoanMessage { SenderId = "u2", IsRead = false }
            };

            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _messageRepoMock.Setup(x => x.GetByLoanIdAsync(1)).ReturnsAsync(unreadMessages);

            await _service.MarkThreadAsReadAsync(1, userId);


            _hubContextMock.Verify(x => x.Clients.Group("loan_1")
                .SendCoreAsync(
                    "MessagesRead",
                    It.Is<object[]>(o => o.Length == 1), 
                    default),
                Times.Once);
        }


        [Theory]
        [InlineData("owner-id", true)]
        [InlineData("borrower-id", true)]
        [InlineData("hacker-id", false)]
        public async Task IsPartyToLoanAsync_ValidatesCorrectly(string testUserId, bool expected)
        {
            var loan = new Loan { BorrowerId = "borrower-id", Item = new Item { OwnerId = "owner-id" } };
            _loanRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            var result = await _service.IsPartyToLoanAsync(1, testUserId);

            result.Should().Be(expected);
        }







    }
}
