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
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _repoMock;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _repoMock = new Mock<INotificationRepository>();
            _service = new NotificationService(_repoMock.Object);
        }

        [Fact]
        public async Task GetSummaryAsync_ReturnsCorrectUnreadCount()
        {
            var notifications = new List<Notification>
            {
                MakeNotification(1, "u1", isRead: false),
                MakeNotification(2, "u1", isRead: false),
                MakeNotification(3, "u1", isRead: true)
            };
            _repoMock.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(notifications);

            var result = await _service.GetSummaryAsync("u1");

            result.UnreadCount.Should().Be(2);
        }


        [Fact]
        public async Task GetSummaryAsync_ReturnsMax10Recent()
        {
            var notifications = Enumerable.Range(1, 15)
                .Select(i => MakeNotification(i, "u1"))
                .ToList();
            _repoMock.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(notifications);

            var result = await _service.GetSummaryAsync("u1");

            result.Recent.Should().HaveCount(10);
        }


        [Fact]
        public async Task GetSummaryAsync_WhenNoNotifications_ReturnsZeroUnreadAndEmptyList()
        {
            _repoMock.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(new List<Notification>());

            var result = await _service.GetSummaryAsync("u1");

            result.UnreadCount.Should().Be(0);
            result.Recent.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedDTOs()
        {
            var notifications = new List<Notification>
            {
                MakeNotification(1, "u1"),
                MakeNotification(2, "u1")
            };
            _repoMock.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(notifications);

            var result = await _service.GetAllAsync("u1");

            result.Should().HaveCount(2);
            result[0].Id.Should().Be(1);
            result[1].Id.Should().Be(2);
        }


        [Fact]
        public async Task GetAllAsync_MapsFieldsCorrectly()
        {
            var notification = new Notification
            {
                Id = 1,
                UserId = "u1",
                Type = NotificationType.LoanApproved,
                Message = "Your loan was approved.",
                ReferenceId = 42,
                ReferenceType = NotificationReferenceType.Loan,
                IsRead = false,
                CreatedAt = new DateTime(2025, 1, 1)
            };
            _repoMock.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(new List<Notification> { notification });

            var result = await _service.GetAllAsync("u1");

            var dto = result.Single();
            dto.Id.Should().Be(1);
            dto.Type.Should().Be("LoanApproved");
            dto.Message.Should().Be("Your loan was approved.");
            dto.ReferenceId.Should().Be(42);
            dto.ReferenceType.Should().Be("Loan");
            dto.IsRead.Should().BeFalse();
            dto.CreatedAt.Should().Be(new DateTime(2025, 1, 1));
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenValid_SetsIsReadTrue()
        {
            var notification = MakeNotification(1, "u1", isRead: false);
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notification);

            await _service.MarkAsReadAsync(1, "u1");

            notification.IsRead.Should().BeTrue();
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenNotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Notification?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.MarkAsReadAsync(99, "u1"));
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenBelongsToDifferentUser_ThrowsKeyNotFoundException()
        {
            var notification = MakeNotification(1, "other-user", isRead: false);
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notification);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.MarkAsReadAsync(1, "u1"));
        }

        [Fact]
        public async Task MarkAsReadAsync_WhenAlreadyRead_DoesNotCallSaveChanges()
        {
            var notification = MakeNotification(1, "u1", isRead: true);
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(notification);

            await _service.MarkAsReadAsync(1, "u1");

            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task MarkAllAsReadAsync_MarksAllUnreadNotifications()
        {
            var notifications = new List<Notification>
            {
                MakeNotification(1, "u1", isRead: false),
                MakeNotification(2, "u1", isRead: false),
                MakeNotification(3, "u1", isRead: true)
            };
            _repoMock.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(notifications);

            await _service.MarkAllAsReadAsync("u1");

            notifications.All(n => n.IsRead).Should().BeTrue();
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task MarkAllAsReadAsync_WhenNoneUnread_DoesNotCallSaveChanges()
        {
            var notifications = new List<Notification>
            {
                MakeNotification(1, "u1", isRead: true),
                MakeNotification(2, "u1", isRead: true)
            };
            _repoMock.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(notifications);

            await _service.MarkAllAsReadAsync("u1");

            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task MarkAllAsReadAsync_WhenNoNotifications_DoesNotCallSaveChanges()
        {
            _repoMock.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(new List<Notification>());

            await _service.MarkAllAsReadAsync("u1");

            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task SendAsync_AddsNotificationWithCorrectFields()
        {
            Notification? captured = null;
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Callback<Notification>(n => captured = n)
                .Returns(Task.CompletedTask);

            await _service.SendAsync("u1", NotificationType.LoanApproved, "Test message", 5, NotificationReferenceType.Loan);

            captured.Should().NotBeNull();
            captured!.UserId.Should().Be("u1");
            captured.Type.Should().Be(NotificationType.LoanApproved);
            captured.Message.Should().Be("Test message");
            captured.ReferenceId.Should().Be(5);
            captured.ReferenceType.Should().Be(NotificationReferenceType.Loan);
            captured.IsRead.Should().BeFalse();
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SendAsync_WithNoReferenceId_StillSavesSuccessfully()
        {
            Notification? captured = null;
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Callback<Notification>(n => captured = n)
                .Returns(Task.CompletedTask);

            await _service.SendAsync("u1", NotificationType.LoanApproved, "No ref message");

            captured!.ReferenceId.Should().BeNull();
            captured.ReferenceType.Should().BeNull();
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }



        //Helpers
        private static Notification MakeNotification(int id, string userId, bool isRead = false) => new()
        {
            Id = id,
            UserId = userId,
            Type = NotificationType.LoanApproved,
            Message = $"Notification {id}",
            IsRead = isRead,
            CreatedAt = DateTime.UtcNow
        };






    }
}
