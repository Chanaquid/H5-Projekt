using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class LoanMessageRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly LoanMessageRepository _repo;

        public LoanMessageRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new LoanMessageRepository(_context);
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

        private async Task<LoanMessage> SeedMessageAsync(
            int loanId,
            string senderId,
            string content = "Hello",
            DateTime? sentAt = null)
        {
            var message = new LoanMessage
            {
                LoanId = loanId,
                SenderId = senderId,
                Content = content,
                IsRead = false,
                SentAt = sentAt ?? DateTime.UtcNow
            };
            _context.LoanMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        [Fact]
        public async Task GetByLoanIdAsync_ReturnsAllMessagesForLoan()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedMessageAsync(loan.Id, "owner-1", "Hello");
            await SeedMessageAsync(loan.Id, "borrower-1", "Hi there");

            var result = await _repo.GetByLoanIdAsync(loan.Id);

            Assert.Equal(2, result.Count);
            Assert.All(result, m => Assert.Equal(loan.Id, m.LoanId));
        }

        [Fact]
        public async Task GetByLoanIdAsync_DoesNotReturnMessagesFromOtherLoans()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan1 = await SeedLoanAsync("owner-1", "borrower-1");
            var loan2 = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedMessageAsync(loan1.Id, "owner-1", "Loan 1 message");
            await SeedMessageAsync(loan2.Id, "owner-1", "Loan 2 message");

            var result = await _repo.GetByLoanIdAsync(loan1.Id);

            Assert.Single(result);
            Assert.Equal("Loan 1 message", result[0].Content);
        }

        [Fact]
        public async Task GetByLoanIdAsync_OrderedBySentAtAscending()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var now = DateTime.UtcNow;
            await SeedMessageAsync(loan.Id, "owner-1", "Second", now.AddMinutes(5));
            await SeedMessageAsync(loan.Id, "borrower-1", "First", now.AddMinutes(-5));

            var result = await _repo.GetByLoanIdAsync(loan.Id);

            Assert.Equal("First", result[0].Content);
            Assert.Equal("Second", result[1].Content);
        }

        [Fact]
        public async Task GetByLoanIdAsync_IncludesSender()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedMessageAsync(loan.Id, "owner-1");

            var result = await _repo.GetByLoanIdAsync(loan.Id);

            Assert.Single(result);
            Assert.NotNull(result[0].Sender);
            Assert.Equal("owner-1", result[0].Sender.Id);
        }

        [Fact]
        public async Task GetByLoanIdAsync_NoMessages_ReturnsEmpty()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");

            var result = await _repo.GetByLoanIdAsync(loan.Id);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByUserIdAsync_ReturnsOnlyMessagesFromUser()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedMessageAsync(loan.Id, "owner-1", "Owner message");
            await SeedMessageAsync(loan.Id, "owner-1", "Owner message 2");
            await SeedMessageAsync(loan.Id, "borrower-1", "Borrower message");

            var result = await _repo.GetByUserIdAsync("owner-1");

            Assert.Equal(2, result.Count);
            Assert.All(result, m => Assert.Equal("owner-1", m.SenderId));
        }

        [Fact]
        public async Task GetByUserIdAsync_OrderedBySentAtDescending()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var now = DateTime.UtcNow;
            var older = await SeedMessageAsync(loan.Id, "owner-1", "Older", now.AddMinutes(-10));
            var newer = await SeedMessageAsync(loan.Id, "owner-1", "Newer", now);

            var result = await _repo.GetByUserIdAsync("owner-1");

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetByUserIdAsync_IncludesSender()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            await SeedMessageAsync(loan.Id, "owner-1");

            var result = await _repo.GetByUserIdAsync("owner-1");

            Assert.Single(result);
            Assert.NotNull(result[0].Sender);
            Assert.Equal("owner-1", result[0].Sender.Id);
        }

        [Fact]
        public async Task GetByUserIdAsync_NoMessages_ReturnsEmpty()
        {
            var result = await _repo.GetByUserIdAsync("owner-1");

            Assert.Empty(result);
        }

        [Fact]
        public async Task AddAsync_SaveChangesAsync_PersistsMessage()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");

            var message = new LoanMessage
            {
                LoanId = loan.Id,
                SenderId = "owner-1",
                Content = "Is the item ready for pickup?",
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            await _repo.AddAsync(message);
            await _repo.SaveChangesAsync();

            var saved = await _context.LoanMessages
                .FirstOrDefaultAsync(m => m.LoanId == loan.Id);
            Assert.NotNull(saved);
            Assert.Equal("Is the item ready for pickup?", saved!.Content);
            Assert.Equal("owner-1", saved.SenderId);
            Assert.False(saved.IsRead);
        }


        [Fact]
        public async Task LoadSenderAsync_LoadsSenderReference()
        {
            await SeedUserAsync("owner-1");
            await SeedUserAsync("borrower-1");
            var loan = await SeedLoanAsync("owner-1", "borrower-1");
            var message = await SeedMessageAsync(loan.Id, "owner-1", "Hello");

            // Clear tracker so sender is not already loaded
            _context.ChangeTracker.Clear();
            var freshMessage = await _context.LoanMessages.FindAsync(message.Id);

            await _repo.LoadSenderAsync(freshMessage!);

            Assert.NotNull(freshMessage!.Sender);
            Assert.Equal("owner-1", freshMessage.Sender.Id);
        }
    }
}