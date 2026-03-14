using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class NotificationRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly NotificationRepository _repo;

        public NotificationRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new NotificationRepository(_context);
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

        private async Task<Notification> SeedNotificationAsync(
            string userId,
            NotificationType type = NotificationType.LoanRequested,
            bool isRead = false,
            DateTime? createdAt = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Message = "Test notification",
                IsRead = isRead,
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }


        [Fact]
        public async Task GetByUserIdAsync_ReturnsOnlyUserNotifications()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedNotificationAsync("user-1");
            await SeedNotificationAsync("user-1");
            await SeedNotificationAsync("user-2"); //different user

            var result = await _repo.GetByUserIdAsync("user-1");

            Assert.Equal(2, result.Count);
            Assert.All(result, n => Assert.Equal("user-1", n.UserId));
        }

        [Fact]
        public async Task GetByUserIdAsync_OrderedByCreatedAtDescending()
        {
            await SeedUserAsync("user-1");
            var older = await SeedNotificationAsync("user-1", createdAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedNotificationAsync("user-1", createdAt: DateTime.UtcNow);

            var result = await _repo.GetByUserIdAsync("user-1");

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetByUserIdAsync_ReturnsAllTypes()
        {
            await SeedUserAsync("user-1");
            await SeedNotificationAsync("user-1", NotificationType.LoanRequested);
            await SeedNotificationAsync("user-1", NotificationType.LoanApproved);
            await SeedNotificationAsync("user-1", NotificationType.ItemApproved);

            var result = await _repo.GetByUserIdAsync("user-1");

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetByUserIdAsync_ReturnsBothReadAndUnread()
        {
            await SeedUserAsync("user-1");
            await SeedNotificationAsync("user-1", isRead: true);
            await SeedNotificationAsync("user-1", isRead: false);

            var result = await _repo.GetByUserIdAsync("user-1");

            Assert.Equal(2, result.Count);
            Assert.Contains(result, n => n.IsRead);
            Assert.Contains(result, n => !n.IsRead);
        }

        [Fact]
        public async Task GetByUserIdAsync_NoNotifications_ReturnsEmpty()
        {
            var result = await _repo.GetByUserIdAsync("user-1");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByUserIdAsync_UnrelatedUser_ReturnsEmpty()
        {
            await SeedUserAsync("user-1");
            await SeedNotificationAsync("user-1");

            var result = await _repo.GetByUserIdAsync("user-2");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsNotification()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedNotificationAsync("user-1");

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
        public async Task GetByIdAsync_ReturnsCorrectNotification()
        {
            await SeedUserAsync("user-1");
            await SeedNotificationAsync("user-1", NotificationType.LoanRequested);
            var target = await SeedNotificationAsync("user-1", NotificationType.ItemApproved);

            var result = await _repo.GetByIdAsync(target.Id);

            Assert.NotNull(result);
            Assert.Equal(NotificationType.ItemApproved, result!.Type);
        }

        [Fact]
        public async Task AddAsync_SaveChangesAsync_PersistsNotification()
        {
            await SeedUserAsync("user-1");

            var notification = new Notification
            {
                UserId = "user-1",
                Type = NotificationType.LoanApproved,
                Message = "Your loan has been approved.",
                ReferenceId = 42,
                ReferenceType = NotificationReferenceType.Loan,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(notification);
            await _repo.SaveChangesAsync();

            var saved = await _context.Notifications
                .FirstOrDefaultAsync(n => n.UserId == "user-1");
            Assert.NotNull(saved);
            Assert.Equal("Your loan has been approved.", saved!.Message);
            Assert.Equal(NotificationType.LoanApproved, saved.Type);
            Assert.Equal(42, saved.ReferenceId);
            Assert.Equal(NotificationReferenceType.Loan, saved.ReferenceType);
            Assert.False(saved.IsRead);
        }

        [Fact]
        public async Task AddAsync_SaveChangesAsync_PersistsWithoutReferenceId()
        {
            await SeedUserAsync("user-1");

            var notification = new Notification
            {
                UserId = "user-1",
                Type = NotificationType.ItemApproved,
                Message = "Your item has been approved.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
                //no ReferenceId or ReferenceType
            };

            await _repo.AddAsync(notification);
            await _repo.SaveChangesAsync();

            var saved = await _context.Notifications
                .FirstOrDefaultAsync(n => n.UserId == "user-1");
            Assert.NotNull(saved);
            Assert.Null(saved!.ReferenceId);
            Assert.Null(saved.ReferenceType);
        }

        [Fact]
        public async Task MarkAsRead_UpdatesIsRead()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedNotificationAsync("user-1", isRead: false);

            seeded.IsRead = true;
            _context.Notifications.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.Notifications.FindAsync(seeded.Id);
            Assert.True(updated!.IsRead);
        }
    }
}