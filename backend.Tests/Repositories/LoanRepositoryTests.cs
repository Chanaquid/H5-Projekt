using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class LoanRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly LoanRepository _repo;

        public LoanRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new LoanRepository(_context);
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
            var category = await SeedCategoryAsync();

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


        private async Task<Loan> SeedLoanAsync(
            string ownerId,
            string borrowerId,
            LoanStatus status = LoanStatus.Pending,
            DateTime? endDate = null,
            DateTime? createdAt = null)
                {
                var item = await SeedItemAsync(ownerId);

                var loan = new Loan
                {
                    ItemId = item.Id,
                    BorrowerId = borrowerId,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = endDate ?? DateTime.UtcNow.Date.AddDays(5),
                    Status = status,
                    SnapshotCondition = ItemCondition.Good,
                    CreatedAt = createdAt ?? DateTime.UtcNow
                };
                _context.Loans.Add(loan);
                await _context.SaveChangesAsync();
                return loan;
        }

        private async Task<Category> SeedCategoryAsync()
        {
            var category = new Category { Name = "Tools", Icon = "🔧", IsActive = true };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }






        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsLoan()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var seeded = await SeedLoanAsync("owner-1", "borrower-1");

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
        public async Task GetByIdWithDetailsAsync_ExistingId_IncludesItemAndBorrower()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var seeded = await SeedLoanAsync("owner-1", "borrower-1");

            _context.ChangeTracker.Clear();

            var result = await _repo.GetByIdWithDetailsAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.NotNull(result!.Item);
            Assert.NotNull(result.Item.Owner);
            Assert.Equal("owner-1", result.Item.Owner.Id);
            Assert.NotNull(result.Borrower);
            Assert.Equal("borrower-1", result.Borrower.Id);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_IncludesSnapshotPhotos()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var seeded = await SeedLoanAsync("owner-1", "borrower-1");

            _context.LoanSnapshotPhotos.Add(new LoanSnapshotPhoto
            {
                LoanId = seeded.Id,
                PhotoUrl = "http://test.com/photo.jpg",
                DisplayOrder = 1,
                SnapshotTakenAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            _context.ChangeTracker.Clear();

            var result = await _repo.GetByIdWithDetailsAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.Single(result!.SnapshotPhotos);
        }


        [Fact]
        public async Task GetByIdWithDetailsAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetByIdWithDetailsAsync(999);

            Assert.Null(result);
        }


        [Fact]
        public async Task GetByBorrowerIdAsync_ReturnsOnlyBorrowerLoans()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            await SeedUserAsync("borrower-2");
            await SeedLoanAsync("owner-1", "borrower-1");
            await SeedLoanAsync("owner-1", "borrower-1");
            await SeedLoanAsync("owner-1", "borrower-2"); //different borrower

            var result = await _repo.GetByBorrowerIdAsync("borrower-1");

            Assert.Equal(2, result.Count);
            Assert.All(result, l => Assert.Equal("borrower-1", l.BorrowerId));
        }

        [Fact]
        public async Task GetByBorrowerIdAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var older = await SeedLoanAsync("owner-1", "borrower-1", createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedLoanAsync("owner-1", "borrower-1", createdAt: DateTime.UtcNow);

            var result = await _repo.GetByBorrowerIdAsync("borrower-1");

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetByBorrowerIdAsync_NoLoans_ReturnsEmpty()
        {
            var result = await _repo.GetByBorrowerIdAsync("borrower-1");

            Assert.Empty(result);
        }


        [Fact]
        public async Task GetByOwnerIdAsync_ReturnsOnlyOwnerLoans()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("owner-2");
            await SeedUserAsync("borrower-1");
            await SeedLoanAsync("owner-1", "borrower-1");
            await SeedLoanAsync("owner-1", "borrower-1");
            await SeedLoanAsync("owner-2", "borrower-1"); //different owner

            var result = await _repo.GetByOwnerIdAsync("owner-1");

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByOwnerIdAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var older = await SeedLoanAsync("owner-1", "borrower-1", createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedLoanAsync("owner-1", "borrower-1", createdAt: DateTime.UtcNow);

            var result = await _repo.GetByOwnerIdAsync("owner-1");

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetByOwnerIdAsync_NoLoans_ReturnsEmpty()
        {
            var result = await _repo.GetByOwnerIdAsync("owner-1");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllLoans()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Pending);
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Active);
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Returned);

            var result = await _repo.GetAllAsync();

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var older = await SeedLoanAsync("owner-1", "borrower-1", createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedLoanAsync("owner-1", "borrower-1", createdAt: DateTime.UtcNow);

            var result = await _repo.GetAllAsync();

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetAllAsync_NoLoans_ReturnsEmpty()
        {
            var result = await _repo.GetAllAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPendingAdminApprovalsAsync_ReturnsOnlyAdminPending()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.AdminPending);
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Pending);
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Approved);

            var result = await _repo.GetPendingAdminApprovalsAsync();

            Assert.Single(result);
            Assert.Equal(LoanStatus.AdminPending, result[0].Status);
        }

        [Fact]
        public async Task GetPendingAdminApprovalsAsync_OrderedByCreatedAtAscending()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var newer = await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.AdminPending, createdAt: DateTime.UtcNow);
            var older = await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.AdminPending, createdAt: DateTime.UtcNow.AddMinutes(-10));

            var result = await _repo.GetPendingAdminApprovalsAsync();

            Assert.Equal(older.Id, result[0].Id); //oldest first — FIFO
            Assert.Equal(newer.Id, result[1].Id);
        }

        [Fact]
        public async Task GetPendingAdminApprovalsAsync_NoAdminPending_ReturnsEmpty()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Pending);

            var result = await _repo.GetPendingAdminApprovalsAsync();

            Assert.Empty(result);
        }


        [Fact]
        public async Task GetActiveAndOverdueAsync_ReturnsActiveAndLateOverdueLoans()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var pastDate = DateTime.UtcNow.Date.AddDays(-3);

            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Active, endDate: pastDate);
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Late, endDate: pastDate);
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Active, endDate: DateTime.UtcNow.Date.AddDays(5)); //not overdue
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Returned, endDate: pastDate); //returned

            var result = await _repo.GetActiveAndOverdueAsync();

            Assert.Equal(2, result.Count);
            Assert.All(result, l => Assert.True(l.EndDate < DateTime.UtcNow));
            Assert.All(result, l => Assert.True(
                l.Status == LoanStatus.Active || l.Status == LoanStatus.Late));
        }

        [Fact]
        public async Task GetActiveAndOverdueAsync_NoOverdueLoans_ReturnsEmpty()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Active,
                endDate: DateTime.UtcNow.Date.AddDays(5)); // not overdue

            var result = await _repo.GetActiveAndOverdueAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task HasActiveLoansAsync_BorrowerHasActiveLoan_ReturnsTrue()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Active);

            var result = await _repo.HasActiveLoansAsync("borrower-1");

            Assert.True(result);
        }

        [Fact]
        public async Task HasActiveLoansAsync_OwnerHasPendingLoan_ReturnsTrue()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Pending);

            var result = await _repo.HasActiveLoansAsync("owner-1");

            Assert.True(result);
        }

        [Fact]
        public async Task HasActiveLoansAsync_OnlyReturnedLoans_ReturnsFalse()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Returned);

            var result = await _repo.HasActiveLoansAsync("borrower-1");

            Assert.False(result);
        }

        [Fact]
        public async Task HasActiveLoansAsync_NoLoans_ReturnsFalse()
        {
            var result = await _repo.HasActiveLoansAsync("borrower-1");

            Assert.False(result);
        }

        [Fact]
        public async Task HasActiveLoansAsync_AllActiveStatuses_ReturnsTrue()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");

            //Each active status should return true
            foreach (var status in new[] { LoanStatus.Pending, LoanStatus.AdminPending, LoanStatus.Approved, LoanStatus.Active })
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;
                using var context = new AppDbContext(options);
                var repo = new LoanRepository(context);

                context.Users.Add(new ApplicationUser
                {
                    Id = "owner-1",
                    UserName = "o@test.com",
                    Email = "o@test.com",
                    NormalizedEmail = "O@TEST.COM",
                    NormalizedUserName = "O@TEST.COM",
                    FullName = "Owner"
                });
                context.Users.Add(new ApplicationUser
                {
                    Id = "borrower-1",
                    UserName = "b@test.com",
                    Email = "b@test.com",
                    NormalizedEmail = "B@TEST.COM",
                    NormalizedUserName = "B@TEST.COM",
                    FullName = "Borrower"
                });
                var item = new Item
                {
                    OwnerId = "owner-1",
                    Title = "Item",
                    Description = "Desc",
                    Status = ItemStatus.Approved,
                    IsActive = true,
                    Condition = ItemCondition.Good,
                    AvailableFrom = DateTime.UtcNow.Date,
                    AvailableUntil = DateTime.UtcNow.Date.AddDays(30),
                    QrCode = Guid.NewGuid().ToString("N")[..12].ToUpper(),
                    RowVersion = Guid.NewGuid().ToByteArray()
                };
                context.Items.Add(item);
                await context.SaveChangesAsync();

                context.Loans.Add(new Loan
                {
                    ItemId = item.Id,
                    BorrowerId = "borrower-1",
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddDays(5),
                    Status = status,
                    SnapshotCondition = ItemCondition.Good,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();

                var result = await repo.HasActiveLoansAsync("borrower-1");
                Assert.True(result, $"Expected true for status {status}");
            }
        }


        [Fact]
        public async Task AddAsync_SaveChangesAsync_PersistsLoan()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var item = await SeedItemAsync("owner-1");

            var loan = new Loan
            {
                ItemId = item.Id,
                BorrowerId = "borrower-1",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(7),
                Status = LoanStatus.Pending,
                SnapshotCondition = ItemCondition.Good,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(loan);
            await _repo.SaveChangesAsync();

            var saved = await _context.Loans.FirstOrDefaultAsync(l => l.BorrowerId == "borrower-1");
            Assert.NotNull(saved);
            Assert.Equal(LoanStatus.Pending, saved!.Status);
            Assert.Equal("borrower-1", saved.BorrowerId);
        }

        [Fact]
        public async Task Update_SaveChangesAsync_PersistsStatusChange()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var seeded = await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Pending);

            seeded.Status = LoanStatus.Approved;
            seeded.DecisionNote = "Approved by owner.";
            seeded.UpdatedAt = DateTime.UtcNow;
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.Loans.FindAsync(seeded.Id);
            Assert.Equal(LoanStatus.Approved, updated!.Status);
            Assert.Equal("Approved by owner.", updated.DecisionNote);
            Assert.NotNull(updated.UpdatedAt);
        }

        [Fact]
        public async Task Update_SaveChangesAsync_PersistsExtensionRequest()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var seeded = await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Active);
            var newEndDate = DateTime.UtcNow.Date.AddDays(10);

            seeded.RequestedExtensionDate = newEndDate;
            seeded.ExtensionRequestStatus = ExtensionStatus.Pending;
            seeded.UpdatedAt = DateTime.UtcNow;
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.Loans.FindAsync(seeded.Id);
            Assert.Equal(newEndDate, updated!.RequestedExtensionDate);
            Assert.Equal(ExtensionStatus.Pending, updated.ExtensionRequestStatus);
        }

        [Fact]
        public async Task Update_SaveChangesAsync_PersistsActualReturnDate()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var seeded = await SeedLoanAsync("owner-1", "borrower-1", LoanStatus.Active);
            var returnDate = DateTime.UtcNow;

            seeded.Status = LoanStatus.Returned;
            seeded.ActualReturnDate = returnDate;
            seeded.UpdatedAt = DateTime.UtcNow;
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.Loans.FindAsync(seeded.Id);
            Assert.Equal(LoanStatus.Returned, updated!.Status);
            Assert.NotNull(updated.ActualReturnDate);
        }
    }
}