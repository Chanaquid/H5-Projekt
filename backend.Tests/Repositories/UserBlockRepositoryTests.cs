using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class UserBlockRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UserBlockRepository _repo;

        public UserBlockRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new UserBlockRepository(_context);
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

        private async Task<UserBlock> SeedBlockAsync(
            string blockerId,
            string blockedId,
            DateTime? createdAt = null)
        {
            var block = new UserBlock
            {
                BlockerId = blockerId,
                BlockedId = blockedId,
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
            _context.UserBlocks.Add(block);
            await _context.SaveChangesAsync();
            return block;
        }

        [Fact]
        public async Task GetAsync_ExistingBlock_ReturnsBlock()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedBlockAsync("user-1", "user-2");

            var result = await _repo.GetAsync("user-1", "user-2");

            Assert.NotNull(result);
            Assert.Equal("user-1", result!.BlockerId);
            Assert.Equal("user-2", result.BlockedId);
        }

        [Fact]
        public async Task GetAsync_NonExistingBlock_ReturnsNull()
        {
            var result = await _repo.GetAsync("user-1", "user-2");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_ReverseOrder_ReturnsNull()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedBlockAsync("user-1", "user-2");

            //user-2 did not block user-1 — only user-1 blocked user-2
            var result = await _repo.GetAsync("user-2", "user-1");

            Assert.Null(result);
        }


        [Fact]
        public async Task GetBlocksByUserIdAsync_ReturnsOnlyBlockerBlocks()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedUserAsync("user-3");
            await SeedUserAsync("user-4");
            await SeedBlockAsync("user-1", "user-2");
            await SeedBlockAsync("user-1", "user-3");
            await SeedBlockAsync("user-4", "user-1"); //user-4 blocked user-1 —> should not appear

            var result = await _repo.GetBlocksByUserIdAsync("user-1");

            Assert.Equal(2, result.Count);
            Assert.All(result, b => Assert.Equal("user-1", b.BlockerId));
        }

        [Fact]
        public async Task GetBlocksByUserIdAsync_IncludesBlockedUser()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedBlockAsync("user-1", "user-2");

            _context.ChangeTracker.Clear();

            var result = await _repo.GetBlocksByUserIdAsync("user-1");

            Assert.Single(result);
            Assert.NotNull(result[0].Blocked);
            Assert.Equal("user-2", result[0].Blocked.Id);
        }

        [Fact]
        public async Task GetBlocksByUserIdAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedUserAsync("user-3");
            var older = await SeedBlockAsync("user-1", "user-2", createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedBlockAsync("user-1", "user-3", createdAt: DateTime.UtcNow);

            var result = await _repo.GetBlocksByUserIdAsync("user-1");

            Assert.Equal(newer.BlockedId, result[0].BlockedId);
            Assert.Equal(older.BlockedId, result[1].BlockedId);
        }

        [Fact]
        public async Task GetBlocksByUserIdAsync_NoBlocks_ReturnsEmpty()
        {
            var result = await _repo.GetBlocksByUserIdAsync("user-1");

            Assert.Empty(result);
        }

        [Fact]
        public async Task IsBlockedAsync_ABlocksB_ReturnsTrue()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedBlockAsync("user-1", "user-2");

            var result = await _repo.IsBlockedAsync("user-1", "user-2");

            Assert.True(result);
        }

        [Fact]
        public async Task IsBlockedAsync_BBlocksA_ReturnsTrue()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedBlockAsync("user-2", "user-1"); //reverse direction

            var result = await _repo.IsBlockedAsync("user-1", "user-2");

            Assert.True(result);
        }

        [Fact]
        public async Task IsBlockedAsync_NoBlock_ReturnsFalse()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");

            var result = await _repo.IsBlockedAsync("user-1", "user-2");

            Assert.False(result);
        }

        [Fact]
        public async Task IsBlockedAsync_UnrelatedBlock_ReturnsFalse()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedUserAsync("user-3");
            await SeedBlockAsync("user-1", "user-3"); // ser-1 blocked user-3, not user-2

            var result = await _repo.IsBlockedAsync("user-1", "user-2");

            Assert.False(result);
        }

 
        [Fact]
        public async Task AddAsync_SaveChangesAsync_PersistsBlock()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");

            var block = new UserBlock
            {
                BlockerId = "user-1",
                BlockedId = "user-2",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(block);
            await _repo.SaveChangesAsync();

            var saved = await _context.UserBlocks
                .FirstOrDefaultAsync(b => b.BlockerId == "user-1" && b.BlockedId == "user-2");
            Assert.NotNull(saved);
            Assert.Equal("user-1", saved!.BlockerId);
            Assert.Equal("user-2", saved.BlockedId);
        }

 
        [Fact]
        public async Task Remove_SaveChangesAsync_RemovesBlock()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var seeded = await SeedBlockAsync("user-1", "user-2");

            _repo.Remove(seeded);
            await _repo.SaveChangesAsync();

            var deleted = await _context.UserBlocks
                .FirstOrDefaultAsync(b => b.BlockerId == "user-1" && b.BlockedId == "user-2");
            Assert.Null(deleted);
        }

 
        [Fact]
        public async Task RemoveAsync_SaveChangesAsync_RemovesBlock()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var seeded = await SeedBlockAsync("user-1", "user-2");

            await _repo.RemoveAsync(seeded);
            await _repo.SaveChangesAsync();

            var deleted = await _context.UserBlocks
                .FirstOrDefaultAsync(b => b.BlockerId == "user-1" && b.BlockedId == "user-2");
            Assert.Null(deleted);
        }

        [Fact]
        public async Task Remove_DoesNotAffectOtherBlocks()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedUserAsync("user-3");
            var blockToRemove = await SeedBlockAsync("user-1", "user-2");
            await SeedBlockAsync("user-1", "user-3"); //should stay

            _repo.Remove(blockToRemove);
            await _repo.SaveChangesAsync();

            var remaining = await _context.UserBlocks
                .Where(b => b.BlockerId == "user-1")
                .ToListAsync();
            Assert.Single(remaining);
            Assert.Equal("user-3", remaining[0].BlockedId);
        }
    }
}