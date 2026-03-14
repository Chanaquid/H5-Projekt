using backend.Models;

namespace backend.Interfaces
{
    public interface IItemRepository
    {
        //Queries
        Task<List<Item>> GetAllApprovedAsync();
        Task<List<Item>> GetAllAsync(bool includeInactive = false);  //Admin — pass true to see inactive
        Task<Item?> GetByIdAsync(int itemId);
        Task<List<Item>> GetPublicByOwnerAsync(string ownerId);

        Task<Item?> GetByIdWithDetailsAsync(int itemId); //Includes Owner, Category, Photos, Reviews, Loans
        Task<Item?> GetByQrCodeAsync(string qrCode);
        Task<List<Item>> GetByOwnerAsync(string ownerId);
        Task<List<Item>> GetPendingApprovalsAsync();
        Task<bool> QrCodeExistsAsync(string qrCode);

        //CRUD
        Task AddAsync(Item item);
        void Update(Item item);
        void Delete(Item item);
        Task SaveChangesAsync();
    }
}
