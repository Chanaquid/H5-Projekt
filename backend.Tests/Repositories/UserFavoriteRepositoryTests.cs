using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class UserFavoriteRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UserFavoriteRepository _repo;

        public UserFavoriteRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new UserFavoriteRepository(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<ApplicationUser> SeedUserAsync(string id)
        {
            var user = new ApplicationUser
            {
                Id = id,
                UserName = $"{id}@test.com",
                Email = $"{id}@test.com",
                FullName = $"User {id}",
                NormalizedEmail = $"{id}@test.com".ToUpper(),
                NormalizedUserName = $"{id}@test.com".ToUpper()
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private async Task<Item> SeedItemAsync(string ownerId)
        {
            var category = new Category { Name = "Tools", Icon = "🔧", IsActive = true };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var item = new Item
            {
                OwnerId = ownerId,
                CategoryId = category.Id,
                Title = "Test Item",
                Description = "Test description",
                Status = ItemStatus.Approved,
                IsActive = true,
                Condition = ItemCondition.Good,
                AvailableFrom = DateTime.UtcNow.Date,
                AvailableUntil = DateTime.UtcNow.Date.AddDays(30),
                QrCode = Guid.NewGuid().ToString("N")[..12].ToUpper(),
                RowVersion = Guid.NewGuid().ToByteArray()
            };
            _context.Items.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        private async Task<UserFavoriteItem> SeedFavoriteAsync(
            string userId,
            int itemId,
            bool notifyWhenAvailable = false,
            DateTime? savedAt = null)
        {
            var favorite = new UserFavoriteItem
            {
                UserId = userId,
                ItemId = itemId,
                NotifyWhenAvailable = notifyWhenAvailable,
                SavedAt = savedAt ?? DateTime.UtcNow
            };
            _context.UserFavoriteItems.Add(favorite);
            await _context.SaveChangesAsync();
            return favorite;
        }

        [Fact]
        public async Task GetAllByUserIdAsync_ReturnsOnlyUserFavorites()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedUserAsync("owner-1");
            var item1 = await SeedItemAsync("owner-1");
            var item2 = await SeedItemAsync("owner-1");
            var item3 = await SeedItemAsync("owner-1");
            await SeedFavoriteAsync("user-1", item1.Id);
            await SeedFavoriteAsync("user-1", item2.Id);
            await SeedFavoriteAsync("user-2", item3.Id); //different user

            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Equal(2, result.Count);
            Assert.All(result, f => Assert.Equal("user-1", f.UserId));
        }

        [Fact]
        public async Task GetAllByUserIdAsync_OrderedBySavedAtDescending()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item1 = await SeedItemAsync("owner-1");
            var item2 = await SeedItemAsync("owner-1");
            var older = await SeedFavoriteAsync("user-1", item1.Id, savedAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedFavoriteAsync("user-1", item2.Id, savedAt: DateTime.UtcNow);

            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Equal(newer.ItemId, result[0].ItemId);
            Assert.Equal(older.ItemId, result[1].ItemId);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_IncludesItemWithDetails()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            await SeedFavoriteAsync("user-1", item.Id);

            _context.ChangeTracker.Clear();

            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Single(result);
            Assert.NotNull(result[0].Item);
            Assert.NotNull(result[0].Item.Category);
            Assert.NotNull(result[0].Item.Owner);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_NoFavorites_ReturnsEmpty()
        {
            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAsync_ExistingFavorite_ReturnsFavorite()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            await SeedFavoriteAsync("user-1", item.Id);

            var result = await _repo.GetAsync("user-1", item.Id);

            Assert.NotNull(result);
            Assert.Equal("user-1", result!.UserId);
            Assert.Equal(item.Id, result.ItemId);
        }

        [Fact]
        public async Task GetAsync_NonExistingFavorite_ReturnsNull()
        {
            var result = await _repo.GetAsync("user-1", 999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_WrongUser_ReturnsNull()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            await SeedFavoriteAsync("user-1", item.Id);

            var result = await _repo.GetAsync("user-2", item.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_WrongItem_ReturnsNull()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            await SeedFavoriteAsync("user-1", item.Id);

            var result = await _repo.GetAsync("user-1", 999);

            Assert.Null(result);
        }


        [Fact]
        public async Task ExistsAsync_ExistingFavorite_ReturnsTrue()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            await SeedFavoriteAsync("user-1", item.Id);

            var result = await _repo.ExistsAsync("user-1", item.Id);

            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_NonExistingFavorite_ReturnsFalse()
        {
            var result = await _repo.ExistsAsync("user-1", 999);

            Assert.False(result);
        }

        [Fact]
        public async Task ExistsAsync_WrongUser_ReturnsFalse()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            await SeedFavoriteAsync("user-1", item.Id);

            var result = await _repo.ExistsAsync("user-2", item.Id);

            Assert.False(result);
        }

        [Fact]
        public async Task ExistsAsync_WrongItem_ReturnsFalse()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            await SeedFavoriteAsync("user-1", item.Id);

            var result = await _repo.ExistsAsync("user-1", 999);

            Assert.False(result);
        }


        [Fact]
        public async Task AddAsync_SaveChangesAsync_PersistsFavorite()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");

            var favorite = new UserFavoriteItem
            {
                UserId = "user-1",
                ItemId = item.Id,
                NotifyWhenAvailable = true,
                SavedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(favorite);
            await _repo.SaveChangesAsync();

            var saved = await _context.UserFavoriteItems
                .FirstOrDefaultAsync(f => f.UserId == "user-1" && f.ItemId == item.Id);
            Assert.NotNull(saved);
            Assert.True(saved!.NotifyWhenAvailable);
        }

        [Fact]
        public async Task AddAsync_SaveChangesAsync_DefaultNotifyIsFalse()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");

            var favorite = new UserFavoriteItem
            {
                UserId = "user-1",
                ItemId = item.Id,
                SavedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(favorite);
            await _repo.SaveChangesAsync();

            var saved = await _context.UserFavoriteItems
                .FirstOrDefaultAsync(f => f.UserId == "user-1" && f.ItemId == item.Id);
            Assert.NotNull(saved);
            Assert.False(saved!.NotifyWhenAvailable);
        }

  
        [Fact]
        public async Task Remove_SaveChangesAsync_RemovesFavorite()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            var seeded = await SeedFavoriteAsync("user-1", item.Id);

            _repo.Remove(seeded);
            await _repo.SaveChangesAsync();

            var deleted = await _context.UserFavoriteItems
                .FirstOrDefaultAsync(f => f.UserId == "user-1" && f.ItemId == item.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task Remove_DoesNotAffectOtherFavorites()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item1 = await SeedItemAsync("owner-1");
            var item2 = await SeedItemAsync("owner-1");
            var toRemove = await SeedFavoriteAsync("user-1", item1.Id);
            await SeedFavoriteAsync("user-1", item2.Id); //should stay

            _repo.Remove(toRemove);
            await _repo.SaveChangesAsync();

            var remaining = await _context.UserFavoriteItems
                .Where(f => f.UserId == "user-1")
                .ToListAsync();
            Assert.Single(remaining);
            Assert.Equal(item2.Id, remaining[0].ItemId);
        }
    }
}