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
using static backend.DTOs.ChatDTO;

namespace backend.Tests.Services
{
    public class SupportChatServiceTests
    {
        private readonly Mock<ISupportChatRepository> _repoMock;
        private readonly Mock<INotificationService> _notificationMock;
        private readonly SupportChatService _service;

        public SupportChatServiceTests()
        {
            _repoMock = new Mock<ISupportChatRepository>();
            _notificationMock = new Mock<INotificationService>();
            _service = new SupportChatService(_repoMock.Object, _notificationMock.Object);
        }

        [Fact]
        public async Task CreateThreadAsync_CreatesThreadAndMessage()
        {
            var dto = new SupportChatDTO.CreateSupportThreadDTO { InitialMessage = "Help me!" };
            var thread = MakeThread(1, "user-1");
            thread.Messages = new List<SupportMessage> { MakeMessage(1, 1, "user-1") };

            _repoMock.Setup(r => r.AddThreadAsync(It.IsAny<SupportThread>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.AddMessageAsync(It.IsAny<SupportMessage>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(It.IsAny<int>())).ReturnsAsync(thread);

            var result = await _service.CreateThreadAsync("user-1", dto);

            result.Should().NotBeNull();
            result.UserId.Should().Be("user-1");
            _repoMock.Verify(r => r.AddThreadAsync(It.IsAny<SupportThread>()), Times.Once);
            _repoMock.Verify(r => r.AddMessageAsync(It.IsAny<SupportMessage>()), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task CreateThreadAsync_InitialMessageIsTrimmed()
        {
            var dto = new SupportChatDTO.CreateSupportThreadDTO { InitialMessage = "  Help me!  " };
            var thread = MakeThread(1, "user-1");
            thread.Messages = new List<SupportMessage> { MakeMessage(1, 1, "user-1") };

            SupportMessage? captured = null;
            _repoMock.Setup(r => r.AddThreadAsync(It.IsAny<SupportThread>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.AddMessageAsync(It.IsAny<SupportMessage>()))
                .Callback<SupportMessage>(m => captured = m)
                .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(It.IsAny<int>())).ReturnsAsync(thread);

            await _service.CreateThreadAsync("user-1", dto);

            captured!.Content.Should().Be("Help me!");
        }

        [Fact]
        public async Task CreateThreadAsync_ThreadStatusIsOpen()
        {
            var dto = new SupportChatDTO.CreateSupportThreadDTO { InitialMessage = "Hello" };
            var thread = MakeThread(1, "user-1");

            SupportThread? capturedThread = null;
            _repoMock.Setup(r => r.AddThreadAsync(It.IsAny<SupportThread>()))
                .Callback<SupportThread>(t => capturedThread = t)
                .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.AddMessageAsync(It.IsAny<SupportMessage>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(It.IsAny<int>())).ReturnsAsync(thread);

            await _service.CreateThreadAsync("user-1", dto);

            capturedThread!.Status.Should().Be(SupportThreadStatus.Open);
        }

        [Fact]
        public async Task SendMessageAsync_ThreadOwnerSendsMessage_Succeeds()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Claimed, "admin-1");
            thread.Messages = new List<SupportMessage> { MakeMessage(1, 1, "user-1") };
            var dto = new SupportChatDTO.SendSupportMessageDTO { SupportThreadId = 1, Content = "My question" };

            _repoMock.Setup(r => r.GetThreadByIdAsync(1)).ReturnsAsync(thread);
            _repoMock.Setup(r => r.AddMessageAsync(It.IsAny<SupportMessage>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            var result = await _service.SendMessageAsync("user-1", dto);

            result.Should().NotBeNull();
            _repoMock.Verify(r => r.AddMessageAsync(It.IsAny<SupportMessage>()), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_ClaimingAdminSendsMessage_Succeeds()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Claimed, "admin-1");
            thread.Messages = new List<SupportMessage> { MakeMessage(1, 1, "admin-1") };
            var dto = new SupportChatDTO.SendSupportMessageDTO { SupportThreadId = 1, Content = "Admin reply" };

            _repoMock.Setup(r => r.GetThreadByIdAsync(1)).ReturnsAsync(thread);
            _repoMock.Setup(r => r.AddMessageAsync(It.IsAny<SupportMessage>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            var result = await _service.SendMessageAsync("admin-1", dto);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task SendMessageAsync_ThreadNotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetThreadByIdAsync(99)).ReturnsAsync((SupportThread?)null);
            var dto = new SupportChatDTO.SendSupportMessageDTO { SupportThreadId = 99, Content = "Hi" };

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.SendMessageAsync("user-1", dto));
        }

        [Fact]
        public async Task SendMessageAsync_ClosedThread_ThrowsInvalidOperationException()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Closed);
            _repoMock.Setup(r => r.GetThreadByIdAsync(1)).ReturnsAsync(thread);
            var dto = new SupportChatDTO.SendSupportMessageDTO { SupportThreadId = 1, Content = "Hi" };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SendMessageAsync("user-1", dto));
        }

        [Fact]
        public async Task SendMessageAsync_UnrelatedUser_ThrowsUnauthorizedAccessException()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Claimed, "admin-1");
            _repoMock.Setup(r => r.GetThreadByIdAsync(1)).ReturnsAsync(thread);
            var dto = new SupportChatDTO.SendSupportMessageDTO { SupportThreadId = 1, Content = "Hi" };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.SendMessageAsync("stranger", dto));
        }

        [Fact]
        public async Task SendMessageAsync_ContentIsTrimmed()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Open);
            thread.Messages = new List<SupportMessage> { MakeMessage(1, 1, "user-1") };
            var dto = new SupportChatDTO.SendSupportMessageDTO { SupportThreadId = 1, Content = "  Hello  " };

            SupportMessage? captured = null;
            _repoMock.Setup(r => r.GetThreadByIdAsync(1)).ReturnsAsync(thread);
            _repoMock.Setup(r => r.AddMessageAsync(It.IsAny<SupportMessage>()))
                .Callback<SupportMessage>(m => captured = m)
                .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await _service.SendMessageAsync("user-1", dto);

            captured!.Content.Should().Be("Hello");
        }

        [Fact]
        public async Task SendMessageAsync_WhenAdminClaimed_NotifiesUser()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Claimed, "admin-1");
            thread.Messages = new List<SupportMessage> { MakeMessage(1, 1, "admin-1") };
            var dto = new SupportChatDTO.SendSupportMessageDTO { SupportThreadId = 1, Content = "Admin reply" };

            _repoMock.Setup(r => r.GetThreadByIdAsync(1)).ReturnsAsync(thread);
            _repoMock.Setup(r => r.AddMessageAsync(It.IsAny<SupportMessage>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await _service.SendMessageAsync("admin-1", dto);

            _notificationMock.Verify(n => n.SendAsync(
                "user-1",
                NotificationType.SupportMessageReceived,
                It.IsAny<string>(),
                1,
                NotificationReferenceType.SupportThread), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WhenNoAdminClaimed_DoesNotNotify()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Open, claimedByAdminId: null);
            thread.Messages = new List<SupportMessage> { MakeMessage(1, 1, "user-1") };
            var dto = new SupportChatDTO.SendSupportMessageDTO { SupportThreadId = 1, Content = "Hi" };

            _repoMock.Setup(r => r.GetThreadByIdAsync(1)).ReturnsAsync(thread);
            _repoMock.Setup(r => r.AddMessageAsync(It.IsAny<SupportMessage>())).Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await _service.SendMessageAsync("user-1", dto);

            _notificationMock.Verify(n => n.SendAsync(
                It.IsAny<string>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<NotificationReferenceType>()), Times.Never);
        }


        [Fact]
        public async Task GetThreadAsync_OwnerCanView_ReturnsDTO()
        {
            var thread = MakeThread(1, "user-1");
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            var result = await _service.GetThreadAsync(1, "user-1", isAdmin: false);

            result.Should().NotBeNull();
            _repoMock.Verify(r => r.MarkMessagesReadAsync(1, "user-1"), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetThreadAsync_AdminCanViewAnyThread()
        {
            var thread = MakeThread(1, "user-1");
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            var result = await _service.GetThreadAsync(1, "admin-1", isAdmin: true);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetThreadAsync_NonOwnerNonAdmin_ThrowsUnauthorizedAccessException()
        {
            var thread = MakeThread(1, "user-1");
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetThreadAsync(1, "stranger", isAdmin: false));
        }

        [Fact]
        public async Task GetThreadAsync_ThreadNotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(99)).ReturnsAsync((SupportThread?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetThreadAsync(99, "user-1", isAdmin: false));
        }

        [Fact]
        public async Task GetMyThreadsAsync_ReturnsMappedSummaries()
        {
            var threads = new List<SupportThread>
            {
                MakeThread(1, "user-1"),
                MakeThread(2, "user-1")
            };
            _repoMock.Setup(r => r.GetThreadsByUserIdAsync("user-1")).ReturnsAsync(threads);

            var result = await _service.GetMyThreadsAsync("user-1");

            result.Should().HaveCount(2);
            result.All(t => t.UserId == "user-1").Should().BeTrue();
        }

        [Fact]
        public async Task GetMyThreadsAsync_NoThreads_ReturnsEmptyList()
        {
            _repoMock.Setup(r => r.GetThreadsByUserIdAsync("user-1")).ReturnsAsync(new List<SupportThread>());

            var result = await _service.GetMyThreadsAsync("user-1");

            result.Should().BeEmpty();
        }


        [Fact]
        public async Task ClaimThreadAsync_OpenThread_SetsClaimedStatus()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Open);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await _service.ClaimThreadAsync(1, "admin-1");

            thread.Status.Should().Be(SupportThreadStatus.Claimed);
            thread.ClaimedByAdminId.Should().Be("admin-1");
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ClaimThreadAsync_NotifiesThreadOwner()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Open);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await _service.ClaimThreadAsync(1, "admin-1");

            _notificationMock.Verify(n => n.SendAsync(
                "user-1",
                NotificationType.SupportMessageReceived,
                It.IsAny<string>(),
                1,
                NotificationReferenceType.SupportThread), Times.Once);
        }

        [Fact]
        public async Task ClaimThreadAsync_ClosedThread_ThrowsInvalidOperationException()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Closed);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ClaimThreadAsync(1, "admin-1"));
        }

        [Fact]
        public async Task ClaimThreadAsync_AlreadyClaimedByAnotherAdmin_ThrowsInvalidOperationException()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Claimed, "admin-1");
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ClaimThreadAsync(1, "admin-2"));
        }

        [Fact]
        public async Task ClaimThreadAsync_SameAdminReclaims_Succeeds()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Claimed, "admin-1");
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await _service.ClaimThreadAsync(1, "admin-1");

            thread.ClaimedByAdminId.Should().Be("admin-1");
        }

        [Fact]
        public async Task ClaimThreadAsync_ThreadNotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(99)).ReturnsAsync((SupportThread?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.ClaimThreadAsync(99, "admin-1"));
        }

        [Fact]
        public async Task CloseThreadAsync_OpenThread_SetsClosedStatus()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Open);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await _service.CloseThreadAsync(1, "admin-1");

            thread.Status.Should().Be(SupportThreadStatus.Closed);
            thread.ClosedAt.Should().NotBeNull();
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CloseThreadAsync_NotifiesThreadOwner()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Open);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await _service.CloseThreadAsync(1, "admin-1");

            _notificationMock.Verify(n => n.SendAsync(
                "user-1",
                NotificationType.SupportMessageReceived,
                It.IsAny<string>(),
                1,
                NotificationReferenceType.SupportThread), Times.Once);
        }

        [Fact]
        public async Task CloseThreadAsync_AlreadyClosed_ThrowsInvalidOperationException()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Closed);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CloseThreadAsync(1, "admin-1"));
        }

        [Fact]
        public async Task CloseThreadAsync_ThreadNotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(99)).ReturnsAsync((SupportThread?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CloseThreadAsync(99, "admin-1"));
        }


        [Fact]
        public async Task ReopenThreadAsync_WithPreviousAdmin_ReopensAsClaimed()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Closed, "admin-1");
            thread.ClosedAt = DateTime.UtcNow;
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await _service.ReopenThreadAsync(1, "admin-1");

            thread.Status.Should().Be(SupportThreadStatus.Claimed);
            thread.ClosedAt.Should().BeNull();
        }

        [Fact]
        public async Task ReopenThreadAsync_WithoutPreviousAdmin_ReopensAsOpen()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Closed, claimedByAdminId: null);
            thread.ClosedAt = DateTime.UtcNow;
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await _service.ReopenThreadAsync(1, "admin-1");

            thread.Status.Should().Be(SupportThreadStatus.Open);
            thread.ClosedAt.Should().BeNull();
        }

        [Fact]
        public async Task ReopenThreadAsync_NotifiesThreadOwner()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Closed);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await _service.ReopenThreadAsync(1, "admin-1");

            _notificationMock.Verify(n => n.SendAsync(
                "user-1",
                NotificationType.SupportMessageReceived,
                It.IsAny<string>(),
                1,
                NotificationReferenceType.SupportThread), Times.Once);
        }

        [Fact]
        public async Task ReopenThreadAsync_NotClosedThread_ThrowsInvalidOperationException()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Open);
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(1)).ReturnsAsync(thread);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ReopenThreadAsync(1, "admin-1"));
        }

        [Fact]
        public async Task ReopenThreadAsync_ThreadNotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetThreadWithMessagesAsync(99)).ReturnsAsync((SupportThread?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.ReopenThreadAsync(99, "admin-1"));
        }


        [Fact]
        public async Task MarkReadAsync_ThreadOwner_MarksAndSaves()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Open);
            _repoMock.Setup(r => r.GetThreadByIdAsync(1)).ReturnsAsync(thread);

            await _service.MarkReadAsync(1, "user-1");

            _repoMock.Verify(r => r.MarkMessagesReadAsync(1, "user-1"), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task MarkReadAsync_ClaimingAdmin_MarksAndSaves()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Claimed, "admin-1");
            _repoMock.Setup(r => r.GetThreadByIdAsync(1)).ReturnsAsync(thread);

            await _service.MarkReadAsync(1, "admin-1");

            _repoMock.Verify(r => r.MarkMessagesReadAsync(1, "admin-1"), Times.Once);
        }

        [Fact]
        public async Task MarkReadAsync_UnrelatedUser_ThrowsUnauthorizedAccessException()
        {
            var thread = MakeThread(1, "user-1", SupportThreadStatus.Claimed, "admin-1");
            _repoMock.Setup(r => r.GetThreadByIdAsync(1)).ReturnsAsync(thread);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.MarkReadAsync(1, "stranger"));
        }

        [Fact]
        public async Task MarkReadAsync_ThreadNotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetThreadByIdAsync(99)).ReturnsAsync((SupportThread?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.MarkReadAsync(99, "user-1"));
        }
    


        //Helpers
        private static SupportThread MakeThread(
            int id,
            string userId,
            SupportThreadStatus status = SupportThreadStatus.Open,
            string? claimedByAdminId = null) => new()
            {
                Id = id,
                UserId = userId,
                Status = status,
                ClaimedByAdminId = claimedByAdminId,
                CreatedAt = DateTime.UtcNow,
                User = new ApplicationUser { Id = userId, FullName = "Test User" },
                Messages = new List<SupportMessage>()
            };

        private static SupportMessage MakeMessage(int id, int threadId, string senderId, bool isRead = false) => new()
        {
            Id = id,
            SupportThreadId = threadId,
            SenderId = senderId,
            Content = $"Message {id}",
            IsRead = isRead,
            SentAt = DateTime.UtcNow,
            Sender = new ApplicationUser { FullName = "Sender Name" }
        };



    }
}
