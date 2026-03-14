using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class UserFavoriteControllerTests
    {
        private readonly Mock<IUserFavoriteService> _favoriteServiceMock;
        private readonly UserFavoriteController _controller;

        public UserFavoriteControllerTests()
        {
            _favoriteServiceMock = new Mock<IUserFavoriteService>();
            _controller = new UserFavoriteController(_favoriteServiceMock.Object);
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

        private static FavoriteDTO.FavoriteResponseDTO MakeFavoriteResponse(
            int itemId = 1,
            bool notifyWhenAvailable = false) => new()
            {
                Item = new ItemDTO.ItemSummaryDTO
                {
                    Id = itemId,
                    Title = "Drill",
                    Condition = "Good",
                    Status = "Approved",
                    CategoryName = "Tools",
                    OwnerName = "Owner"
                },
                NotifyWhenAvailable = notifyWhenAvailable,
                SavedAt = DateTime.UtcNow
            };

        [Fact]
        public async Task GetMyFavorites_ReturnsOk_WithFavorites()
        {
            var favorites = new List<FavoriteDTO.FavoriteResponseDTO>
            {
                MakeFavoriteResponse(1),
                MakeFavoriteResponse(2)
            };
            _favoriteServiceMock
                .Setup(s => s.GetMyFavoritesAsync("user-1"))
                .ReturnsAsync(favorites);

            var result = await _controller.GetMyFavorites();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<FavoriteDTO.FavoriteResponseDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetMyFavorites_ReturnsOk_WithEmptyList()
        {
            _favoriteServiceMock
                .Setup(s => s.GetMyFavoritesAsync("user-1"))
                .ReturnsAsync(new List<FavoriteDTO.FavoriteResponseDTO>());

            var result = await _controller.GetMyFavorites();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Empty((List<FavoriteDTO.FavoriteResponseDTO>)ok.Value!);
        }

        [Fact]
        public async Task GetMyFavorites_NoUser_ReturnsUnauthorized()
        {
            SetNoUser();

            var result = await _controller.GetMyFavorites();

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetMyFavorites_CallsServiceWithCorrectUserId()
        {
            SetUser("specific-user");
            _favoriteServiceMock
                .Setup(s => s.GetMyFavoritesAsync("specific-user"))
                .ReturnsAsync(new List<FavoriteDTO.FavoriteResponseDTO>());

            await _controller.GetMyFavorites();

            _favoriteServiceMock.Verify(s =>
                s.GetMyFavoritesAsync("specific-user"), Times.Once);
        }

        [Fact]
        public async Task GetMyFavorites_ServiceThrows_ExceptionPropagates()
        {
            _favoriteServiceMock
                .Setup(s => s.GetMyFavoritesAsync("user-1"))
                .ThrowsAsync(new InvalidOperationException("Service error."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.GetMyFavorites());
        }

        [Fact]
        public async Task AddFavorite_ReturnsOk_WithMessage()
        {
            _favoriteServiceMock
                .Setup(s => s.AddAsync("user-1", 1))
                .Returns(Task.CompletedTask);

            var result = await _controller.AddFavorite(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AddFavorite_NoUser_ReturnsUnauthorized()
        {
            SetNoUser();

            var result = await _controller.AddFavorite(1);

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task AddFavorite_CallsServiceWithCorrectArguments()
        {
            SetUser("specific-user");
            _favoriteServiceMock
                .Setup(s => s.AddAsync("specific-user", 5))
                .Returns(Task.CompletedTask);

            await _controller.AddFavorite(5);

            _favoriteServiceMock.Verify(s =>
                s.AddAsync("specific-user", 5), Times.Once);
        }

        [Fact]
        public async Task AddFavorite_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            _favoriteServiceMock
                .Setup(s => s.AddAsync("user-1", 999))
                .ThrowsAsync(new KeyNotFoundException("Item 999 not found."));

            var result = await _controller.AddFavorite(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AddFavorite_ServiceThrows_InvalidOperation_ReturnsConflict()
        {
            _favoriteServiceMock
                .Setup(s => s.AddAsync("user-1", 1))
                .ThrowsAsync(new InvalidOperationException("Item is already in your favorites."));

            var result = await _controller.AddFavorite(1);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task RemoveFavorite_ReturnsOk_WithMessage()
        {
            _favoriteServiceMock
                .Setup(s => s.RemoveAsync("user-1", 1))
                .Returns(Task.CompletedTask);

            var result = await _controller.RemoveFavorite(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task RemoveFavorite_NoUser_ReturnsUnauthorized()
        {
            SetNoUser();

            var result = await _controller.RemoveFavorite(1);

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task RemoveFavorite_CallsServiceWithCorrectArguments()
        {
            SetUser("specific-user");
            _favoriteServiceMock
                .Setup(s => s.RemoveAsync("specific-user", 3))
                .Returns(Task.CompletedTask);

            await _controller.RemoveFavorite(3);

            _favoriteServiceMock.Verify(s =>
                s.RemoveAsync("specific-user", 3), Times.Once);
        }

        [Fact]
        public async Task RemoveFavorite_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            _favoriteServiceMock
                .Setup(s => s.RemoveAsync("user-1", 999))
                .ThrowsAsync(new KeyNotFoundException("Favorite not found."));

            var result = await _controller.RemoveFavorite(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ToggleNotify_Enable_ReturnsOk_WithUpdatedFavorite()
        {
            var dto = new FavoriteDTO.ToggleNotifyDTO { NotifyWhenAvailable = true };
            var response = MakeFavoriteResponse(1, notifyWhenAvailable: true);
            _favoriteServiceMock
                .Setup(s => s.ToggleNotifyAsync("user-1", 1, true))
                .ReturnsAsync(response);

            var result = await _controller.ToggleNotify(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<FavoriteDTO.FavoriteResponseDTO>(ok.Value);
            Assert.True(returned.NotifyWhenAvailable);
        }

        [Fact]
        public async Task ToggleNotify_Disable_ReturnsOk_WithUpdatedFavorite()
        {
            var dto = new FavoriteDTO.ToggleNotifyDTO { NotifyWhenAvailable = false };
            var response = MakeFavoriteResponse(1, notifyWhenAvailable: false);
            _favoriteServiceMock
                .Setup(s => s.ToggleNotifyAsync("user-1", 1, false))
                .ReturnsAsync(response);

            var result = await _controller.ToggleNotify(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<FavoriteDTO.FavoriteResponseDTO>(ok.Value);
            Assert.False(returned.NotifyWhenAvailable);
        }

        [Fact]
        public async Task ToggleNotify_NoUser_ReturnsUnauthorized()
        {
            SetNoUser();
            var dto = new FavoriteDTO.ToggleNotifyDTO { NotifyWhenAvailable = true };

            var result = await _controller.ToggleNotify(1, dto);

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task ToggleNotify_CallsServiceWithCorrectArguments()
        {
            SetUser("specific-user");
            var dto = new FavoriteDTO.ToggleNotifyDTO { NotifyWhenAvailable = true };
            _favoriteServiceMock
                .Setup(s => s.ToggleNotifyAsync("specific-user", 5, true))
                .ReturnsAsync(MakeFavoriteResponse(5, true));

            await _controller.ToggleNotify(5, dto);

            _favoriteServiceMock.Verify(s =>
                s.ToggleNotifyAsync("specific-user", 5, true), Times.Once);
        }

        [Fact]
        public async Task ToggleNotify_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            var dto = new FavoriteDTO.ToggleNotifyDTO { NotifyWhenAvailable = true };
            _favoriteServiceMock
                .Setup(s => s.ToggleNotifyAsync("user-1", 999, true))
                .ThrowsAsync(new KeyNotFoundException("Favorite not found."));

            var result = await _controller.ToggleNotify(999, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}