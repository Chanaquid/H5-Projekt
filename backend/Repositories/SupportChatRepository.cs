using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class SupportChatRepository : ISupportChatRepository
    {
        private readonly AppDbContext _context;

        public SupportChatRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SupportThread?> GetThreadByIdAsync(int threadId)
        {
            return await _context.SupportThreads
                .Include(t => t.User)
                .Include(t => t.ClaimedByAdmin)
                .FirstOrDefaultAsync(t => t.Id == threadId);
        }

        public async Task<SupportThread?> GetThreadWithMessagesAsync(int threadId)
        {
            return await _context.SupportThreads
                .Include(t => t.User)
                .Include(t => t.ClaimedByAdmin)
                .Include(t => t.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(t => t.Id == threadId);
        }

        public async Task<List<SupportThread>> GetThreadsByUserIdAsync(string userId)
        {
            return await _context.SupportThreads
                .Include(t => t.User)
                .Include(t => t.ClaimedByAdmin)
                .Include(t => t.Messages)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<SupportThread>> GetAllOpenThreadsAsync()
        {
            return await _context.SupportThreads
                .Include(t => t.User)
                .Include(t => t.ClaimedByAdmin)
                .Include(t => t.Messages)
                .Where(t => t.Status == SupportThreadStatus.Open || t.Status == SupportThreadStatus.Claimed)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task AddThreadAsync(SupportThread thread)
        {
            await _context.SupportThreads.AddAsync(thread);
        }

        public async Task AddMessageAsync(SupportMessage message)
        {
            await _context.SupportMessages.AddAsync(message);
        }

        public async Task MarkMessagesReadAsync(int threadId, string readByUserId)
        {
            var unread = await _context.SupportMessages
                .Where(m => m.SupportThreadId == threadId && m.SenderId != readByUserId && !m.IsRead)
                .ToListAsync();

            foreach (var m in unread)
                m.IsRead = true;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}