using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class ItemRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ItemRepository _repo;

        public ItemRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new ItemRepository(_context);
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

        private async Task<Category> SeedCategoryAsync(string name = "Tools")
        {
            var category = new Category { Name = name, Icon = "🔧", IsActive = true };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        private async Task<Item> SeedItemAsync(
            string ownerId,
            int categoryId,
            ItemStatus status = ItemStatus.Approved,
            bool isActive = true,
            string qrCode = "",
            DateTime? availableFrom = null,
            DateTime? availableUntil = null,
            DateTime? createdAt = null)
        {
            var item = new Item
            {
                OwnerId = ownerId,
                CategoryId = categoryId,
                Title = "Test Item",
                Description = "Test description",
                CurrentValue = 500m,
                Condition = ItemCondition.Good,
                Status = status,
                IsActive = isActive,
                QrCode = string.IsNullOrEmpty(qrCode) ? Guid.NewGuid().ToString("N")[..12].ToUpper() : qrCode,
                PickupAddress = "Test Address",
                PickupLatitude = 55.6761,
                PickupLongitude = 12.5683,
                AvailableFrom = availableFrom ?? DateTime.UtcNow.Date,
                AvailableUntil = availableUntil ?? DateTime.UtcNow.Date.AddDays(30),
                CreatedAt = createdAt ?? DateTime.UtcNow,
                RowVersion = Guid.NewGuid().ToByteArray()
            };
            _context.Items.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        [Fact]
        public async Task GetAllApprovedAsync_ReturnsOnlyApprovedAndActive()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            await SeedItemAsync("owner-1", cat.Id, ItemStatus.Approved, isActive: true);
            await SeedItemAsync("owner-1", cat.Id, ItemStatus.Pending, isActive: true);
            await SeedItemAsync("owner-1", cat.Id, ItemStatus.Approved, isActive: false);
            await SeedItemAsync("owner-1", cat.Id, ItemStatus.Rejected, isActive: true);

            var result = await _repo.GetAllApprovedAsync();

            Assert.Single(result);
            Assert.Equal(ItemStatus.Approved, result[0].Status);
            Assert.True(result[0].IsActive);
        }

        [Fact]
        public async Task GetAllApprovedAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            var older = await SeedItemAsync("owner-1", cat.Id, createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedItemAsync("owner-1", cat.Id, createdAt: DateTime.UtcNow);

            var result = await _repo.GetAllApprovedAsync();

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetAllApprovedAsync_NoItems_ReturnsEmpty()
        {
            var result = await _repo.GetAllApprovedAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_IncludeInactive_False_ReturnsOnlyActive()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            await SeedItemAsync("owner-1", cat.Id, isActive: true);
            await SeedItemAsync("owner-1", cat.Id, isActive: false);

            var result = await _repo.GetAllAsync(includeInactive: false);

            Assert.Single(result);
            Assert.True(result[0].IsActive);
        }

        [Fact]
        public async Task GetAllAsync_IncludeInactive_True_ReturnsAll()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            await SeedItemAsync("owner-1", cat.Id, isActive: true);
            await SeedItemAsync("owner-1", cat.Id, isActive: false);

            var result = await _repo.GetAllAsync(includeInactive: true);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            var older = await SeedItemAsync("owner-1", cat.Id, createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedItemAsync("owner-1", cat.Id, createdAt: DateTime.UtcNow);

            var result = await _repo.GetAllAsync(includeInactive: true);

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetPublicByOwnerAsync_ReturnsOnlyApprovedActiveItemsForOwner()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("owner-2");
            var cat = await SeedCategoryAsync();
            await SeedItemAsync("owner-1", cat.Id, ItemStatus.Approved, isActive: true);
            await SeedItemAsync("owner-1", cat.Id, ItemStatus.Pending, isActive: true);
            await SeedItemAsync("owner-1", cat.Id, ItemStatus.Approved, isActive: false);
            await SeedItemAsync("owner-2", cat.Id, ItemStatus.Approved, isActive: true); //different owner

            var result = await _repo.GetPublicByOwnerAsync("owner-1");

            Assert.Single(result);
            Assert.Equal("owner-1", result[0].OwnerId);
            Assert.Equal(ItemStatus.Approved, result[0].Status);
            Assert.True(result[0].IsActive);
        }

        [Fact]
        public async Task GetPublicByOwnerAsync_NoItems_ReturnsEmpty()
        {
            var result = await _repo.GetPublicByOwnerAsync("owner-1");

            Assert.Empty(result);
        }


        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsItem()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            var seeded = await SeedItemAsync("owner-1", cat.Id);

            var result = await _repo.GetByIdAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetByIdAsync(999);

            Assert.Null(result);
        }


        [Fact]
        public async Task GetByIdWithDetailsAsync_ExistingId_IncludesOwnerAndCategory()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            var seeded = await SeedItemAsync("owner-1", cat.Id);

            var result = await _repo.GetByIdWithDetailsAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.NotNull(result!.Owner);
            Assert.NotNull(result.Category);
            Assert.Equal("owner-1", result.Owner.Id);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetByIdWithDetailsAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByQrCodeAsync_ExistingQrCode_ReturnsItem()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            var seeded = await SeedItemAsync("owner-1", cat.Id, qrCode: "TESTQR123456");

            var result = await _repo.GetByQrCodeAsync("TESTQR123456");

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
        }

        [Fact]
        public async Task GetByQrCodeAsync_NonExistingQrCode_ReturnsNull()
        {
            var result = await _repo.GetByQrCodeAsync("DOESNOTEXIST");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByQrCodeAsync_IncludesOwnerAndLoans()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            var seeded = await SeedItemAsync("owner-1", cat.Id, qrCode: "TESTQR123456");

            var result = await _repo.GetByQrCodeAsync("TESTQR123456");

            Assert.NotNull(result!.Owner);
            Assert.NotNull(result.Loans);
        }

        [Fact]
        public async Task GetByOwnerAsync_ReturnsOnlyItemsForOwner()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("owner-2");
            var cat = await SeedCategoryAsync();
            await SeedItemAsync("owner-1", cat.Id);
            await SeedItemAsync("owner-1", cat.Id);
            await SeedItemAsync("owner-2", cat.Id);

            var result = await _repo.GetByOwnerAsync("owner-1");

            Assert.Equal(2, result.Count);
            Assert.All(result, i => Assert.Equal("owner-1", i.OwnerId));
        }

        [Fact]
        public async Task GetByOwnerAsync_NoItems_ReturnsEmpty()
        {
            var result = await _repo.GetByOwnerAsync("owner-1");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByOwnerAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            var older = await SeedItemAsync("owner-1", cat.Id, createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedItemAsync("owner-1", cat.Id, createdAt: DateTime.UtcNow);

            var result = await _repo.GetByOwnerAsync("owner-1");

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetPendingApprovalsAsync_ReturnsOnlyPendingItems()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            await SeedItemAsync("owner-1", cat.Id, ItemStatus.Pending);
            await SeedItemAsync("owner-1", cat.Id, ItemStatus.Approved);
            await SeedItemAsync("owner-1", cat.Id, ItemStatus.Rejected);

            var result = await _repo.GetPendingApprovalsAsync();

            Assert.Single(result);
            Assert.Equal(ItemStatus.Pending, result[0].Status);
        }

        [Fact]
        public async Task GetPendingApprovalsAsync_OrderedByCreatedAtAscending()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            var newer = await SeedItemAsync("owner-1", cat.Id, ItemStatus.Pending, createdAt: DateTime.UtcNow);
            var older = await SeedItemAsync("owner-1", cat.Id, ItemStatus.Pending, createdAt: DateTime.UtcNow.AddMinutes(-10));

            var result = await _repo.GetPendingApprovalsAsync();

            Assert.Equal(older.Id, result[0].Id); //oldest first — FIFO
            Assert.Equal(newer.Id, result[1].Id);
        }

        [Fact]
        public async Task GetPendingApprovalsAsync_NoPendingItems_ReturnsEmpty()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            await SeedItemAsync("owner-1", cat.Id, ItemStatus.Approved);

            var result = await _repo.GetPendingApprovalsAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetActiveItemsExpiredBeforeAsync_ReturnsExpiredActiveItems()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            await SeedItemAsync("owner-1", cat.Id, isActive: true,
                availableUntil: DateTime.UtcNow.Date.AddDays(-1)); //expired
            await SeedItemAsync("owner-1", cat.Id, isActive: true,
                availableUntil: DateTime.UtcNow.Date.AddDays(5));  //not yet expired
            await SeedItemAsync("owner-1", cat.Id, isActive: false,
                availableUntil: DateTime.UtcNow.Date.AddDays(-1)); //expired but already inactive

            var result = await _repo.GetActiveItemsExpiredBeforeAsync(DateTime.UtcNow.Date);

            Assert.Single(result);
            Assert.True(result[0].IsActive);
            Assert.True(result[0].AvailableUntil < DateTime.UtcNow.Date);
        }

        [Fact]
        public async Task GetActiveItemsExpiredBeforeAsync_NoExpiredItems_ReturnsEmpty()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            await SeedItemAsync("owner-1", cat.Id, isActive: true,
                availableUntil: DateTime.UtcNow.Date.AddDays(5));

            var result = await _repo.GetActiveItemsExpiredBeforeAsync(DateTime.UtcNow.Date);

            Assert.Empty(result);
        }


        [Fact]
        public async Task QrCodeExistsAsync_ExistingCode_ReturnsTrue()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            await SeedItemAsync("owner-1", cat.Id, qrCode: "UNIQUECODE12");

            var result = await _repo.QrCodeExistsAsync("UNIQUECODE12");

            Assert.True(result);
        }

        [Fact]
        public async Task QrCodeExistsAsync_NonExistingCode_ReturnsFalse()
        {
            var result = await _repo.QrCodeExistsAsync("DOESNOTEXIST");

            Assert.False(result);
        }


        [Fact]
        public async Task AddAsync_SaveChangesAsync_PersistsItem()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();

            var item = new Item
            {
                OwnerId = "owner-1",
                CategoryId = cat.Id,
                Title = "New Drill",
                Description = "A brand new drill",
                CurrentValue = 300m,
                Condition = ItemCondition.Excellent,
                Status = ItemStatus.Pending,
                IsActive = true,
                QrCode = "NEWDRILLQR12",
                PickupAddress = "Test Address",
                AvailableFrom = DateTime.UtcNow.Date,
                AvailableUntil = DateTime.UtcNow.Date.AddDays(30),
                CreatedAt = DateTime.UtcNow,
                RowVersion = Guid.NewGuid().ToByteArray()
            };

            await _repo.AddAsync(item);
            await _repo.SaveChangesAsync();

            var saved = await _context.Items.FirstOrDefaultAsync(i => i.QrCode == "NEWDRILLQR12");
            Assert.NotNull(saved);
            Assert.Equal("New Drill", saved!.Title);
            Assert.Equal(ItemStatus.Pending, saved.Status);
        }


        [Fact]
        public async Task Update_SaveChangesAsync_PersistsChanges()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            var seeded = await SeedItemAsync("owner-1", cat.Id, ItemStatus.Pending);

            seeded.Status = ItemStatus.Approved;
            seeded.AdminNote = "Looks good!";
            seeded.UpdatedAt = DateTime.UtcNow;
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.Items.FindAsync(seeded.Id);
            Assert.Equal(ItemStatus.Approved, updated!.Status);
            Assert.Equal("Looks good!", updated.AdminNote);
            Assert.NotNull(updated.UpdatedAt);
        }

        [Fact]
        public async Task Update_DeactivateItem_PersistsIsActiveFalse()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            var seeded = await SeedItemAsync("owner-1", cat.Id, isActive: true);

            seeded.IsActive = false;
            seeded.UpdatedAt = DateTime.UtcNow;
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.Items.FindAsync(seeded.Id);
            Assert.False(updated!.IsActive);
        }

        [Fact]
        public async Task Delete_SaveChangesAsync_RemovesItem()
        {
            await SeedUserAsync("owner-1");
            var cat = await SeedCategoryAsync();
            var seeded = await SeedItemAsync("owner-1", cat.Id);

            _repo.Delete(seeded);
            await _repo.SaveChangesAsync();

            var deleted = await _context.Items.FindAsync(seeded.Id);
            Assert.Null(deleted);
        }
    }
}