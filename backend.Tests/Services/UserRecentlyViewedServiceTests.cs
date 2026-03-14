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
    public class UserRecentlyViewedServiceTests
    {
        private readonly Mock<IUserRecentlyViewedRepository> _repoMock;
        private readonly Mock<IItemRepository> _itemRepoMock;
        private readonly UserRecentlyViewedService _service;

        public UserRecentlyViewedServiceTests()
        {
            _repoMock = new Mock<IUserRecentlyViewedRepository>();
            _itemRepoMock = new Mock<IItemRepository>();
            _service = new UserRecentlyViewedService(_repoMock.Object, _itemRepoMock.Object);
        }

        [Fact]
        public async Task GetMyRecentlyViewedAsync_ReturnsMappedList()
        {
            var entries = MakeEntries("user-1", 3);
            _repoMock.Setup(r => r.GetAllByUserIdAsync("user-1", 10)).ReturnsAsync(entries);

            var result = await _service.GetMyRecentlyViewedAsync("user-1");

            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetMyRecentlyViewedAsync_NoEntries_ReturnsEmptyList()
        {
            _repoMock.Setup(r => r.GetAllByUserIdAsync("user-1", 10))
                .ReturnsAsync(new List<UserRecentlyViewedItem>());

            var result = await _service.GetMyRecentlyViewedAsync("user-1");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyRecentlyViewedAsync_MapsFieldsCorrectly()
        {
            var item = MakeItem(1);
            item.Reviews = new List<ItemReview>
            {
                new() { Rating = 3 },
                new() { Rating = 5 }
            };
            item.Loans = new List<Loan> { new() { Status = LoanStatus.Active } };
            item.Photos = new List<ItemPhoto>
            {
                new() { PhotoUrl = "https://example.com/photo.jpg", IsPrimary = true }
            };

            var viewedAt = new DateTime(2025, 1, 1);
            var entry = new UserRecentlyViewedItem
            {
                UserId = "user-1",
                ItemId = 1,
                ViewedAt = viewedAt,
                Item = item
            };

            _repoMock.Setup(r => r.GetAllByUserIdAsync("user-1", 10))
                .ReturnsAsync(new List<UserRecentlyViewedItem> { entry });

            var result = await _service.GetMyRecentlyViewedAsync("user-1");

            var dto = result.Single();
            dto.ViewedAt.Should().Be(viewedAt);
            dto.Item.Id.Should().Be(1);
            dto.Item.AverageRating.Should().Be(4.0);
            dto.Item.ReviewCount.Should().Be(2);
            dto.Item.IsCurrentlyOnLoan.Should().BeTrue();
            dto.Item.PrimaryPhotoUrl.Should().Be("https://example.com/photo.jpg");
        }

        [Fact]
        public async Task GetMyRecentlyViewedAsync_NoReviews_AverageRatingIsZero()
        {
            var entry = MakeEntry("user-1", 1);
            entry.Item.Reviews = new List<ItemReview>();
            _repoMock.Setup(r => r.GetAllByUserIdAsync("user-1", 10))
                .ReturnsAsync(new List<UserRecentlyViewedItem> { entry });

            var result = await _service.GetMyRecentlyViewedAsync("user-1");

            result.Single().Item.AverageRating.Should().Be(0);
        }

        [Fact]
        public async Task GetMyRecentlyViewedAsync_NoPrimaryPhoto_PrimaryPhotoUrlIsNull()
        {
            var entry = MakeEntry("user-1", 1);
            entry.Item.Photos = new List<ItemPhoto>
            {
                new() { PhotoUrl = "https://example.com/photo.jpg", IsPrimary = false }
            };
            _repoMock.Setup(r => r.GetAllByUserIdAsync("user-1", 10))
                .ReturnsAsync(new List<UserRecentlyViewedItem> { entry });

            var result = await _service.GetMyRecentlyViewedAsync("user-1");

            result.Single().Item.PrimaryPhotoUrl.Should().BeNull();
        }


        [Fact]
        public async Task TrackViewAsync_NewItem_AddsEntry()
        {
            var item = MakeItem(1, "owner-1");
            _itemRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
            _repoMock.Setup(r => r.GetAsync("user-1", 1)).ReturnsAsync((UserRecentlyViewedItem?)null);
            _repoMock.Setup(r => r.GetAllByUserIdAsync("user-1", 100))
                .ReturnsAsync(new List<UserRecentlyViewedItem>());

            await _service.TrackViewAsync("user-1", 1);

            _repoMock.Verify(r => r.AddAsync(It.IsAny<UserRecentlyViewedItem>()), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task TrackViewAsync_ExistingEntry_UpdatesViewedAt()
        {
            var item = MakeItem(1, "owner-1");
            var existing = MakeEntry("user-1", 1);
            existing.ViewedAt = DateTime.UtcNow.AddDays(-1);

            _itemRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
            _repoMock.Setup(r => r.GetAsync("user-1", 1)).ReturnsAsync(existing);

            await _service.TrackViewAsync("user-1", 1);

            existing.ViewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            _repoMock.Verify(r => r.AddAsync(It.IsAny<UserRecentlyViewedItem>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task TrackViewAsync_ItemNotFound_ThrowsKeyNotFoundException()
        {
            _itemRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Item?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.TrackViewAsync("user-1", 99));
        }

        [Fact]
        public async Task TrackViewAsync_OwnItem_DoesNotTrack()
        {
            var item = MakeItem(1, "user-1"); //owner == viewer
            _itemRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

            await _service.TrackViewAsync("user-1", 1);

            _repoMock.Verify(r => r.GetAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<UserRecentlyViewedItem>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task TrackViewAsync_AtCapacity_RemovesOldestBeforeAdding()
        {
            var item = MakeItem(99, "owner-1");
            var entries = MakeEntries("user-1", 10); //exactly at cap

            _itemRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(item);
            _repoMock.Setup(r => r.GetAsync("user-1", 99)).ReturnsAsync((UserRecentlyViewedItem?)null);
            _repoMock.Setup(r => r.GetAllByUserIdAsync("user-1", 100)).ReturnsAsync(entries);

            await _service.TrackViewAsync("user-1", 99);

            //Should remove the oldest (last in desc-ordered list)
            _repoMock.Verify(r => r.Remove(entries.Last()), Times.Once);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<UserRecentlyViewedItem>()), Times.Once);
        }

        [Fact]
        public async Task TrackViewAsync_BelowCapacity_DoesNotRemoveAny()
        {
            var item = MakeItem(99, "owner-1");
            var entries = MakeEntries("user-1", 9); //one below cap

            _itemRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(item);
            _repoMock.Setup(r => r.GetAsync("user-1", 99)).ReturnsAsync((UserRecentlyViewedItem?)null);
            _repoMock.Setup(r => r.GetAllByUserIdAsync("user-1", 100)).ReturnsAsync(entries);

            await _service.TrackViewAsync("user-1", 99);

            _repoMock.Verify(r => r.Remove(It.IsAny<UserRecentlyViewedItem>()), Times.Never);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<UserRecentlyViewedItem>()), Times.Once);
        }

        [Fact]
        public async Task TrackViewAsync_NewEntryHasCorrectFields()
        {
            var item = MakeItem(1, "owner-1");
            _itemRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
            _repoMock.Setup(r => r.GetAsync("user-1", 1)).ReturnsAsync((UserRecentlyViewedItem?)null);
            _repoMock.Setup(r => r.GetAllByUserIdAsync("user-1", 100))
                .ReturnsAsync(new List<UserRecentlyViewedItem>());

            UserRecentlyViewedItem? captured = null;
            _repoMock.Setup(r => r.AddAsync(It.IsAny<UserRecentlyViewedItem>()))
                .Callback<UserRecentlyViewedItem>(e => captured = e)
                .Returns(Task.CompletedTask);

            await _service.TrackViewAsync("user-1", 1);

            captured!.UserId.Should().Be("user-1");
            captured.ItemId.Should().Be(1);
            captured.ViewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }


        [Fact]
        public async Task TrackViewAsync_OverCapacity_RemovesAllExcess()
        {
            var item = MakeItem(99, "owner-1");
            var entries = MakeEntries("user-1", 11); //over cap due to bug/direct DB insert

            _itemRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(item);
            _repoMock.Setup(r => r.GetAsync("user-1", 99)).ReturnsAsync((UserRecentlyViewedItem?)null);
            _repoMock.Setup(r => r.GetAllByUserIdAsync("user-1", 100)).ReturnsAsync(entries);

            await _service.TrackViewAsync("user-1", 99);

            //Should remove 2 (entries at index 9 and 10) to stay at cap after adding 1
            _repoMock.Verify(r => r.Remove(It.IsAny<UserRecentlyViewedItem>()), Times.Exactly(2));
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

        private static UserRecentlyViewedItem MakeEntry(string userId, int itemId) => new()
        {
            UserId = userId,
            ItemId = itemId,
            ViewedAt = DateTime.UtcNow,
            Item = MakeItem(itemId)
        };

        private static List<UserRecentlyViewedItem> MakeEntries(string userId, int count) =>
            Enumerable.Range(1, count)
                .Select(i => MakeEntry(userId, i))
                .ToList();







    }
}
