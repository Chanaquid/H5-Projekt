using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class UserRecentlyViewedControllerTests
    {
        private readonly Mock<IUserRecentlyViewedService> _recentlyViewedServiceMock;
        private readonly UserRecentlyViewedController _controller;

        public UserRecentlyViewedControllerTests()
        {
            _recentlyViewedServiceMock = new Mock<IUserRecentlyViewedService>();
            _controller = new UserRecentlyViewedController(_recentlyViewedServiceMock.Object);
            SetUser("user-1");
        }

        // ───────────────────────────────────────────
        // Helpers
        // ───────────────────────────────────────────

        private void SetUser(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private void SetNoUser()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };
        }

        private static RecentlyViewedDTO.RecentlyViewedResponseDTO MakeRecentlyViewed(
            int itemId = 1,
            string title = "Test Item") => new()
            {
                Item = new ItemDTO.ItemSummaryDTO
                {
                    Id = itemId,
                    Title = title,
                    Condition = "Good",
                    Status = "Approved",
                    PickupAddress = "123 Main St",
                    PickupLatitude = 40.7128,
                    PickupLongitude = -74.0060,
                    AvailableFrom = DateTime.UtcNow.Date,
                    AvailableUntil = DateTime.UtcNow.Date.AddDays(7),
                    PrimaryPhotoUrl = "http://example.com/photo.jpg",
                    CategoryName = "Electronics",
                    CategoryIcon = "icon.png",
                    OwnerName = "Owner User",
                    AverageRating = 4.5,
                    ReviewCount = 10,
                    IsCurrentlyOnLoan = false
                },
                ViewedAt = DateTime.UtcNow
            };

        private static T GetProperty<T>(object obj, string propertyName)
        {
            return (T)obj.GetType().GetProperty(propertyName)!.GetValue(obj)!;
        }

        // ───────────────────────────────────────────
        // GetMyRecentlyViewed
        // ───────────────────────────────────────────

        [Fact]
        public async Task GetMyRecentlyViewed_ReturnsOk_WithData()
        {
            var items = new List<RecentlyViewedDTO.RecentlyViewedResponseDTO>
            {
                MakeRecentlyViewed(101, "Item 101"),
                MakeRecentlyViewed(102, "Item 102"),
                MakeRecentlyViewed(103, "Item 103")
            };
            _recentlyViewedServiceMock
                .Setup(s => s.GetMyRecentlyViewedAsync("user-1"))
                .ReturnsAsync(items);

            var result = await _controller.GetMyRecentlyViewed();

            var ok = Assert.IsType<OkObjectResult>(result);
            var value = ok.Value!;
            Assert.Equal("user-1", GetProperty<string>(value, "userId"));
            Assert.Equal(3, GetProperty<int>(value, "count"));
            Assert.Equal(items, GetProperty<List<RecentlyViewedDTO.RecentlyViewedResponseDTO>>(value, "result"));
        }

        [Fact]
        public async Task GetMyRecentlyViewed_ReturnsOk_WithEmptyList()
        {
            _recentlyViewedServiceMock
                .Setup(s => s.GetMyRecentlyViewedAsync("user-1"))
                .ReturnsAsync(new List<RecentlyViewedDTO.RecentlyViewedResponseDTO>());

            var result = await _controller.GetMyRecentlyViewed();

            var ok = Assert.IsType<OkObjectResult>(result);
            var value = ok.Value!;
            Assert.Equal(0, GetProperty<int>(value, "count"));
        }

        [Fact]
        public async Task GetMyRecentlyViewed_NoUser_ReturnsUnauthorized()
        {
            SetNoUser();

            var result = await _controller.GetMyRecentlyViewed();

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetMyRecentlyViewed_CallsServiceWithCorrectUserId()
        {
            SetUser("specific-user");
            _recentlyViewedServiceMock
                .Setup(s => s.GetMyRecentlyViewedAsync("specific-user"))
                .ReturnsAsync(new List<RecentlyViewedDTO.RecentlyViewedResponseDTO>());

            await _controller.GetMyRecentlyViewed();

            _recentlyViewedServiceMock.Verify(s =>
                s.GetMyRecentlyViewedAsync("specific-user"), Times.Once);
        }

        [Fact]
        public async Task GetMyRecentlyViewed_ServiceThrows_ExceptionPropagates()
        {
            _recentlyViewedServiceMock
                .Setup(s => s.GetMyRecentlyViewedAsync("user-1"))
                .ThrowsAsync(new InvalidOperationException("Service error."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.GetMyRecentlyViewed());
        }

        // ───────────────────────────────────────────
        // TrackView
        // ───────────────────────────────────────────

        [Fact]
        public async Task TrackView_ReturnsOk()
        {
            _recentlyViewedServiceMock
                .Setup(s => s.TrackViewAsync("user-1", 1))
                .Returns(Task.CompletedTask);

            var result = await _controller.TrackView(1);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task TrackView_NoUser_ReturnsUnauthorized()
        {
            SetNoUser();

            var result = await _controller.TrackView(1);

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task TrackView_CallsServiceWithCorrectArguments()
        {
            SetUser("specific-user");
            _recentlyViewedServiceMock
                .Setup(s => s.TrackViewAsync("specific-user", 5))
                .Returns(Task.CompletedTask);

            await _controller.TrackView(5);

            _recentlyViewedServiceMock.Verify(s =>
                s.TrackViewAsync("specific-user", 5), Times.Once);
        }

        [Fact]
        public async Task TrackView_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            _recentlyViewedServiceMock
                .Setup(s => s.TrackViewAsync("user-1", 999))
                .ThrowsAsync(new KeyNotFoundException("Item not found"));

            var result = await _controller.TrackView(999);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var value = notFound.Value!;
            Assert.Equal("Item not found", GetProperty<string>(value, "message"));
        }

        [Fact]
        public async Task TrackView_ServiceThrows_OtherException_ExceptionPropagates()
        {
            _recentlyViewedServiceMock
                .Setup(s => s.TrackViewAsync("user-1", 1))
                .ThrowsAsync(new InvalidOperationException("Unexpected error."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.TrackView(1));
        }
    }
}