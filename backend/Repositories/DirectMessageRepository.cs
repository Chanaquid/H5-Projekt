using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class DirectMessageRepository : IDirectMessageRepository
    {
        private readonly AppDbContext _context;

        public DirectMessageRepository(AppDbContext context)
        {
            _context = context;
        }

        //Find existing conversation between two users (doesnt matter who initiated)

        public async Task<DirectConversation?> GetConversationAsync(string userAId, string userBId)
        {
            return await _context.DirectConversations
                .Include(c => c.InitiatedBy)
                .Include(c => c.OtherUser)
                .FirstOrDefaultAsync(c =>
                    (c.InitiatedById == userAId && c.OtherUserId == userBId) ||
                    (c.InitiatedById == userBId && c.OtherUserId == userAId));
        }


        //Get conversation by id with both user details
        public async Task<DirectConversation?> GetConversationByIdAsync(int conversationId)
        {
            return await _context.DirectConversations
                .Include(c => c.InitiatedBy)
                .Include(c => c.OtherUser)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }


        //Get all conversations for inbox — excludes hidden ones for this user
        public async Task<List<DirectConversation>> GetConversationsByUserIdAsync(string userId)
        {
            return await _context.DirectConversations
                .Include(c => c.InitiatedBy)
                .Include(c => c.OtherUser)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Where(c =>
                    (c.InitiatedById == userId && !c.HiddenForInitiator) ||
                    (c.OtherUserId == userId && !c.HiddenForOther))
                .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.SentAt) ?? c.CreatedAt)
                .ToListAsync();
        }


        //Check if user is a participant — for ChatHub validation
        public async Task<bool> IsParticipantAsync(int conversationId, string userId)
        {
            return await _context.DirectConversations
                .AnyAsync(c => c.Id == conversationId &&
                               (c.InitiatedById == userId || c.OtherUserId == userId));
        }


        //Get messages after a given cutoff timestamp (handles per-user hide/delete history)
        public async Task<List<DirectMessage>> GetMessagesAsync(int conversationId, DateTime? after)
        {
            var query = _context.DirectMessages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId);

            if (after.HasValue)
                query = query.Where(m => m.SentAt > after.Value);

            return await query.OrderBy(m => m.SentAt).ToListAsync();
        }


        public async Task AddConversationAsync(DirectConversation conversation)
        {
            await _context.DirectConversations.AddAsync(conversation);
        }

        public async Task AddMessageAsync(DirectMessage message)
        {
            await _context.DirectMessages.AddAsync(message);
        }


        //Load only sender reference after insert — avoids reloading full thread
        public async Task LoadMessageSenderAsync(DirectMessage message)
        {
            await _context.Entry(message)
                .Reference(m => m.Sender)
                .LoadAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

    }
}
