using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class DirectMessageRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly DirectMessageRepository _repo;

        public DirectMessageRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new DirectMessageRepository(_context);
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

        private async Task<DirectConversation> SeedConversationAsync(
            string initiatedById,
            string otherUserId,
            bool hiddenForInitiator = false,
            bool hiddenForOther = false)
        {
            var conversation = new DirectConversation
            {
                InitiatedById = initiatedById,
                OtherUserId = otherUserId,
                HiddenForInitiator = hiddenForInitiator,
                HiddenForOther = hiddenForOther,
                CreatedAt = DateTime.UtcNow
            };
            _context.DirectConversations.Add(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        private async Task<DirectMessage> SeedMessageAsync(
            int conversationId,
            string senderId,
            string content = "Hello",
            DateTime? sentAt = null)
        {
            var message = new DirectMessage
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                SentAt = sentAt ?? DateTime.UtcNow
            };
            _context.DirectMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }


        [Fact]
        public async Task GetConversationAsync_InitiatorToOther_ReturnsConversation()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedConversationAsync("user-1", "user-2");

            var result = await _repo.GetConversationAsync("user-1", "user-2");

            Assert.NotNull(result);
            Assert.Equal("user-1", result!.InitiatedById);
            Assert.Equal("user-2", result.OtherUserId);
        }

        [Fact]
        public async Task GetConversationAsync_ReverseOrder_ReturnsConversation()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedConversationAsync("user-1", "user-2");

            //Asking as user-2 → user-1 should still find the same conversation
            var result = await _repo.GetConversationAsync("user-2", "user-1"); 

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetConversationAsync_NoConversation_ReturnsNull()
        {
            var result = await _repo.GetConversationAsync("user-1", "user-2");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetConversationAsync_IncludesBothUsers()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedConversationAsync("user-1", "user-2");

            var result = await _repo.GetConversationAsync("user-1", "user-2");

            Assert.NotNull(result!.InitiatedBy);
            Assert.NotNull(result.OtherUser);
        }


        [Fact]
        public async Task GetConversationByIdAsync_ExistingId_ReturnsConversation()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var seeded = await SeedConversationAsync("user-1", "user-2");

            var result = await _repo.GetConversationByIdAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
        }

        [Fact]
        public async Task GetConversationByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetConversationByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetConversationByIdAsync_IncludesBothUsers()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var seeded = await SeedConversationAsync("user-1", "user-2");

            var result = await _repo.GetConversationByIdAsync(seeded.Id);

            Assert.NotNull(result!.InitiatedBy);
            Assert.NotNull(result.OtherUser);
        }

        [Fact]
        public async Task GetConversationsByUserIdAsync_ReturnsConversationsForUser()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedUserAsync("user-3");
            await SeedConversationAsync("user-1", "user-2");
            await SeedConversationAsync("user-1", "user-3");

            var result = await _repo.GetConversationsByUserIdAsync("user-1");

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetConversationsByUserIdAsync_ExcludesHiddenForInitiator()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedConversationAsync("user-1", "user-2", hiddenForInitiator: true);

            var result = await _repo.GetConversationsByUserIdAsync("user-1");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetConversationsByUserIdAsync_ExcludesHiddenForOther()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedConversationAsync("user-1", "user-2", hiddenForOther: true);

            //user-2 is the "other" — should not see this conversation
            var result = await _repo.GetConversationsByUserIdAsync("user-2");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetConversationsByUserIdAsync_HiddenForOtherButNotInitiator_InitiatorStillSees()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedConversationAsync("user-1", "user-2", hiddenForOther: true);

            var result = await _repo.GetConversationsByUserIdAsync("user-1");

            Assert.Single(result);
        }

        [Fact]
        public async Task GetConversationsByUserIdAsync_NoConversations_ReturnsEmpty()
        {
            var result = await _repo.GetConversationsByUserIdAsync("user-1");

            Assert.Empty(result);
        }


        [Fact]
        public async Task IsParticipantAsync_InitiatorUser_ReturnsTrue()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var seeded = await SeedConversationAsync("user-1", "user-2");

            var result = await _repo.IsParticipantAsync(seeded.Id, "user-1");

            Assert.True(result);
        }

        [Fact]
        public async Task IsParticipantAsync_OtherUser_ReturnsTrue()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var seeded = await SeedConversationAsync("user-1", "user-2");

            var result = await _repo.IsParticipantAsync(seeded.Id, "user-2");

            Assert.True(result);
        }

        [Fact]
        public async Task IsParticipantAsync_NonParticipant_ReturnsFalse()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var seeded = await SeedConversationAsync("user-1", "user-2");

            var result = await _repo.IsParticipantAsync(seeded.Id, "user-3");

            Assert.False(result);
        }

        [Fact]
        public async Task IsParticipantAsync_NonExistingConversation_ReturnsFalse()
        {
            var result = await _repo.IsParticipantAsync(999, "user-1");

            Assert.False(result);
        }

        [Fact]
        public async Task GetMessagesAsync_ReturnsAllMessages_WhenNoAfterFilter()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var conv = await SeedConversationAsync("user-1", "user-2");
            await SeedMessageAsync(conv.Id, "user-1", "Hello");
            await SeedMessageAsync(conv.Id, "user-2", "Hi");

            var result = await _repo.GetMessagesAsync(conv.Id, null);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetMessagesAsync_WithAfterFilter_ReturnsOnlyNewerMessages()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var conv = await SeedConversationAsync("user-1", "user-2");
            var cutoff = DateTime.UtcNow;
            await SeedMessageAsync(conv.Id, "user-1", "Old message", cutoff.AddMinutes(-10));
            await SeedMessageAsync(conv.Id, "user-2", "New message", cutoff.AddMinutes(10));

            var result = await _repo.GetMessagesAsync(conv.Id, cutoff);

            Assert.Single(result);
            Assert.Equal("New message", result[0].Content);
        }

        [Fact]
        public async Task GetMessagesAsync_OrderedBySentAtAscending()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var conv = await SeedConversationAsync("user-1", "user-2");
            var now = DateTime.UtcNow;
            await SeedMessageAsync(conv.Id, "user-1", "Second", now.AddMinutes(5));
            await SeedMessageAsync(conv.Id, "user-2", "First", now.AddMinutes(-5));

            var result = await _repo.GetMessagesAsync(conv.Id, null);

            Assert.Equal("First", result[0].Content);
            Assert.Equal("Second", result[1].Content);
        }

        [Fact]
        public async Task GetMessagesAsync_NoMessages_ReturnsEmpty()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var conv = await SeedConversationAsync("user-1", "user-2");

            var result = await _repo.GetMessagesAsync(conv.Id, null);

            Assert.Empty(result);
        }

 
        [Fact]
        public async Task AddConversationAsync_SaveChangesAsync_PersistsConversation()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");

            var conversation = new DirectConversation
            {
                InitiatedById = "user-1",
                OtherUserId = "user-2",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddConversationAsync(conversation);
            await _repo.SaveChangesAsync();

            var saved = await _context.DirectConversations
                .FirstOrDefaultAsync(c => c.InitiatedById == "user-1" && c.OtherUserId == "user-2");
            Assert.NotNull(saved);
        }

        [Fact]
        public async Task AddMessageAsync_SaveChangesAsync_PersistsMessage()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var conv = await SeedConversationAsync("user-1", "user-2");

            var message = new DirectMessage
            {
                ConversationId = conv.Id,
                SenderId = "user-1",
                Content = "Hey there!",
                SentAt = DateTime.UtcNow
            };

            await _repo.AddMessageAsync(message);
            await _repo.SaveChangesAsync();

            var saved = await _context.DirectMessages
                .FirstOrDefaultAsync(m => m.ConversationId == conv.Id);
            Assert.NotNull(saved);
            Assert.Equal("Hey there!", saved!.Content);
        }


        [Fact]
        public async Task LoadMessageSenderAsync_LoadsSenderReference()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var conv = await SeedConversationAsync("user-1", "user-2");
            var message = await SeedMessageAsync(conv.Id, "user-1", "Hello");

            //Detach so sender is not already loaded
            _context.ChangeTracker.Clear();
            var freshMessage = await _context.DirectMessages.FindAsync(message.Id);

            await _repo.LoadMessageSenderAsync(freshMessage!);

            Assert.NotNull(freshMessage!.Sender);
            Assert.Equal("user-1", freshMessage.Sender.Id);
        }
    }
}