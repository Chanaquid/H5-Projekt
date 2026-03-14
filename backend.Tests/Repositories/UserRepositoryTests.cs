using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UserRepository _repo;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new UserRepository(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<ApplicationUser> SeedUserAsync(
            string id,
            bool isDeleted = false,
            string fullName = "Test User")
        {
            var user = new ApplicationUser
            {
                Id = id,
                UserName = $"{id}@test.com",
                Email = $"{id}@test.com",
                FullName = fullName,
                Address = "Test Address",
                NormalizedEmail = $"{id}@test.com".ToUpper(),
                NormalizedUserName = $"{id}@test.com".ToUpper(),
                Score = 100,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTime.UtcNow : null,
                MembershipDate = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private async Task<ScoreHistory> SeedScoreHistoryAsync(
            string userId,
            int pointsChanged = 5,
            ScoreChangeReason reason = ScoreChangeReason.OnTimeReturn,
            DateTime? createdAt = null)
        {
            var entry = new ScoreHistory
            {
                UserId = userId,
                PointsChanged = pointsChanged,
                ScoreAfterChange = 100 + pointsChanged,
                Reason = reason,
                Note = "Test score change",
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
            _context.ScoreHistories.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUser()
        {
            await SeedUserAsync("user-1");

            var result = await _repo.GetByIdAsync("user-1");

            Assert.NotNull(result);
            Assert.Equal("user-1", result!.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetByIdAsync("nonexistent");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_DeletedUser_ReturnsNull()
        {
            //Soft-deleted users are filtered out by default query filter
            await SeedUserAsync("user-1", isDeleted: true);

            var result = await _repo.GetByIdAsync("user-1");

            Assert.Null(result);
        }


        [Fact]
        public async Task GetByIdWithDetailsAsync_ExistingId_ReturnsUser()
        {
            await SeedUserAsync("user-1");

            var result = await _repo.GetByIdWithDetailsAsync("user-1");

            Assert.NotNull(result);
            Assert.Equal("user-1", result!.Id);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_IncludesScoreHistory()
        {
            await SeedUserAsync("user-1");
            await SeedScoreHistoryAsync("user-1", 5);
            await SeedScoreHistoryAsync("user-1", -5);

            _context.ChangeTracker.Clear();

            var result = await _repo.GetByIdWithDetailsAsync("user-1");

            Assert.NotNull(result);
            Assert.Equal(2, result!.ScoreHistory.Count);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetByIdWithDetailsAsync("nonexistent");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdIgnoreFiltersAsync_ActiveUser_ReturnsUser()
        {
            await SeedUserAsync("user-1");

            var result = await _repo.GetByIdIgnoreFiltersAsync("user-1");

            Assert.NotNull(result);
            Assert.Equal("user-1", result!.Id);
        }

        [Fact]
        public async Task GetByIdIgnoreFiltersAsync_DeletedUser_ReturnsUser()
        {
            await SeedUserAsync("user-1", isDeleted: true);

            var result = await _repo.GetByIdIgnoreFiltersAsync("user-1");

            Assert.NotNull(result);
            Assert.Equal("user-1", result!.Id);
            Assert.True(result.IsDeleted);
        }

        [Fact]
        public async Task GetByIdIgnoreFiltersAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetByIdIgnoreFiltersAsync("nonexistent");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsersIncludingDeleted()
        {
            await SeedUserAsync("user-1", isDeleted: false);
            await SeedUserAsync("user-2", isDeleted: true);
            await SeedUserAsync("user-3", isDeleted: false);

            var result = await _repo.GetAllAsync();

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_OrderedByFullNameAscending()
        {
            await SeedUserAsync("user-1", fullName: "Charlie");
            await SeedUserAsync("user-2", fullName: "Alice");
            await SeedUserAsync("user-3", fullName: "Bob");

            var result = await _repo.GetAllAsync();

            Assert.Equal("Alice", result[0].FullName);
            Assert.Equal("Bob", result[1].FullName);
            Assert.Equal("Charlie", result[2].FullName);
        }

        [Fact]
        public async Task GetAllAsync_NoUsers_ReturnsEmpty()
        {
            var result = await _repo.GetAllAsync();

            Assert.Empty(result);
        }


        [Fact]
        public async Task GetScoreHistoryAsync_ReturnsOnlyUserHistory()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedScoreHistoryAsync("user-1");
            await SeedScoreHistoryAsync("user-1");
            await SeedScoreHistoryAsync("user-2"); //different user

            var result = await _repo.GetScoreHistoryAsync("user-1");

            Assert.Equal(2, result.Count);
            Assert.All(result, s => Assert.Equal("user-1", s.UserId));
        }

        [Fact]
        public async Task GetScoreHistoryAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("user-1");
            var older = await SeedScoreHistoryAsync("user-1", createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedScoreHistoryAsync("user-1", createdAt: DateTime.UtcNow);

            var result = await _repo.GetScoreHistoryAsync("user-1");

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetScoreHistoryAsync_ReturnsAllReasons()
        {
            await SeedUserAsync("user-1");
            await SeedScoreHistoryAsync("user-1", 5, ScoreChangeReason.OnTimeReturn);
            await SeedScoreHistoryAsync("user-1", -5, ScoreChangeReason.LateReturn);
            await SeedScoreHistoryAsync("user-1", 10, ScoreChangeReason.AdminAdjustment);

            var result = await _repo.GetScoreHistoryAsync("user-1");

            Assert.Equal(3, result.Count);
            Assert.Contains(result, s => s.Reason == ScoreChangeReason.OnTimeReturn);
            Assert.Contains(result, s => s.Reason == ScoreChangeReason.LateReturn);
            Assert.Contains(result, s => s.Reason == ScoreChangeReason.AdminAdjustment);
        }

        [Fact]
        public async Task GetScoreHistoryAsync_NoHistory_ReturnsEmpty()
        {
            var result = await _repo.GetScoreHistoryAsync("user-1");

            Assert.Empty(result);
        }

        [Fact]
        public async Task AddScoreHistoryAsync_SaveChangesAsync_PersistsEntry()
        {
            await SeedUserAsync("user-1");

            var entry = new ScoreHistory
            {
                UserId = "user-1",
                PointsChanged = 5,
                ScoreAfterChange = 105,
                Reason = ScoreChangeReason.OnTimeReturn,
                Note = "On-time return of 'Drill'.",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddScoreHistoryAsync(entry);
            await _repo.SaveChangesAsync();

            var saved = await _context.ScoreHistories
                .FirstOrDefaultAsync(s => s.UserId == "user-1");
            Assert.NotNull(saved);
            Assert.Equal(5, saved!.PointsChanged);
            Assert.Equal(105, saved.ScoreAfterChange);
            Assert.Equal(ScoreChangeReason.OnTimeReturn, saved.Reason);
            Assert.Equal("On-time return of 'Drill'.", saved.Note);
        }

        [Fact]
        public async Task AddScoreHistoryAsync_NegativePoints_PersistsCorrectly()
        {
            await SeedUserAsync("user-1");

            var entry = new ScoreHistory
            {
                UserId = "user-1",
                PointsChanged = -5,
                ScoreAfterChange = 95,
                Reason = ScoreChangeReason.LateReturn,
                Note = "Late return penalty.",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddScoreHistoryAsync(entry);
            await _repo.SaveChangesAsync();

            var saved = await _context.ScoreHistories
                .FirstOrDefaultAsync(s => s.UserId == "user-1");
            Assert.NotNull(saved);
            Assert.Equal(-5, saved!.PointsChanged);
            Assert.Equal(95, saved.ScoreAfterChange);
        }

 
        [Fact]
        public async Task UpdateAsync_ScoreChange_PersistsViaChangeTracking()
        {
            await SeedUserAsync("user-1");

            //fetch tracked entity and modify it
            var user = await _repo.GetByIdAsync("user-1");
            user!.Score = 75;
            user.UnpaidFinesTotal = 100m;

            await _repo.UpdateAsync(user);
            await _repo.SaveChangesAsync();

            var updated = await _context.Users.FindAsync("user-1");
            Assert.Equal(75, updated!.Score);
            Assert.Equal(100m, updated.UnpaidFinesTotal);
        }

        [Fact]
        public async Task UpdateAsync_SoftDelete_PersistsViaChangeTracking()
        {
            await SeedUserAsync("user-1");

            var user = await _repo.GetByIdAsync("user-1");
            user!.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(user);
            await _repo.SaveChangesAsync();

            //use IgnoreQueryFilters to find the soft-deleted user
            var updated = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == "user-1");
            Assert.NotNull(updated);
            Assert.True(updated!.IsDeleted);
            Assert.NotNull(updated.DeletedAt);
        }
    }
}