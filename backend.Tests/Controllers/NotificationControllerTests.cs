using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class NotificationControllerTests
    {
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly NotificationController _controller;

        public NotificationControllerTests()
        {
            _notificationServiceMock = new Mock<INotificationService>();
            _controller = new NotificationController(_notificationServiceMock.Object);
            SetUser("user-1");
        }

 
        private void SetUser(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private static NotificationDTO.NotificationResponseDTO MakeNotification(
            int id = 1,
            bool isRead = false) => new()
            {
                Id = id,
                Type = "LoanRequested",
                Message = "Someone wants to borrow your item.",
                IsRead = isRead,
                CreatedAt = DateTime.UtcNow
            };

        private static NotificationDTO.NotificationSummaryDTO MakeSummary(
            int unreadCount = 3) => new()
            {
                UnreadCount = unreadCount,
                Recent = new List<NotificationDTO.NotificationResponseDTO>
            {
                MakeNotification(1),
                MakeNotification(2)
            }
            };


        [Fact]
        public async Task GetSummary_ReturnsOk_WithSummary()
        {
            var summary = MakeSummary(unreadCount: 3);
            _notificationServiceMock
                .Setup(s => s.GetSummaryAsync("user-1"))
                .ReturnsAsync(summary);

            var result = await _controller.GetSummary();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<NotificationDTO.NotificationSummaryDTO>(ok.Value);
            Assert.Equal(3, returned.UnreadCount);
            Assert.Equal(2, returned.Recent.Count);
        }

        [Fact]
        public async Task GetSummary_ReturnsOk_WithZeroUnread()
        {
            var summary = new NotificationDTO.NotificationSummaryDTO
            {
                UnreadCount = 0,
                Recent = new List<NotificationDTO.NotificationResponseDTO>()
            };
            _notificationServiceMock
                .Setup(s => s.GetSummaryAsync("user-1"))
                .ReturnsAsync(summary);

            var result = await _controller.GetSummary();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<NotificationDTO.NotificationSummaryDTO>(ok.Value);
            Assert.Equal(0, returned.UnreadCount);
            Assert.Empty(returned.Recent);
        }

        [Fact]
        public async Task GetSummary_CallsServiceWithCorrectUserId()
        {
            SetUser("specific-user");
            _notificationServiceMock
                .Setup(s => s.GetSummaryAsync("specific-user"))
                .ReturnsAsync(MakeSummary());

            await _controller.GetSummary();

            _notificationServiceMock.Verify(s =>
                s.GetSummaryAsync("specific-user"), Times.Once);
        }

        [Fact]
        public async Task GetSummary_ServiceThrows_ExceptionPropagates()
        {
            _notificationServiceMock
                .Setup(s => s.GetSummaryAsync("user-1"))
                .ThrowsAsync(new InvalidOperationException("Service error."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.GetSummary());
        }


        [Fact]
        public async Task GetAll_ReturnsOk_WithNotifications()
        {
            var notifications = new List<NotificationDTO.NotificationResponseDTO>
            {
                MakeNotification(1),
                MakeNotification(2),
                MakeNotification(3, isRead: true)
            };
            _notificationServiceMock
                .Setup(s => s.GetAllAsync("user-1"))
                .ReturnsAsync(notifications);

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<NotificationDTO.NotificationResponseDTO>>(ok.Value);
            Assert.Equal(3, returned.Count);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithEmptyList()
        {
            _notificationServiceMock
                .Setup(s => s.GetAllAsync("user-1"))
                .ReturnsAsync(new List<NotificationDTO.NotificationResponseDTO>());

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Empty((List<NotificationDTO.NotificationResponseDTO>)ok.Value!);
        }

        [Fact]
        public async Task GetAll_CallsServiceWithCorrectUserId()
        {
            SetUser("specific-user");
            _notificationServiceMock
                .Setup(s => s.GetAllAsync("specific-user"))
                .ReturnsAsync(new List<NotificationDTO.NotificationResponseDTO>());

            await _controller.GetAll();

            _notificationServiceMock.Verify(s =>
                s.GetAllAsync("specific-user"), Times.Once);
        }

        [Fact]
        public async Task GetAll_ServiceThrows_ExceptionPropagates()
        {
            _notificationServiceMock
                .Setup(s => s.GetAllAsync("user-1"))
                .ThrowsAsync(new InvalidOperationException("Service error."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.GetAll());
        }

        [Fact]
        public async Task MarkAsRead_ReturnsNoContent()
        {
            _notificationServiceMock
                .Setup(s => s.MarkAsReadAsync(1, "user-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.MarkAsRead(1);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task MarkAsRead_CallsServiceWithCorrectArguments()
        {
            SetUser("specific-user");
            _notificationServiceMock
                .Setup(s => s.MarkAsReadAsync(5, "specific-user"))
                .Returns(Task.CompletedTask);

            await _controller.MarkAsRead(5);

            _notificationServiceMock.Verify(s =>
                s.MarkAsReadAsync(5, "specific-user"), Times.Once);
        }

        [Fact]
        public async Task MarkAsRead_ServiceThrows_KeyNotFound_ExceptionPropagates()
        {
            _notificationServiceMock
                .Setup(s => s.MarkAsReadAsync(999, "user-1"))
                .ThrowsAsync(new KeyNotFoundException("Notification 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.MarkAsRead(999));
        }

        [Fact]
        public async Task MarkAsRead_ServiceThrows_Unauthorized_ExceptionPropagates()
        {
            _notificationServiceMock
                .Setup(s => s.MarkAsReadAsync(1, "user-1"))
                .ThrowsAsync(new UnauthorizedAccessException("Not your notification."));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _controller.MarkAsRead(1));
        }

 
        [Fact]
        public async Task MarkAllAsRead_ReturnsNoContent()
        {
            _notificationServiceMock
                .Setup(s => s.MarkAllAsReadAsync("user-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.MarkAllAsRead();

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task MarkAllAsRead_CallsServiceWithCorrectUserId()
        {
            SetUser("specific-user");
            _notificationServiceMock
                .Setup(s => s.MarkAllAsReadAsync("specific-user"))
                .Returns(Task.CompletedTask);

            await _controller.MarkAllAsRead();

            _notificationServiceMock.Verify(s =>
                s.MarkAllAsReadAsync("specific-user"), Times.Once);
        }

        [Fact]
        public async Task MarkAllAsRead_ServiceThrows_ExceptionPropagates()
        {
            _notificationServiceMock
                .Setup(s => s.MarkAllAsReadAsync("user-1"))
                .ThrowsAsync(new InvalidOperationException("Service error."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.MarkAllAsRead());
        }
    }
}