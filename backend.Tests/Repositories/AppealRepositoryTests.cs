using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;

namespace backend.Tests.Repositories
{
    public class AppealRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AppealRepository _repo;

        public AppealRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) //fresh DB per test
                .Options;

            _context = new AppDbContext(options);
            _repo = new AppealRepository(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }


        private async Task<ApplicationUser> SeedUserAsync(string id = "user-1")
        {
            var user = new ApplicationUser
            {
                Id = id,
                UserName = $"{id}@test.com",
                Email = $"{id}@test.com",
                FullName = "Test User",
                NormalizedEmail = $"{id}@test.com".ToUpper(),
                NormalizedUserName = $"{id}@test.com".ToUpper()
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private async Task<Appeal> SeedAppealAsync(
            string userId,
            AppealStatus status = AppealStatus.Pending,
            AppealType type = AppealType.Score,
            int? fineId = null)
        {
            var appeal = new Appeal
            {
                UserId = userId,
                AppealType = type,
                FineId = fineId,
                Message = "Test appeal",
                Status = status,
                CreatedAt = DateTime.UtcNow
            };
            _context.Appeals.Add(appeal);
            await _context.SaveChangesAsync();
            return appeal;
        }


        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsAppeal()
        {
            await SeedUserAsync();
            var seeded = await SeedAppealAsync("user-1");

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
        public async Task GetByIdWithDetailsAsync_ExistingId_IncludesUser()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedAppealAsync("user-1");

            var result = await _repo.GetByIdWithDetailsAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.NotNull(result!.User);
            Assert.Equal("user-1", result.User!.Id);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetByIdWithDetailsAsync(999);

            Assert.Null(result);
        }


        [Fact]
        public async Task GetPendingByUserIdAsync_HasPendingAppeal_ReturnsIt()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedAppealAsync("user-1", AppealStatus.Pending);

            var result = await _repo.GetPendingByUserIdAsync("user-1");

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
            Assert.Equal(AppealStatus.Pending, result.Status);
        }

        [Fact]
        public async Task GetPendingByUserIdAsync_OnlyHasResolvedAppeal_ReturnsNull()
        {
            await SeedUserAsync("user-1");
            await SeedAppealAsync("user-1", AppealStatus.Approved);

            var result = await _repo.GetPendingByUserIdAsync("user-1");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetPendingByUserIdAsync_NoAppeals_ReturnsNull()
        {
            var result = await _repo.GetPendingByUserIdAsync("user-1");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetPendingByUserIdAsync_DifferentUser_ReturnsNull()
        {
            await SeedUserAsync("user-1");
            await SeedAppealAsync("user-1", AppealStatus.Pending);

            var result = await _repo.GetPendingByUserIdAsync("user-2");

            Assert.Null(result);
        }


        [Fact]
        public async Task GetAllPendingAsync_ReturnsPendingOnly()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedAppealAsync("user-1", AppealStatus.Pending);
            await SeedAppealAsync("user-2", AppealStatus.Approved);
            await SeedAppealAsync("user-1", AppealStatus.Rejected);

            var result = await _repo.GetAllPendingAsync();

            Assert.Single(result);
            Assert.All(result, a => Assert.Equal(AppealStatus.Pending, a.Status));
        }

        [Fact]
        public async Task GetAllPendingAsync_OrderedByCreatedAtAscending()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");

            //Seed with slight delay to ensure different CreatedAt
            var older = new Appeal
            {
                UserId = "user-1",
                AppealType = AppealType.Score,
                Message = "Older",
                Status = AppealStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            var newer = new Appeal
            {
                UserId = "user-2",
                AppealType = AppealType.Score,
                Message = "Newer",
                Status = AppealStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _context.Appeals.AddRange(older, newer);
            await _context.SaveChangesAsync();

            var result = await _repo.GetAllPendingAsync();

            Assert.Equal(2, result.Count);
            Assert.True(result[0].CreatedAt <= result[1].CreatedAt); //FIFO order
        }

        [Fact]
        public async Task GetAllPendingAsync_NoPendingAppeals_ReturnsEmptyList()
        {
            await SeedUserAsync("user-1");
            await SeedAppealAsync("user-1", AppealStatus.Approved);

            var result = await _repo.GetAllPendingAsync();

            Assert.Empty(result);
        }


        [Fact]
        public async Task GetAllByUserIdAsync_ReturnsOnlyThatUsersAppeals()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedAppealAsync("user-1", AppealStatus.Pending);
            await SeedAppealAsync("user-1", AppealStatus.Approved);
            await SeedAppealAsync("user-2", AppealStatus.Pending);

            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Equal(2, result.Count);
            Assert.All(result, a => Assert.Equal("user-1", a.UserId));
        }

        [Fact]
        public async Task GetAllByUserIdAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("user-1");

            var older = new Appeal
            {
                UserId = "user-1",
                AppealType = AppealType.Score,
                Message = "Older",
                Status = AppealStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            var newer = new Appeal
            {
                UserId = "user-1",
                AppealType = AppealType.Score,
                Message = "Newer",
                Status = AppealStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };
            _context.Appeals.AddRange(older, newer);
            await _context.SaveChangesAsync();

            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.True(result[0].CreatedAt >= result[1].CreatedAt); //ewest first
        }

        [Fact]
        public async Task GetAllByUserIdAsync_NoAppeals_ReturnsEmptyList()
        {
            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Empty(result);
        }


        [Fact]
        public async Task GetPendingFineAppealByFineIdAsync_HasPendingFineAppeal_ReturnsIt()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedAppealAsync("user-1", AppealStatus.Pending, AppealType.Fine, fineId: 42);

            var result = await _repo.GetPendingFineAppealByFineIdAsync(42);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
        }

        [Fact]
        public async Task GetPendingFineAppealByFineIdAsync_AppealIsResolved_ReturnsNull()
        {
            await SeedUserAsync("user-1");
            await SeedAppealAsync("user-1", AppealStatus.Approved, AppealType.Fine, fineId: 42);

            var result = await _repo.GetPendingFineAppealByFineIdAsync(42);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetPendingFineAppealByFineIdAsync_DifferentFineId_ReturnsNull()
        {
            await SeedUserAsync("user-1");
            await SeedAppealAsync("user-1", AppealStatus.Pending, AppealType.Fine, fineId: 42);

            var result = await _repo.GetPendingFineAppealByFineIdAsync(99);

            Assert.Null(result);
        }


        [Fact]
        public async Task AddAsync_SaveChangesAsync_PersistsAppeal()
        {
            await SeedUserAsync("user-1");
            var appeal = new Appeal
            {
                UserId = "user-1",
                AppealType = AppealType.Score,
                Message = "Please restore my score.",
                Status = AppealStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(appeal);
            await _repo.SaveChangesAsync();

            var saved = await _context.Appeals.FirstOrDefaultAsync(a => a.UserId == "user-1");
            Assert.NotNull(saved);
            Assert.Equal("Please restore my score.", saved!.Message);
        }


        [Fact]
        public async Task Update_SaveChangesAsync_PersistsChanges()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedAppealAsync("user-1", AppealStatus.Pending);

            seeded.Status = AppealStatus.Approved;
            seeded.AdminNote = "Looks good.";
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.Appeals.FindAsync(seeded.Id);
            Assert.Equal(AppealStatus.Approved, updated!.Status);
            Assert.Equal("Looks good.", updated.AdminNote);
        }
    }
}