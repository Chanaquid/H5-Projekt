using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class FineRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly FineRepository _repo;

        public FineRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new FineRepository(_context);
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

        private async Task<Loan> SeedLoanAsync(string ownerId, string borrowerId)
        {
            var item = new Item
            {
                OwnerId = ownerId,
                Title = "Test Item",
                Status = ItemStatus.Approved,
                IsActive = true,
                Condition = ItemCondition.Good,
                AvailableFrom = DateTime.UtcNow.Date,
                AvailableUntil = DateTime.UtcNow.Date.AddDays(30),
                RowVersion = Guid.NewGuid().ToByteArray()
            };
            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            var loan = new Loan
            {
                ItemId = item.Id,
                BorrowerId = borrowerId,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(5),
                Status = LoanStatus.Active,
                SnapshotCondition = ItemCondition.Good,
                CreatedAt = DateTime.UtcNow
            };
            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();
            return loan;
        }

        private async Task<Fine> SeedFineAsync(
            string userId,
            int? loanId = null,
            FineStatus status = FineStatus.Unpaid,
            FineType type = FineType.Late,
            decimal amount = 100m,
            DateTime? createdAt = null)
        {
            var fine = new Fine
            {
                UserId = userId,
                LoanId = loanId,
                Type = type,
                Status = status,
                Amount = amount,
                ItemValueAtTimeOfFine = 500m,
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
            _context.Fines.Add(fine);
            await _context.SaveChangesAsync();
            return fine;
        }


        [Fact]
        public async Task GetByUserIdAsync_ReturnsFinesForUser()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedFineAsync("user-1");
            await SeedFineAsync("user-1");
            await SeedFineAsync("user-2"); //different user

            var result = await _repo.GetByUserIdAsync("user-1");

            Assert.Equal(2, result.Count);
            Assert.All(result, f => Assert.Equal("user-1", f.UserId));
        }

        [Fact]
        public async Task GetByUserIdAsync_IncludesLoanAndItem()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedFineAsync("borrower-1", loanId: loan.Id);

            var result = await _repo.GetByUserIdAsync("borrower-1");

            Assert.Single(result);
            Assert.NotNull(result[0].Loan);
            Assert.NotNull(result[0].Loan!.Item);
        }

        [Fact]
        public async Task GetByUserIdAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("user-1");
            var older = await SeedFineAsync("user-1", createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedFineAsync("user-1", createdAt: DateTime.UtcNow);

            var result = await _repo.GetByUserIdAsync("user-1");

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetByUserIdAsync_NoFines_ReturnsEmpty()
        {
            var result = await _repo.GetByUserIdAsync("user-1");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllUnpaidAsync_ReturnsOnlyUnpaidFines()
        {
            await SeedUserAsync("user-1");
            await SeedFineAsync("user-1", status: FineStatus.Unpaid);
            await SeedFineAsync("user-1", status: FineStatus.Paid);
            await SeedFineAsync("user-1", status: FineStatus.Waived);
            await SeedFineAsync("user-1", status: FineStatus.PendingVerification);

            var result = await _repo.GetAllUnpaidAsync();

            Assert.Single(result);
            Assert.Equal(FineStatus.Unpaid, result[0].Status);
        }

        [Fact]
        public async Task GetAllUnpaidAsync_IncludesLoanItemAndUser()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedFineAsync("borrower-1", loanId: loan.Id, status: FineStatus.Unpaid);

            var result = await _repo.GetAllUnpaidAsync();

            Assert.Single(result);
            Assert.NotNull(result[0].Loan);
            Assert.NotNull(result[0].Loan!.Item);
            Assert.NotNull(result[0].User);
        }

        [Fact]
        public async Task GetAllUnpaidAsync_NoUnpaidFines_ReturnsEmpty()
        {
            await SeedUserAsync("user-1");
            await SeedFineAsync("user-1", status: FineStatus.Paid);

            var result = await _repo.GetAllUnpaidAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsFine()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedFineAsync("user-1");

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
        public async Task GetByIdWithDetailsAsync_ExistingId_IncludesLoanAndItem()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedFineAsync("borrower-1", loanId: loan.Id);

            var result = await _repo.GetByIdWithDetailsAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.NotNull(result!.Loan);
            Assert.NotNull(result.Loan!.Item);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_ExistingId_IncludesUser()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedFineAsync("user-1");

            var result = await _repo.GetByIdWithDetailsAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.NotNull(result!.User);
            Assert.Equal("user-1", result.User.Id);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetByIdWithDetailsAsync(999);

            Assert.Null(result);
        }


        [Fact]
        public async Task GetPendingVerificationAsync_ReturnsOnlyPendingVerification()
        {
            await SeedUserAsync("user-1");
            await SeedFineAsync("user-1", status: FineStatus.PendingVerification);
            await SeedFineAsync("user-1", status: FineStatus.Unpaid);
            await SeedFineAsync("user-1", status: FineStatus.Paid);

            var result = await _repo.GetPendingVerificationAsync();

            Assert.Single(result);
            Assert.Equal(FineStatus.PendingVerification, result[0].Status);
        }

        [Fact]
        public async Task GetPendingVerificationAsync_IncludesLoanItemAndUser()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedFineAsync("borrower-1", loanId: loan.Id, status: FineStatus.PendingVerification);

            var result = await _repo.GetPendingVerificationAsync();

            Assert.Single(result);
            Assert.NotNull(result[0].Loan);
            Assert.NotNull(result[0].Loan!.Item);
            Assert.NotNull(result[0].User);
        }

        [Fact]
        public async Task GetPendingVerificationAsync_OrderedByCreatedAtAscending()
        {
            await SeedUserAsync("user-1");
            var newer = await SeedFineAsync("user-1", status: FineStatus.PendingVerification, createdAt: DateTime.UtcNow);
            var older = await SeedFineAsync("user-1", status: FineStatus.PendingVerification, createdAt: DateTime.UtcNow.AddMinutes(-10));

            var result = await _repo.GetPendingVerificationAsync();

            Assert.Equal(older.Id, result[0].Id); //oldest first
            Assert.Equal(newer.Id, result[1].Id);
        }

        [Fact]
        public async Task GetPendingVerificationAsync_NoPendingFines_ReturnsEmpty()
        {
            await SeedUserAsync("user-1");
            await SeedFineAsync("user-1", status: FineStatus.Unpaid);

            var result = await _repo.GetPendingVerificationAsync();

            Assert.Empty(result);
        }


        [Fact]
        public async Task AddAsync_SaveChangesAsync_PersistsFine()
        {
            await SeedUserAsync("user-1");

            var fine = new Fine
            {
                UserId = "user-1",
                Type = FineType.Late,
                Status = FineStatus.Unpaid,
                Amount = 100m,
                ItemValueAtTimeOfFine = 500m,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(fine);
            await _repo.SaveChangesAsync();

            var saved = await _context.Fines.FirstOrDefaultAsync(f => f.UserId == "user-1");
            Assert.NotNull(saved);
            Assert.Equal(100m, saved!.Amount);
            Assert.Equal(FineType.Late, saved.Type);
            Assert.Equal(FineStatus.Unpaid, saved.Status);
        }

        [Fact]
        public async Task Update_SaveChangesAsync_PersistsStatusChange()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedFineAsync("user-1", status: FineStatus.Unpaid);

            seeded.Status = FineStatus.PendingVerification;
            seeded.PaymentProofImageUrl = "http://test.com/proof.jpg";
            seeded.PaymentDescription = "Paid via MobilePay";
            seeded.PaidAt = DateTime.UtcNow;
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.Fines.FindAsync(seeded.Id);
            Assert.Equal(FineStatus.PendingVerification, updated!.Status);
            Assert.Equal("http://test.com/proof.jpg", updated.PaymentProofImageUrl);
            Assert.Equal("Paid via MobilePay", updated.PaymentDescription);
            Assert.NotNull(updated.PaidAt);
        }

        [Fact]
        public async Task Update_SaveChangesAsync_AdminVerifiesFine()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedFineAsync("user-1", status: FineStatus.PendingVerification);

            seeded.Status = FineStatus.Paid;
            seeded.VerifiedAt = DateTime.UtcNow;
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.Fines.FindAsync(seeded.Id);
            Assert.Equal(FineStatus.Paid, updated!.Status);
            Assert.NotNull(updated.VerifiedAt);
        }

        [Fact]
        public async Task Update_SaveChangesAsync_AdminRejectsFineProof()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedFineAsync("user-1", status: FineStatus.PendingVerification);

            seeded.Status = FineStatus.Rejected;
            seeded.RejectionReason = "Proof image is unclear.";
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.Fines.FindAsync(seeded.Id);
            Assert.Equal(FineStatus.Rejected, updated!.Status);
            Assert.Equal("Proof image is unclear.", updated.RejectionReason);
        }
    }
}