using backend.Hubs;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static backend.DTOs.ChatDTO;

namespace backend.Tests.Services
{
    public class DirectMessageServiceTests
    {
        private readonly Mock<IDirectMessageRepository> _mockDMRepo = new();
        private readonly Mock<IUserBlockRepository> _mockBlockRepo = new();
        private readonly Mock<INotificationService> _mockNotification = new();
        private readonly Mock<IHubContext<ChatHub>> _mockHubContext = new();
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly DirectMessageService _service;

        public DirectMessageServiceTests()
        {
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _service = new DirectMessageService(
                _mockDMRepo.Object,
                _mockBlockRepo.Object,
                _mockHubContext.Object,
                _mockNotification.Object,
                _mockUserManager.Object
            );
        }


        [Fact]
        public async Task SendAsync_WhenContentEmpty_ThrowsArgumentException()
        {
            var dto = new DirectMessageDTO.SendDirectMessageDTO { RecipientUsernameOrEmail = "user", Content = "" };
            await Assert.ThrowsAsync<ArgumentException>(() => _service.SendAsync("sender1", dto));
        }

        [Fact]
        public async Task SendAsync_WhenRecipientNotFound_ThrowsKeyNotFoundException()
        {
            var dto = new DirectMessageDTO.SendDirectMessageDTO { RecipientUsernameOrEmail = "user", Content = "Hi" };
            _mockUserManager.Setup(u => u.FindByNameAsync("user")).ReturnsAsync((ApplicationUser)null);
            _mockUserManager.Setup(u => u.FindByEmailAsync("user")).ReturnsAsync((ApplicationUser)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.SendAsync("sender1", dto));
        }

        [Fact]
        public async Task SendAsync_WhenSenderEqualsRecipient_ThrowsArgumentException()
        {
            var dto = new DirectMessageDTO.SendDirectMessageDTO { RecipientUsernameOrEmail = "user", Content = "Hi" };
            var user = new ApplicationUser { Id = "sender1" };
            _mockUserManager.Setup(u => u.FindByNameAsync("user")).ReturnsAsync(user);

            await Assert.ThrowsAsync<ArgumentException>(() => _service.SendAsync("sender1", dto));
        }


        [Fact]
        public async Task SendAsync_WhenBlocked_ThrowsInvalidOperationException()
        {
            var dto = new DirectMessageDTO.SendDirectMessageDTO { RecipientUsernameOrEmail = "user", Content = "Hi" };
            var recipient = new ApplicationUser { Id = "r1" };
            _mockUserManager.Setup(u => u.FindByNameAsync("user")).ReturnsAsync(recipient);
            _mockBlockRepo.Setup(b => b.IsBlockedAsync("sender1", "r1")).ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendAsync("sender1", dto));
        }


        [Fact]
        public async Task SendAsync_WhenConversationDoesNotExist_CreatesConversationAndSendsMessage()
        {
            var dto = new DirectMessageDTO.SendDirectMessageDTO { RecipientUsernameOrEmail = "user", Content = "Hello" };
            var recipient = new ApplicationUser { Id = "r1", FullName = "Recipient" };

            _mockUserManager.Setup(u => u.FindByNameAsync("user")).ReturnsAsync(recipient);
            _mockBlockRepo.Setup(b => b.IsBlockedAsync("sender1", "r1")).ReturnsAsync(false);
            _mockDMRepo.Setup(r => r.GetConversationAsync("sender1", "r1")).ReturnsAsync((DirectConversation)null);

            var clientsMock = new Mock<IHubClients>();
            var clientProxy = new Mock<IClientProxy>();
            clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxy.Object);
            _mockHubContext.Setup(h => h.Clients).Returns(clientsMock.Object);

            var result = await _service.SendAsync("sender1", dto);

            Assert.NotNull(result);
            _mockDMRepo.Verify(r => r.AddConversationAsync(It.IsAny<DirectConversation>()), Times.Once);
            _mockDMRepo.Verify(r => r.AddMessageAsync(It.IsAny<DirectMessage>()), Times.Once);
            _mockDMRepo.Verify(r => r.SaveChangesAsync(), Times.AtLeastOnce);
            clientProxy.Verify(
                c => c.SendCoreAsync(
                    It.IsAny<string>(),
                    It.Is<object[]>(o => o.Length >= 1), //SignalR wraps arguments in an object array
                    default),
                Times.AtLeastOnce); _mockNotification.Verify(n => n.SendAsync("r1", It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<NotificationReferenceType>()), Times.Once);
        }

        [Fact]
        public async Task GetInboxAsync_ReturnsConversationSummaries()
        {
            var userId = "u1";
            var conversation = new DirectConversation
            {
                Id = 1,
                InitiatedById = userId,
                OtherUserId = "u2",
                InitiatedBy = new ApplicationUser { Id = userId, FullName = "Sender" },
                OtherUser = new ApplicationUser { Id = "u2", FullName = "Recipient" },
                CreatedAt = DateTime.UtcNow
            };

            _mockDMRepo.Setup(r => r.GetConversationsByUserIdAsync(userId)).ReturnsAsync(new List<DirectConversation> { conversation });
            _mockDMRepo.Setup(r => r.GetMessagesAsync(1, null)).ReturnsAsync(new List<DirectMessage>());

            var result = await _service.GetInboxAsync(userId);

            Assert.Single(result);
            Assert.Equal("Recipient", result[0].OtherUserName);
        }

        [Fact]
        public async Task GetThreadAsync_WhenConversationNotFound_ThrowsKeyNotFoundException()
        {
            _mockDMRepo.Setup(r => r.GetConversationByIdAsync(1)).ReturnsAsync((DirectConversation)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetThreadAsync(1, "u1"));
        }

        [Fact]
        public async Task GetThreadAsync_WhenUserNotParticipant_ThrowsUnauthorizedAccessException()
        {
            _mockDMRepo.Setup(r => r.GetConversationByIdAsync(1)).ReturnsAsync(new DirectConversation
            {
                Id = 1,
                InitiatedById = "u2",
                OtherUserId = "u3"
            });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetThreadAsync(1, "u1"));
        }

        [Fact]
        public async Task GetThreadAsync_ConversationIdNotFound_ThrowsKeyNotFoundException()
        {
            var conversationId = 1;


            _mockDMRepo.Setup(r => r.GetConversationByIdAsync(conversationId)).ReturnsAsync((DirectConversation)null);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetThreadAsync(conversationId, "userId"));

            Assert.Equal($"Conversation {conversationId} not found.", ex.Message);
            _mockDMRepo.Verify(r => r.GetMessagesAsync(conversationId, It.IsAny<DateTime>()), Times.Never);
            _mockDMRepo.Verify(r => r.SaveChangesAsync(), Times.Never);




        }


        [Fact]
        public async Task MarkAsReadAsync_WhenNoUnreadMessages_DoesNothing()
        {
            var conversation = new DirectConversation { Id = 1, InitiatedById = "u1", OtherUserId = "u2" };
            _mockDMRepo.Setup(r => r.GetConversationByIdAsync(1)).ReturnsAsync(conversation);
            _mockDMRepo.Setup(r => r.GetMessagesAsync(1, null)).ReturnsAsync(new List<DirectMessage>());

            await _service.MarkAsReadAsync(1, "u1");

            _mockDMRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task HideConversationAsync_WhenUserIsInitiator_SetsHiddenAndDeletedAt()
        {
            var conversation = new DirectConversation
            {
                Id = 1,
                InitiatedById = "u1",
                OtherUserId = "u2"
            };

            _mockDMRepo.Setup(r => r.GetConversationByIdAsync(1)).ReturnsAsync(conversation);

            await _service.HideConversationAsync(1, "u1");

            Assert.True(conversation.HiddenForInitiator);
            Assert.NotNull(conversation.InitiatorDeletedAt);
            _mockDMRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task IsParticipantAsync_CallsRepository()
        {
            _mockDMRepo.Setup(r => r.IsParticipantAsync(1, "u1")).ReturnsAsync(true);
            var result = await _service.IsParticipantAsync(1, "u1");
            Assert.True(result);
        }








    }

}
