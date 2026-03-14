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
    public class UserFavoriteServiceTests
    {
        private readonly Mock<IUserFavoriteRepository> _favoriteRepoMock;
        private readonly Mock<IItemRepository> _itemRepoMock;
        private readonly UserFavoriteService _service;

        public UserFavoriteServiceTests()
        {
            _favoriteRepoMock = new Mock<IUserFavoriteRepository>();
            _itemRepoMock = new Mock<IItemRepository>();
            _service = new UserFavoriteService(_favoriteRepoMock.Object, _itemRepoMock.Object);
        }

        //Helpers
        private static Item MakeItem(int id, string ownerId = "owner-1") => new()
        {
            Id = id,
            OwnerId = ownerId,
            Title = $"Item {id}",
            Condition = ItemCondition.Good,
            Status = ItemStatus.Approved,
            PickupAddress = "123 Main St",
            Owner = new ApplicationUser { Id = ownerId, FullName = "Owner Name" },
            Photos = new List<ItemPhoto>(),
            Reviews = new List<ItemReview>(),
            Loans = new List<Loan>(),
            Category = new Category { Name = "Tools", Icon = "🔧" }
        };

        private static UserFavoriteItem MakeFavorite(string userId, int itemId, bool notify = false) => new()
        {
            UserId = userId,
            ItemId = itemId,
            NotifyWhenAvailable = notify,
            SavedAt = DateTime.UtcNow,
            Item = MakeItem(itemId)
        };

        [Fact]
        public async Task GetMyFavoritesAsync_ReturnsMappedList()
        {
            var favorites = new List<UserFavoriteItem>
            {
                MakeFavorite("user-1", 1),
                MakeFavorite("user-1", 2)
            };
            _favoriteRepoMock.Setup(r => r.GetAllByUserIdAsync("user-1")).ReturnsAsync(favorites);

            var result = await _service.GetMyFavoritesAsync("user-1");

            result.Should().HaveCount(2);
            result[0].Item.Id.Should().Be(1);
            result[1].Item.Id.Should().Be(2);
        }

        [Fact]
        public async Task GetMyFavoritesAsync_NoFavorites_ReturnsEmptyList()
        {
            _favoriteRepoMock.Setup(r => r.GetAllByUserIdAsync("user-1"))
                .ReturnsAsync(new List<UserFavoriteItem>());

            var result = await _service.GetMyFavoritesAsync("user-1");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyFavoritesAsync_MapsItemFieldsCorrectly()
        {
            var item = MakeItem(1);
            item.Reviews = new List<ItemReview>
            {
                new() { Rating = 4 },
                new() { Rating = 5 }
            };
            item.Loans = new List<Loan>
            {
                new() { Status = LoanStatus.Active }
            };
            item.Photos = new List<ItemPhoto>
            {
                new() { PhotoUrl = "https://example.com/photo.jpg", IsPrimary = true }
            };

            var favorite = new UserFavoriteItem
            {
                UserId = "user-1",
                ItemId = 1,
                NotifyWhenAvailable = true,
                SavedAt = new DateTime(2025, 1, 1),
                Item = item
            };

            _favoriteRepoMock.Setup(r => r.GetAllByUserIdAsync("user-1"))
                .ReturnsAsync(new List<UserFavoriteItem> { favorite });

            var result = await _service.GetMyFavoritesAsync("user-1");

            var dto = result.Single();
            dto.NotifyWhenAvailable.Should().BeTrue();
            dto.SavedAt.Should().Be(new DateTime(2025, 1, 1));
            dto.Item.Id.Should().Be(1);
            dto.Item.AverageRating.Should().Be(4.5);
            dto.Item.ReviewCount.Should().Be(2);
            dto.Item.IsCurrentlyOnLoan.Should().BeTrue();
            dto.Item.PrimaryPhotoUrl.Should().Be("https://example.com/photo.jpg");
            dto.Item.CategoryName.Should().Be("Tools");
        }

        [Fact]
        public async Task GetMyFavoritesAsync_NoReviews_AverageRatingIsZero()
        {
            var favorite = MakeFavorite("user-1", 1);
            favorite.Item.Reviews = new List<ItemReview>();
            _favoriteRepoMock.Setup(r => r.GetAllByUserIdAsync("user-1"))
                .ReturnsAsync(new List<UserFavoriteItem> { favorite });

            var result = await _service.GetMyFavoritesAsync("user-1");

            result.Single().Item.AverageRating.Should().Be(0);
        }

        [Fact]
        public async Task GetMyFavoritesAsync_NoPrimaryPhoto_PrimaryPhotoUrlIsNull()
        {
            var favorite = MakeFavorite("user-1", 1);
            favorite.Item.Photos = new List<ItemPhoto>
            {
                new() { PhotoUrl = "https://example.com/photo.jpg", IsPrimary = false }
            };
            _favoriteRepoMock.Setup(r => r.GetAllByUserIdAsync("user-1"))
                .ReturnsAsync(new List<UserFavoriteItem> { favorite });

            var result = await _service.GetMyFavoritesAsync("user-1");

            result.Single().Item.PrimaryPhotoUrl.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidFavorite_AddsAndSaves()
        {
            var item = MakeItem(1, "owner-1");
            _itemRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
            _favoriteRepoMock.Setup(r => r.ExistsAsync("user-1", 1)).ReturnsAsync(false);
            _favoriteRepoMock.Setup(r => r.AddAsync(It.IsAny<UserFavoriteItem>())).Returns(Task.CompletedTask);

            await _service.AddAsync("user-1", 1);

            _favoriteRepoMock.Verify(r => r.AddAsync(It.IsAny<UserFavoriteItem>()), Times.Once);
            _favoriteRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AddAsync_ItemNotFound_ThrowsKeyNotFoundException()
        {
            _itemRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Item?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.AddAsync("user-1", 99));
        }

        [Fact]
        public async Task AddAsync_OwnItem_ThrowsInvalidOperationException()
        {
            var item = MakeItem(1, "user-1"); // same as requestingUserId
            _itemRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddAsync("user-1", 1));
        }

        [Fact]
        public async Task AddAsync_AlreadyFavorited_ThrowsInvalidOperationException()
        {
            var item = MakeItem(1, "owner-1");
            _itemRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
            _favoriteRepoMock.Setup(r => r.ExistsAsync("user-1", 1)).ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddAsync("user-1", 1));
        }

        [Fact]
        public async Task AddAsync_AlreadyFavorited_DoesNotCallAdd()
        {
            var item = MakeItem(1, "owner-1");
            _itemRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
            _favoriteRepoMock.Setup(r => r.ExistsAsync("user-1", 1)).ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AddAsync("user-1", 1));

            _favoriteRepoMock.Verify(r => r.AddAsync(It.IsAny<UserFavoriteItem>()), Times.Never);
            _favoriteRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task AddAsync_SetsCorrectDefaultValues()
        {
            var item = MakeItem(1, "owner-1");
            _itemRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
            _favoriteRepoMock.Setup(r => r.ExistsAsync("user-1", 1)).ReturnsAsync(false);

            UserFavoriteItem? captured = null;
            _favoriteRepoMock.Setup(r => r.AddAsync(It.IsAny<UserFavoriteItem>()))
                .Callback<UserFavoriteItem>(f => captured = f)
                .Returns(Task.CompletedTask);

            await _service.AddAsync("user-1", 1);

            captured!.UserId.Should().Be("user-1");
            captured.ItemId.Should().Be(1);
            captured.NotifyWhenAvailable.Should().BeFalse();
            captured.SavedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task RemoveAsync_ExistingFavorite_RemovesAndSaves()
        {
            var favorite = MakeFavorite("user-1", 1);
            _favoriteRepoMock.Setup(r => r.GetAsync("user-1", 1)).ReturnsAsync(favorite);

            await _service.RemoveAsync("user-1", 1);

            _favoriteRepoMock.Verify(r => r.Remove(favorite), Times.Once);
            _favoriteRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_FavoriteNotFound_ThrowsKeyNotFoundException()
        {
            _favoriteRepoMock.Setup(r => r.GetAsync("user-1", 99))
                .ReturnsAsync((UserFavoriteItem?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.RemoveAsync("user-1", 99));
        }

        [Fact]
        public async Task RemoveAsync_FavoriteNotFound_DoesNotCallRemove()
        {
            _favoriteRepoMock.Setup(r => r.GetAsync("user-1", 99))
                .ReturnsAsync((UserFavoriteItem?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.RemoveAsync("user-1", 99));

            _favoriteRepoMock.Verify(r => r.Remove(It.IsAny<UserFavoriteItem>()), Times.Never);
            _favoriteRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ToggleNotifyAsync_SetsNotifyTrue_UpdatesAndReturnsDTO()
        {
            var favorite = MakeFavorite("user-1", 1, notify: false);
            _favoriteRepoMock.Setup(r => r.GetAsync("user-1", 1)).ReturnsAsync(favorite);
            _favoriteRepoMock.Setup(r => r.GetAllByUserIdAsync("user-1"))
                .ReturnsAsync(new List<UserFavoriteItem> { favorite });

            var result = await _service.ToggleNotifyAsync("user-1", 1, true);

            result.NotifyWhenAvailable.Should().BeTrue();
            _favoriteRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ToggleNotifyAsync_SetsNotifyFalse_UpdatesAndReturnsDTO()
        {
            var favorite = MakeFavorite("user-1", 1, notify: true);
            _favoriteRepoMock.Setup(r => r.GetAsync("user-1", 1)).ReturnsAsync(favorite);
            _favoriteRepoMock.Setup(r => r.GetAllByUserIdAsync("user-1"))
                .ReturnsAsync(new List<UserFavoriteItem> { favorite });

            var result = await _service.ToggleNotifyAsync("user-1", 1, false);

            result.NotifyWhenAvailable.Should().BeFalse();
        }

        [Fact]
        public async Task ToggleNotifyAsync_FavoriteNotFound_ThrowsKeyNotFoundException()
        {
            _favoriteRepoMock.Setup(r => r.GetAsync("user-1", 99))
                .ReturnsAsync((UserFavoriteItem?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.ToggleNotifyAsync("user-1", 99, true));
        }

        [Fact]
        public async Task ToggleNotifyAsync_ReturnsCorrectItemFromReloadedList()
        {
            var favorite1 = MakeFavorite("user-1", 1, notify: false);
            var favorite2 = MakeFavorite("user-1", 2, notify: false);

            _favoriteRepoMock.Setup(r => r.GetAsync("user-1", 1)).ReturnsAsync(favorite1);
            _favoriteRepoMock.Setup(r => r.GetAllByUserIdAsync("user-1"))
                .ReturnsAsync(new List<UserFavoriteItem> { favorite1, favorite2 });

            var result = await _service.ToggleNotifyAsync("user-1", 1, true);

            //Should return item 1, not item 2
            result.Item.Id.Should().Be(1);
        }

        [Fact]
        public async Task ToggleNotifyAsync_DoesNotSave_WhenFavoriteNotFound()
        {
            _favoriteRepoMock.Setup(r => r.GetAsync("user-1", 99))
                .ReturnsAsync((UserFavoriteItem?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.ToggleNotifyAsync("user-1", 99, true));

            _favoriteRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

    }
}
