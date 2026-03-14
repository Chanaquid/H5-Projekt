using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class SupportChatRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly SupportChatRepository _repo;

        public SupportChatRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new SupportChatRepository(_context);
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

        private async Task<SupportThread> SeedThreadAsync(
            string userId,
            SupportThreadStatus status = SupportThreadStatus.Open,
            string? claimedByAdminId = null,
            DateTime? createdAt = null)
        {
            var thread = new SupportThread
            {
                UserId = userId,
                ClaimedByAdminId = claimedByAdminId,
                Status = status,
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
            _context.SupportThreads.Add(thread);
            await _context.SaveChangesAsync();
            return thread;
        }

        private async Task<SupportMessage> SeedMessageAsync(
            int threadId,
            string senderId,
            string content = "Hello",
            bool isRead = false,
            DateTime? sentAt = null)
        {
            var message = new SupportMessage
            {
                SupportThreadId = threadId,
                SenderId = senderId,
                Content = content,
                IsRead = isRead,
                SentAt = sentAt ?? DateTime.UtcNow
            };
            _context.SupportMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

 
        [Fact]
        public async Task GetThreadByIdAsync_ExistingId_ReturnsThread()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedThreadAsync("user-1");

            var result = await _repo.GetThreadByIdAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
        }

        [Fact]
        public async Task GetThreadByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetThreadByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetThreadByIdAsync_IncludesUser()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedThreadAsync("user-1");

            _context.ChangeTracker.Clear();

            var result = await _repo.GetThreadByIdAsync(seeded.Id);

            Assert.NotNull(result!.User);
            Assert.Equal("user-1", result.User.Id);
        }

        [Fact]
        public async Task GetThreadByIdAsync_IncludesClaimedByAdmin()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("admin-1");
            var seeded = await SeedThreadAsync("user-1",
                SupportThreadStatus.Claimed, claimedByAdminId: "admin-1");

            _context.ChangeTracker.Clear();

            var result = await _repo.GetThreadByIdAsync(seeded.Id);

            Assert.NotNull(result!.ClaimedByAdmin);
            Assert.Equal("admin-1", result.ClaimedByAdmin!.Id);
        }

        [Fact]
        public async Task GetThreadByIdAsync_NoAdmin_ClaimedByAdminIsNull()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedThreadAsync("user-1");

            var result = await _repo.GetThreadByIdAsync(seeded.Id);

            Assert.Null(result!.ClaimedByAdminId);
        }


        [Fact]
        public async Task GetThreadWithMessagesAsync_ExistingId_ReturnsThread()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedThreadAsync("user-1");

            var result = await _repo.GetThreadWithMessagesAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
        }

        [Fact]
        public async Task GetThreadWithMessagesAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetThreadWithMessagesAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetThreadWithMessagesAsync_IncludesMessages()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedThreadAsync("user-1");
            await SeedMessageAsync(seeded.Id, "user-1", "Hello");
            await SeedMessageAsync(seeded.Id, "user-1", "Any updates?");

            _context.ChangeTracker.Clear();

            var result = await _repo.GetThreadWithMessagesAsync(seeded.Id);

            Assert.Equal(2, result!.Messages.Count);
        }

        [Fact]
        public async Task GetThreadWithMessagesAsync_IncludesMessageSender()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedThreadAsync("user-1");
            await SeedMessageAsync(seeded.Id, "user-1", "Hello");

            _context.ChangeTracker.Clear();

            var result = await _repo.GetThreadWithMessagesAsync(seeded.Id);

            var message = result!.Messages.First();
            Assert.NotNull(message.Sender);
            Assert.Equal("user-1", message.Sender.Id);
        }

        [Fact]
        public async Task GetThreadWithMessagesAsync_NoMessages_ReturnsEmptyCollection()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedThreadAsync("user-1");

            var result = await _repo.GetThreadWithMessagesAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.Empty(result!.Messages);
        }


        [Fact]
        public async Task GetThreadsByUserIdAsync_ReturnsOnlyUserThreads()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedThreadAsync("user-1");
            await SeedThreadAsync("user-1");
            await SeedThreadAsync("user-2"); //different user

            var result = await _repo.GetThreadsByUserIdAsync("user-1");

            Assert.Equal(2, result.Count);
            Assert.All(result, t => Assert.Equal("user-1", t.UserId));
        }

        [Fact]
        public async Task GetThreadsByUserIdAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("user-1");
            var older = await SeedThreadAsync("user-1", createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedThreadAsync("user-1", createdAt: DateTime.UtcNow);

            var result = await _repo.GetThreadsByUserIdAsync("user-1");

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetThreadsByUserIdAsync_NoThreads_ReturnsEmpty()
        {
            var result = await _repo.GetThreadsByUserIdAsync("user-1");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetThreadsByUserIdAsync_ReturnsAllStatuses()
        {
            await SeedUserAsync("user-1");
            await SeedThreadAsync("user-1", SupportThreadStatus.Open);
            await SeedThreadAsync("user-1", SupportThreadStatus.Claimed);
            await SeedThreadAsync("user-1", SupportThreadStatus.Closed);

            var result = await _repo.GetThreadsByUserIdAsync("user-1");

            Assert.Equal(3, result.Count);
        }

  
        [Fact]
        public async Task GetAllOpenThreadsAsync_ReturnsOpenAndClaimedOnly()
        {
            await SeedUserAsync("user-1");
            await SeedThreadAsync("user-1", SupportThreadStatus.Open);
            await SeedThreadAsync("user-1", SupportThreadStatus.Claimed);
            await SeedThreadAsync("user-1", SupportThreadStatus.Closed); //excluded

            var result = await _repo.GetAllOpenThreadsAsync();

            Assert.Equal(2, result.Count);
            Assert.All(result, t => Assert.NotEqual(SupportThreadStatus.Closed, t.Status));
        }

        [Fact]
        public async Task GetAllOpenThreadsAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("user-1");
            var older = await SeedThreadAsync("user-1", createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedThreadAsync("user-1", createdAt: DateTime.UtcNow);

            var result = await _repo.GetAllOpenThreadsAsync();

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetAllOpenThreadsAsync_NoOpenThreads_ReturnsEmpty()
        {
            await SeedUserAsync("user-1");
            await SeedThreadAsync("user-1", SupportThreadStatus.Closed);

            var result = await _repo.GetAllOpenThreadsAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task AddThreadAsync_SaveChangesAsync_PersistsThread()
        {
            await SeedUserAsync("user-1");

            var thread = new SupportThread
            {
                UserId = "user-1",
                Status = SupportThreadStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddThreadAsync(thread);
            await _repo.SaveChangesAsync();

            var saved = await _context.SupportThreads
                .FirstOrDefaultAsync(t => t.UserId == "user-1");
            Assert.NotNull(saved);
            Assert.Equal(SupportThreadStatus.Open, saved!.Status);
        }


        [Fact]
        public async Task AddMessageAsync_SaveChangesAsync_PersistsMessage()
        {
            await SeedUserAsync("user-1");
            var thread = await SeedThreadAsync("user-1");

            var message = new SupportMessage
            {
                SupportThreadId = thread.Id,
                SenderId = "user-1",
                Content = "I need help with my account.",
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            await _repo.AddMessageAsync(message);
            await _repo.SaveChangesAsync();

            var saved = await _context.SupportMessages
                .FirstOrDefaultAsync(m => m.SupportThreadId == thread.Id);
            Assert.NotNull(saved);
            Assert.Equal("I need help with my account.", saved!.Content);
            Assert.Equal("user-1", saved.SenderId);
            Assert.False(saved.IsRead);
        }

        [Fact]
        public async Task MarkMessagesReadAsync_MarksUnreadMessagesFromOtherSender()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("admin-1");
            var thread = await SeedThreadAsync("user-1");

            //admin sent messages to user — user reads them
            await SeedMessageAsync(thread.Id, "admin-1", "We are looking into it.", isRead: false);
            await SeedMessageAsync(thread.Id, "admin-1", "Please wait.", isRead: false);
            //user's own message — should NOT be marked read
            await SeedMessageAsync(thread.Id, "user-1", "Hello!", isRead: false);

            await _repo.MarkMessagesReadAsync(thread.Id, "user-1");
            await _repo.SaveChangesAsync();

            var messages = await _context.SupportMessages
                .Where(m => m.SupportThreadId == thread.Id)
                .ToListAsync();

            //admin messages marked as read
            Assert.True(messages.Where(m => m.SenderId == "admin-1").All(m => m.IsRead));
            //user's own message stays unread
            Assert.False(messages.Single(m => m.SenderId == "user-1").IsRead);
        }

        [Fact]
        public async Task MarkMessagesReadAsync_AlreadyReadMessages_StayRead()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("admin-1");
            var thread = await SeedThreadAsync("user-1");
            await SeedMessageAsync(thread.Id, "admin-1", "Already read.", isRead: true);

            await _repo.MarkMessagesReadAsync(thread.Id, "user-1");
            await _repo.SaveChangesAsync();

            var message = await _context.SupportMessages
                .FirstAsync(m => m.SupportThreadId == thread.Id);
            Assert.True(message.IsRead);
        }

        [Fact]
        public async Task MarkMessagesReadAsync_NoUnreadMessages_NoChanges()
        {
            await SeedUserAsync("user-1");
            var thread = await SeedThreadAsync("user-1");
            //no messages at all

            await _repo.MarkMessagesReadAsync(thread.Id, "user-1");
            await _repo.SaveChangesAsync();

            var count = await _context.SupportMessages
                .CountAsync(m => m.SupportThreadId == thread.Id);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task MarkMessagesReadAsync_OnlyMarksCorrectThread()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("admin-1");
            var thread1 = await SeedThreadAsync("user-1");
            var thread2 = await SeedThreadAsync("user-1");

            await SeedMessageAsync(thread1.Id, "admin-1", "Thread 1 message", isRead: false);
            await SeedMessageAsync(thread2.Id, "admin-1", "Thread 2 message", isRead: false);

            // only mark thread1 as read
            await _repo.MarkMessagesReadAsync(thread1.Id, "user-1");
            await _repo.SaveChangesAsync();

            var thread1Message = await _context.SupportMessages
                .FirstAsync(m => m.SupportThreadId == thread1.Id);
            var thread2Message = await _context.SupportMessages
                .FirstAsync(m => m.SupportThreadId == thread2.Id);

            Assert.True(thread1Message.IsRead);
            Assert.False(thread2Message.IsRead); //untouched
        }
    }
}