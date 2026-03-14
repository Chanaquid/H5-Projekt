using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class DisputeRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly DisputeRepository _repo;

        public DisputeRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new DisputeRepository(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        // ───────────────────────────────────────────
        // Helpers
        // ───────────────────────────────────────────

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

        private async Task<Dispute> SeedDisputeAsync(
            int loanId,
            string filedById,
            DisputeStatus status = DisputeStatus.Open,
            DisputeFiledAs filedAs = DisputeFiledAs.AsOwner,
            DateTime? createdAt = null)
        {
            var dispute = new Dispute
            {
                LoanId = loanId,
                FiledById = filedById,
                FiledAs = filedAs,
                Description = "Test description",
                ResponseDeadline = DateTime.UtcNow.AddHours(72),
                Status = status,
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
            _context.Disputes.Add(dispute);
            await _context.SaveChangesAsync();
            return dispute;
        }

        // ───────────────────────────────────────────
        // GetByIdAsync
        // ───────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDispute()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedDisputeAsync(loan.Id, "owner-1");

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

        // ───────────────────────────────────────────
        // GetByIdWithDetailsAsync
        // ───────────────────────────────────────────

        [Fact]
        public async Task GetByIdWithDetailsAsync_ExistingId_IncludesFiledBy()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedDisputeAsync(loan.Id, "owner-1");

            var result = await _repo.GetByIdWithDetailsAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.NotNull(result!.FiledBy);
            Assert.Equal("owner-1", result.FiledBy.Id);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_ExistingId_IncludesLoanAndItem()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedDisputeAsync(loan.Id, "owner-1");

            var result = await _repo.GetByIdWithDetailsAsync(seeded.Id);

            Assert.NotNull(result!.Loan);
            Assert.NotNull(result.Loan.Item);
            Assert.NotNull(result.Loan.Item.Owner);
            Assert.NotNull(result.Loan.Borrower);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_IncludesPhotos()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedDisputeAsync(loan.Id, "owner-1");

            _context.DisputePhotos.Add(new DisputePhoto
            {
                DisputeId = seeded.Id,
                SubmittedById = "owner-1",
                PhotoUrl = "http://test.com/photo.jpg",
                Caption = "Scratch on the front",
                UploadedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var result = await _repo.GetByIdWithDetailsAsync(seeded.Id);

            Assert.Single(result!.Photos);
            Assert.Equal("Scratch on the front", result.Photos.First().Caption);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetByIdWithDetailsAsync(999);

            Assert.Null(result);
        }

        // ───────────────────────────────────────────
        // GetByUserIdAsync
        // ───────────────────────────────────────────

        [Fact]
        public async Task GetByUserIdAsync_AsFiledBy_ReturnsDispute()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedDisputeAsync(loan.Id, "owner-1");

            var result = await _repo.GetByUserIdAsync("owner-1");

            Assert.Single(result);
            Assert.Equal("owner-1", result[0].FiledById);
        }

        [Fact]
        public async Task GetByUserIdAsync_AsBorrower_ReturnsDispute()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedDisputeAsync(loan.Id, "owner-1"); // filed by owner

            // borrower is involved in the loan so should see it
            var result = await _repo.GetByUserIdAsync("borrower-1");

            Assert.Single(result);
        }

        [Fact]
        public async Task GetByUserIdAsync_AsItemOwner_ReturnsDispute()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedDisputeAsync(loan.Id, "borrower-1", DisputeStatus.Open, DisputeFiledAs.AsBorrower);

            var result = await _repo.GetByUserIdAsync("owner-1");

            Assert.Single(result);
        }

        [Fact]
        public async Task GetByUserIdAsync_UnrelatedUser_ReturnsEmpty()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedDisputeAsync(loan.Id, "owner-1");

            var result = await _repo.GetByUserIdAsync("unrelated-user");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByUserIdAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var older = await SeedDisputeAsync(loan.Id, "owner-1", createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedDisputeAsync(loan.Id, "owner-1", createdAt: DateTime.UtcNow);

            var result = await _repo.GetByUserIdAsync("owner-1");

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        // ───────────────────────────────────────────
        // GetAllOpenAsync
        // ───────────────────────────────────────────

        [Fact]
        public async Task GetAllOpenAsync_ReturnsOnlyNonResolved()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedDisputeAsync(loan.Id, "owner-1", DisputeStatus.Open);
            await SeedDisputeAsync(loan.Id, "owner-1", DisputeStatus.AwaitingResponse);
            await SeedDisputeAsync(loan.Id, "owner-1", DisputeStatus.UnderReview);
            await SeedDisputeAsync(loan.Id, "owner-1", DisputeStatus.Resolved);

            var result = await _repo.GetAllOpenAsync();

            Assert.Equal(3, result.Count);
            Assert.All(result, d => Assert.NotEqual(DisputeStatus.Resolved, d.Status));
        }

        [Fact]
        public async Task GetAllOpenAsync_OrderedByCreatedAtAscending()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var newer = await SeedDisputeAsync(loan.Id, "owner-1", createdAt: DateTime.UtcNow);
            var older = await SeedDisputeAsync(loan.Id, "owner-1", createdAt: DateTime.UtcNow.AddMinutes(-10));

            var result = await _repo.GetAllOpenAsync();

            Assert.Equal(older.Id, result[0].Id); // oldest first — FIFO
            Assert.Equal(newer.Id, result[1].Id);
        }

        [Fact]
        public async Task GetAllOpenAsync_NoOpenDisputes_ReturnsEmpty()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedDisputeAsync(loan.Id, "owner-1", DisputeStatus.Resolved);

            var result = await _repo.GetAllOpenAsync();

            Assert.Empty(result);
        }

        // ───────────────────────────────────────────
        // AddAsync + SaveChangesAsync
        // ───────────────────────────────────────────

        [Fact]
        public async Task AddAsync_SaveChangesAsync_PersistsDispute()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");

            var dispute = new Dispute
            {
                LoanId = loan.Id,
                FiledById = "owner-1",
                FiledAs = DisputeFiledAs.AsOwner,
                Description = "Item was damaged.",
                ResponseDeadline = DateTime.UtcNow.AddHours(72),
                Status = DisputeStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(dispute);
            await _repo.SaveChangesAsync();

            var saved = await _context.Disputes.FirstOrDefaultAsync(d => d.LoanId == loan.Id);
            Assert.NotNull(saved);
            Assert.Equal("Item was damaged.", saved!.Description);
            Assert.Equal(DisputeFiledAs.AsOwner, saved.FiledAs);
            Assert.Equal(DisputeStatus.Open, saved.Status);
        }

        // ───────────────────────────────────────────
        // AddPhotoAsync + SaveChangesAsync
        // ───────────────────────────────────────────

        [Fact]
        public async Task AddPhotoAsync_SaveChangesAsync_PersistsPhoto()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var dispute = await SeedDisputeAsync(loan.Id, "owner-1");

            var photo = new DisputePhoto
            {
                DisputeId = dispute.Id,
                SubmittedById = "owner-1",
                PhotoUrl = "http://test.com/photo.jpg",
                Caption = "Scratch on the front",
                UploadedAt = DateTime.UtcNow
            };

            await _repo.AddPhotoAsync(photo);
            await _repo.SaveChangesAsync();

            var saved = await _context.DisputePhotos
                .FirstOrDefaultAsync(p => p.DisputeId == dispute.Id);
            Assert.NotNull(saved);
            Assert.Equal("http://test.com/photo.jpg", saved!.PhotoUrl);
            Assert.Equal("Scratch on the front", saved.Caption);
            Assert.Equal("owner-1", saved.SubmittedById);
        }

        // ───────────────────────────────────────────
        // Update + SaveChangesAsync
        // ───────────────────────────────────────────

        [Fact]
        public async Task Update_SaveChangesAsync_PersistsChanges()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedDisputeAsync(loan.Id, "owner-1", DisputeStatus.Open);

            seeded.Status = DisputeStatus.Resolved;
            seeded.AdminNote = "Resolved in favour of owner.";
            seeded.AdminVerdict = DisputeVerdict.OwnerFavored;
            seeded.ResolvedAt = DateTime.UtcNow;
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.Disputes.FindAsync(seeded.Id);
            Assert.Equal(DisputeStatus.Resolved, updated!.Status);
            Assert.Equal("Resolved in favour of owner.", updated.AdminNote);
            Assert.Equal(DisputeVerdict.OwnerFavored, updated.AdminVerdict);
            Assert.NotNull(updated.ResolvedAt);
        }

        [Fact]
        public async Task Update_ResponseDescription_PersistsChanges()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedDisputeAsync(loan.Id, "owner-1", DisputeStatus.AwaitingResponse);

            seeded.ResponseDescription = "The item was already scratched when I received it.";
            seeded.Status = DisputeStatus.UnderReview;
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.Disputes.FindAsync(seeded.Id);
            Assert.Equal("The item was already scratched when I received it.", updated!.ResponseDescription);
            Assert.Equal(DisputeStatus.UnderReview, updated.Status);
        }
    }
}