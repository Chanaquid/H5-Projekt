using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class ReviewRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ReviewRepository _repo;

        public ReviewRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new ReviewRepository(_context);
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

        private async Task<Loan> SeedLoanAsync(string ownerId, string borrowerId)
        {
            var item = await SeedItemAsync(ownerId);

            var loan = new Loan
            {
                ItemId = item.Id,
                BorrowerId = borrowerId,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(5),
                Status = LoanStatus.Returned,
                SnapshotCondition = ItemCondition.Good,
                CreatedAt = DateTime.UtcNow
            };
            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();
            return loan;
        }

        private async Task<ItemReview> SeedItemReviewAsync(
            int itemId,
            string reviewerId,
            int? loanId = null,
            int rating = 4,
            DateTime? createdAt = null)
        {
            var review = new ItemReview
            {
                ItemId = itemId,
                ReviewerId = reviewerId,
                LoanId = loanId,
                Rating = rating,
                Comment = "Great item!",
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
            _context.ItemReviews.Add(review);
            await _context.SaveChangesAsync();
            return review;
        }

        private async Task<UserReview> SeedUserReviewAsync(
            string reviewerId,
            string reviewedUserId,
            int? loanId = null,
            int rating = 4,
            DateTime? createdAt = null)
        {
            var review = new UserReview
            {
                ReviewerId = reviewerId,
                ReviewedUserId = reviewedUserId,
                LoanId = loanId,
                Rating = rating,
                Comment = "Great user!",
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
            _context.UserReviews.Add(review);
            await _context.SaveChangesAsync();
            return review;
        }

        [Fact]
        public async Task GetItemReviewByLoanIdAsync_ExistingLoanId_ReturnsReview()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedItemReviewAsync(loan.ItemId, "borrower-1", loanId: loan.Id);

            var result = await _repo.GetItemReviewByLoanIdAsync(loan.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
        }

        [Fact]
        public async Task GetItemReviewByLoanIdAsync_NonExistingLoanId_ReturnsNull()
        {
            var result = await _repo.GetItemReviewByLoanIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetItemReviewByLoanIdAsync_IncludesReviewer()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedItemReviewAsync(loan.ItemId, "borrower-1", loanId: loan.Id);

            _context.ChangeTracker.Clear();

            var result = await _repo.GetItemReviewByLoanIdAsync(loan.Id);

            Assert.NotNull(result!.Reviewer);
            Assert.Equal("borrower-1", result.Reviewer.Id);
        }

        [Fact]
        public async Task GetItemReviewByIdAsync_ExistingId_ReturnsReview()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedItemReviewAsync(loan.ItemId, "borrower-1");

            var result = await _repo.GetItemReviewByIdAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
        }

        [Fact]
        public async Task GetItemReviewByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetItemReviewByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetItemReviewByIdAsync_IncludesReviewer()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedItemReviewAsync(loan.ItemId, "borrower-1");

            _context.ChangeTracker.Clear();

            var result = await _repo.GetItemReviewByIdAsync(seeded.Id);

            Assert.NotNull(result!.Reviewer);
            Assert.Equal("borrower-1", result.Reviewer.Id);
        }


        [Fact]
        public async Task GetItemReviewsByItemIdAsync_ReturnsOnlyItemReviews()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            await SeedUserAsync("borrower-2");
            var loan1 = await SeedLoanAsync("owner-1", "borrower-1");
            var loan2 = await SeedLoanAsync("owner-1", "borrower-2");
            await SeedItemReviewAsync(loan1.ItemId, "borrower-1");
            await SeedItemReviewAsync(loan1.ItemId, "borrower-2");
            await SeedItemReviewAsync(loan2.ItemId, "borrower-1"); // different item

            var result = await _repo.GetItemReviewsByItemIdAsync(loan1.ItemId);

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(loan1.ItemId, r.ItemId));
        }

        [Fact]
        public async Task GetItemReviewsByItemIdAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            await SeedUserAsync("borrower-2");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var older = await SeedItemReviewAsync(loan.ItemId, "borrower-1", createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedItemReviewAsync(loan.ItemId, "borrower-2", createdAt: DateTime.UtcNow);

            var result = await _repo.GetItemReviewsByItemIdAsync(loan.ItemId);

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetItemReviewsByItemIdAsync_NoReviews_ReturnsEmpty()
        {
            var result = await _repo.GetItemReviewsByItemIdAsync(999);

            Assert.Empty(result);
        }


        [Fact]
        public async Task GetUserReviewByIdAsync_ExistingId_ReturnsReview()
        {
            await SeedUserAsync("reviewer-1");
            await SeedUserAsync("reviewed-1");
            var seeded = await SeedUserReviewAsync("reviewer-1", "reviewed-1");

            var result = await _repo.GetUserReviewByIdAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
        }

        [Fact]
        public async Task GetUserReviewByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetUserReviewByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserReviewByIdAsync_IncludesReviewer()
        {
            await SeedUserAsync("reviewer-1");
            await SeedUserAsync("reviewed-1");
            var seeded = await SeedUserReviewAsync("reviewer-1", "reviewed-1");

            _context.ChangeTracker.Clear();

            var result = await _repo.GetUserReviewByIdAsync(seeded.Id);

            Assert.NotNull(result!.Reviewer);
            Assert.Equal("reviewer-1", result.Reviewer.Id);
        }

        [Fact]
        public async Task GetUserReviewByIdAsync_WithLoan_IncludesLoanAndItem()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedUserReviewAsync("borrower-1", "owner-1", loanId: loan.Id);

            _context.ChangeTracker.Clear();

            var result = await _repo.GetUserReviewByIdAsync(seeded.Id);

            Assert.NotNull(result!.Loan);
            Assert.NotNull(result.Loan!.Item);
        }

        [Fact]
        public async Task GetUserReviewByLoanAndReviewerAsync_ExistingMatch_ReturnsReview()
        {
            await SeedUserAsync("reviewer-1");
            await SeedUserAsync("reviewed-1");
            await SeedUserAsync("owner-1");
            var loan = await SeedLoanAsync("owner-1", "reviewer-1");
            var seeded = await SeedUserReviewAsync("reviewer-1", "reviewed-1", loanId: loan.Id);

            var result = await _repo.GetUserReviewByLoanAndReviewerAsync(loan.Id, "reviewer-1");

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
        }

        [Fact]
        public async Task GetUserReviewByLoanAndReviewerAsync_WrongReviewer_ReturnsNull()
        {
            await SeedUserAsync("reviewer-1");
            await SeedUserAsync("reviewed-1");
            await SeedUserAsync("owner-1");
            var loan = await SeedLoanAsync("owner-1", "reviewer-1");
            await SeedUserReviewAsync("reviewer-1", "reviewed-1", loanId: loan.Id);

            var result = await _repo.GetUserReviewByLoanAndReviewerAsync(loan.Id, "wrong-user");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserReviewByLoanAndReviewerAsync_WrongLoan_ReturnsNull()
        {
            await SeedUserAsync("reviewer-1");
            await SeedUserAsync("reviewed-1");
            await SeedUserAsync("owner-1");
            var loan = await SeedLoanAsync("owner-1", "reviewer-1");
            await SeedUserReviewAsync("reviewer-1", "reviewed-1", loanId: loan.Id);

            var result = await _repo.GetUserReviewByLoanAndReviewerAsync(999, "reviewer-1");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserReviewsByReviewedUserIdAsync_ReturnsOnlyReviewsForUser()
        {
            await SeedUserAsync("reviewer-1");
            await SeedUserAsync("reviewed-1");
            await SeedUserAsync("reviewed-2");
            await SeedUserReviewAsync("reviewer-1", "reviewed-1");
            await SeedUserReviewAsync("reviewer-1", "reviewed-1");
            await SeedUserReviewAsync("reviewer-1", "reviewed-2"); //different reviewed user

            var result = await _repo.GetUserReviewsByReviewedUserIdAsync("reviewed-1");

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal("reviewed-1", r.ReviewedUserId));
        }

        [Fact]
        public async Task GetUserReviewsByReviewedUserIdAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("reviewer-1");
            await SeedUserAsync("reviewed-1");
            var older = await SeedUserReviewAsync("reviewer-1", "reviewed-1", createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedUserReviewAsync("reviewer-1", "reviewed-1", createdAt: DateTime.UtcNow);

            var result = await _repo.GetUserReviewsByReviewedUserIdAsync("reviewed-1");

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetUserReviewsByReviewedUserIdAsync_NoReviews_ReturnsEmpty()
        {
            var result = await _repo.GetUserReviewsByReviewedUserIdAsync("reviewed-1");

            Assert.Empty(result);
        }

        [Fact]
        public async Task AddItemReviewAsync_SaveChangesAsync_PersistsReview()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");

            var review = new ItemReview
            {
                ItemId = loan.ItemId,
                ReviewerId = "borrower-1",
                LoanId = loan.Id,
                Rating = 5,
                Comment = "Excellent item!",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddItemReviewAsync(review);
            await _repo.SaveChangesAsync();

            var saved = await _context.ItemReviews
                .FirstOrDefaultAsync(r => r.LoanId == loan.Id);
            Assert.NotNull(saved);
            Assert.Equal(5, saved!.Rating);
            Assert.Equal("Excellent item!", saved.Comment);
        }

        [Fact]
        public async Task AddUserReviewAsync_SaveChangesAsync_PersistsReview()
        {
            await SeedUserAsync("reviewer-1");
            await SeedUserAsync("reviewed-1");

            var review = new UserReview
            {
                ReviewerId = "reviewer-1",
                ReviewedUserId = "reviewed-1",
                Rating = 3,
                Comment = "Decent borrower.",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddUserReviewAsync(review);
            await _repo.SaveChangesAsync();

            var saved = await _context.UserReviews
                .FirstOrDefaultAsync(r => r.ReviewerId == "reviewer-1");
            Assert.NotNull(saved);
            Assert.Equal(3, saved!.Rating);
            Assert.Equal("Decent borrower.", saved.Comment);
        }

        [Fact]
        public async Task LoadReviewerAsync_LoadsReviewerReference()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedItemReviewAsync(loan.ItemId, "borrower-1");

            _context.ChangeTracker.Clear();
            var freshReview = await _context.ItemReviews.FindAsync(seeded.Id);

            await _repo.LoadReviewerAsync(freshReview!);

            Assert.NotNull(freshReview!.Reviewer);
            Assert.Equal("borrower-1", freshReview.Reviewer.Id);
        }

        [Fact]
        public async Task LoadUserReviewerAsync_LoadsReviewerReference()
        {
            await SeedUserAsync("reviewer-1");
            await SeedUserAsync("reviewed-1");
            var seeded = await SeedUserReviewAsync("reviewer-1", "reviewed-1");

            _context.ChangeTracker.Clear();
            var freshReview = await _context.UserReviews.FindAsync(seeded.Id);

            await _repo.LoadUserReviewerAsync(freshReview!);

            Assert.NotNull(freshReview!.Reviewer);
            Assert.Equal("reviewer-1", freshReview.Reviewer.Id);
        }

        [Fact]
        public async Task LoadUserReviewDetailsAsync_LoadsReviewerAndLoanAndItem()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedUserReviewAsync("borrower-1", "owner-1", loanId: loan.Id);

            _context.ChangeTracker.Clear();
            var freshReview = await _context.UserReviews.FindAsync(seeded.Id);

            await _repo.LoadUserReviewDetailsAsync(freshReview!);

            Assert.NotNull(freshReview!.Reviewer);
            Assert.NotNull(freshReview.Loan);
            Assert.NotNull(freshReview.Loan!.Item);
        }

        [Fact]
        public async Task DeleteItemReview_SaveChangesAsync_RemovesReview()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var seeded = await SeedItemReviewAsync(loan.ItemId, "borrower-1");

            _repo.DeleteItemReview(seeded);
            await _repo.SaveChangesAsync();

            var deleted = await _context.ItemReviews.FindAsync(seeded.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task DeleteUserReview_SaveChangesAsync_RemovesReview()
        {
            await SeedUserAsync("reviewer-1");
            await SeedUserAsync("reviewed-1");
            var seeded = await SeedUserReviewAsync("reviewer-1", "reviewed-1");

            _repo.DeleteUserReview(seeded);
            await _repo.SaveChangesAsync();

            var deleted = await _context.UserReviews.FindAsync(seeded.Id);
            Assert.Null(deleted);
        }
    }
}