using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class UserRecentlyViewedRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UserRecentlyViewedRepository _repo;

        public UserRecentlyViewedRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new UserRecentlyViewedRepository(_context);
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

        private async Task<UserRecentlyViewedItem> SeedRecentlyViewedAsync(
            string userId,
            int itemId,
            DateTime? viewedAt = null)
        {
            var entry = new UserRecentlyViewedItem
            {
                UserId = userId,
                ItemId = itemId,
                ViewedAt = viewedAt ?? DateTime.UtcNow
            };
            _context.UserRecentlyViewedItems.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }


        [Fact]
        public async Task GetAllByUserIdAsync_ReturnsOnlyUserEntries()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedUserAsync("owner-1");
            var item1 = await SeedItemAsync("owner-1");
            var item2 = await SeedItemAsync("owner-1");
            var item3 = await SeedItemAsync("owner-1");
            await SeedRecentlyViewedAsync("user-1", item1.Id);
            await SeedRecentlyViewedAsync("user-1", item2.Id);
            await SeedRecentlyViewedAsync("user-2", item3.Id); //different user

            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal("user-1", r.UserId));
        }

        [Fact]
        public async Task GetAllByUserIdAsync_OrderedByViewedAtDescending()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item1 = await SeedItemAsync("owner-1");
            var item2 = await SeedItemAsync("owner-1");
            var item3 = await SeedItemAsync("owner-1");
            var oldest = await SeedRecentlyViewedAsync("user-1", item1.Id, DateTime.UtcNow.AddMinutes(-20));
            var middle = await SeedRecentlyViewedAsync("user-1", item2.Id, DateTime.UtcNow.AddMinutes(-10));
            var newest = await SeedRecentlyViewedAsync("user-1", item3.Id, DateTime.UtcNow);

            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Equal(newest.ItemId, result[0].ItemId);
            Assert.Equal(middle.ItemId, result[1].ItemId);
            Assert.Equal(oldest.ItemId, result[2].ItemId);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_RespectsLimit()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");

            //seed 5 entries
            for (int i = 0; i < 5; i++)
            {
                var item = await SeedItemAsync("owner-1");
                await SeedRecentlyViewedAsync("user-1", item.Id,
                    DateTime.UtcNow.AddMinutes(-i));
            }

            var result = await _repo.GetAllByUserIdAsync("user-1", limit: 3);

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_DefaultLimit_Returns20Max()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");

            //seed 25 entries
            for (int i = 0; i < 25; i++)
            {
                var item = await SeedItemAsync("owner-1");
                await SeedRecentlyViewedAsync("user-1", item.Id,
                    DateTime.UtcNow.AddMinutes(-i));
            }

            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Equal(20, result.Count);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_IncludesItemWithDetails()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            await SeedRecentlyViewedAsync("user-1", item.Id);

            _context.ChangeTracker.Clear();

            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Single(result);
            Assert.NotNull(result[0].Item);
            Assert.NotNull(result[0].Item.Category);
            Assert.NotNull(result[0].Item.Owner);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_NoEntries_ReturnsEmpty()
        {
            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAsync_ExistingEntry_ReturnsEntry()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            await SeedRecentlyViewedAsync("user-1", item.Id);

            var result = await _repo.GetAsync("user-1", item.Id);

            Assert.NotNull(result);
            Assert.Equal("user-1", result!.UserId);
            Assert.Equal(item.Id, result.ItemId);
        }

        [Fact]
        public async Task GetAsync_NonExistingEntry_ReturnsNull()
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
            await SeedRecentlyViewedAsync("user-1", item.Id);

            var result = await _repo.GetAsync("user-2", item.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_WrongItem_ReturnsNull()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            await SeedRecentlyViewedAsync("user-1", item.Id);

            var result = await _repo.GetAsync("user-1", 999);

            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_SaveChangesAsync_PersistsEntry()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");

            var entry = new UserRecentlyViewedItem
            {
                UserId = "user-1",
                ItemId = item.Id,
                ViewedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(entry);
            await _repo.SaveChangesAsync();

            var saved = await _context.UserRecentlyViewedItems
                .FirstOrDefaultAsync(r => r.UserId == "user-1" && r.ItemId == item.Id);
            Assert.NotNull(saved);
            Assert.Equal("user-1", saved!.UserId);
            Assert.Equal(item.Id, saved.ItemId);
        }

        [Fact]
        public async Task UpdateViewedAt_OnRepeatVisit_UpdatesTimestamp()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            var original = await SeedRecentlyViewedAsync("user-1", item.Id,
                DateTime.UtcNow.AddMinutes(-30));

            //simulate upsert — update ViewedAt
            original.ViewedAt = DateTime.UtcNow;
            _context.UserRecentlyViewedItems.Update(original);
            await _repo.SaveChangesAsync();

            var updated = await _context.UserRecentlyViewedItems
                .FirstOrDefaultAsync(r => r.UserId == "user-1" && r.ItemId == item.Id);
            Assert.NotNull(updated);
            Assert.True(updated!.ViewedAt > DateTime.UtcNow.AddMinutes(-5));

            //still only one record — not duplicated
            var count = await _context.UserRecentlyViewedItems
                .CountAsync(r => r.UserId == "user-1" && r.ItemId == item.Id);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Remove_SaveChangesAsync_RemovesEntry()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            var seeded = await SeedRecentlyViewedAsync("user-1", item.Id);

            _repo.Remove(seeded);
            await _repo.SaveChangesAsync();

            var deleted = await _context.UserRecentlyViewedItems
                .FirstOrDefaultAsync(r => r.UserId == "user-1" && r.ItemId == item.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task Remove_DoesNotAffectOtherEntries()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("owner-1");
            var item1 = await SeedItemAsync("owner-1");
            var item2 = await SeedItemAsync("owner-1");
            var toRemove = await SeedRecentlyViewedAsync("user-1", item1.Id);
            await SeedRecentlyViewedAsync("user-1", item2.Id); //should stay

            _repo.Remove(toRemove);
            await _repo.SaveChangesAsync();

            var remaining = await _context.UserRecentlyViewedItems
                .Where(r => r.UserId == "user-1")
                .ToListAsync();
            Assert.Single(remaining);
            Assert.Equal(item2.Id, remaining[0].ItemId);
        }

        [Fact]
        public async Task Remove_DoesNotAffectOtherUsersEntries()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedUserAsync("owner-1");
            var item = await SeedItemAsync("owner-1");
            var toRemove = await SeedRecentlyViewedAsync("user-1", item.Id);
            await SeedRecentlyViewedAsync("user-2", item.Id); //different user — should stay

            _repo.Remove(toRemove);
            await _repo.SaveChangesAsync();

            var user2Entry = await _context.UserRecentlyViewedItems
                .FirstOrDefaultAsync(r => r.UserId == "user-2" && r.ItemId == item.Id);
            Assert.NotNull(user2Entry);
        }
    }
}