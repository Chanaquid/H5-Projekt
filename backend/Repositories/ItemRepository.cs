using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class ItemRepository : IItemRepository
    {
        private readonly AppDbContext _context;

        public ItemRepository(AppDbContext context)
        {
            _context = context;
        }

        //get all approved and active items
        public async Task<List<Item>> GetAllApprovedAsync()
        {
            return await _context.Items
                .Where(i => i.Status == ItemStatus.Approved && i.IsActive)
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Include(i => i.Loans)
                .Include(i => i.Reviews)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        //Get all items — admin only, includes deleted owners via IgnoreQueryFilters
        public async Task<List<Item>> GetAllAsync(bool includeInactive = false)
        {
            return await _context.Items
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .IgnoreQueryFilters()
                .Where(i => includeInactive || i.IsActive)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        //Get other users item as public (can only see approved items)
        public async Task<List<Item>> GetPublicByOwnerAsync(string ownerId)
        {
            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Include(i => i.Loans)
                .Include(i => i.Reviews)
                .Where(i => i.OwnerId == ownerId && i.Status == ItemStatus.Approved && i.IsActive)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        //get item by id
        public async Task<Item?> GetByIdAsync(int itemId)
        {
            return await _context.Items
                .FirstOrDefaultAsync(i => i.Id == itemId);
        }

        //Get item by id with details
        public async Task<Item?> GetByIdWithDetailsAsync(int itemId)
        {
            return await _context.Items
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Include(i => i.Loans)
                .Include(i => i.Reviews)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.Id == itemId);
        }

        //Get item by qrcode
        public async Task<Item?> GetByQrCodeAsync(string qrCode)
        {
            return await _context.Items
                .Include(i => i.Owner)
                .Include(i => i.Photos)
                .Include(i => i.Loans)
                    .ThenInclude(l => l.Borrower)
                .Include(i => i.Loans)
                    .ThenInclude(l => l.Fines)
                .Include(i => i.Loans)
                    .ThenInclude(l => l.SnapshotPhotos)
                .FirstOrDefaultAsync(i => i.QrCode == qrCode);
        }

        //Get item by owner
        public async Task<List<Item>> GetByOwnerAsync(string ownerId)
        {
            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Include(i => i.Loans)
                .Include(i => i.Reviews)
                .Where(i => i.OwnerId == ownerId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        //Get all item pending approval by created first order
        public async Task<List<Item>> GetPendingApprovalsAsync()
        {
            return await _context.Items
                .Include(i => i.Owner)
                .Include(i => i.Category)
                .Include(i => i.Photos)
                .Where(i => i.Status == ItemStatus.Pending)
                .OrderBy(i => i.CreatedAt)   //Oldest first — FIFO queue
                .ToListAsync();
        }

        public async Task<List<Item>> GetActiveItemsExpiredBeforeAsync(DateTime date)
        {
            return await _context.Items
                .Where(i => i.IsActive && i.AvailableUntil < date)
                .ToListAsync();
        }


        //Checks if qrcode already exists in db.
        public async Task<bool> QrCodeExistsAsync(string qrCode)
        {
            return await _context.Items.AnyAsync(i => i.QrCode == qrCode);
        }

        public async Task AddAsync(Item item)
        {
            await _context.Items.AddAsync(item);
        }

        public void Update(Item item)
        {
            _context.Items.Update(item);
        }

        public void Delete(Item item)
        {
            _context.Items.Remove(item);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }



    }
}
