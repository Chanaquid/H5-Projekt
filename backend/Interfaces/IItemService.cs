using backend.DTOs;

namespace backend.Interfaces
{
    public interface IItemService
    {
        //Browse
        Task<List<ItemDTO.ItemSummaryDTO>> GetAllApprovedAsync();
        Task<ItemDTO.ItemDetailDTO> GetByIdAsync(int itemId, string? requestingUserId, bool isAdmin = false);
        Task<List<ItemDTO.ItemSummaryDTO>> GetPublicByOwnerAsync(string ownerId);
        Task<List<ItemDTO.ItemSummaryDTO>> GetNearbyAsync(double lat, double lng, double radiusKm);


        //Owner actions
        Task<ItemDTO.ItemDetailDTO> CreateAsync(string ownerId, ItemDTO.CreateItemDTO dto, bool isAdmin = false);
        Task<ItemDTO.ItemDetailDTO> UpdateAsync(int itemId, string ownerId, ItemDTO.UpdateItemDTO dto, bool isAdmin = false);
        Task DeleteAsync(int itemId, string ownerId, bool isAdmin = false);
        Task<List<ItemDTO.ItemSummaryDTO>> GetByOwnerAsync(string ownerId);

        //QR code
        //Only for owner or admin — enforced in controller
        Task<ItemDTO.ItemQrCodeDTO> GetQrCodeAsync(int itemId, string requestingUserId, bool isAdmin = false);
        Task<ItemDTO.ItemDetailDTO> ScanQrCodeAsync(string qrCode, string scannedByUserId, bool isAdmin = false);

        //Admin actions
        Task<List<ItemDTO.ItemSummaryDTO>> GetAllForAdminAsync(bool includeInactive = false);
        Task<ItemDTO.ItemDetailDTO> UpdateStatusAsync(int itemId, ItemDTO.AdminItemStatusDTO dto);
        Task<List<ItemDTO.AdminPendingItemDTO>> GetPendingApprovalsAsync();
        Task<ItemDTO.ItemDetailDTO> AdminDecideAsync(int itemId, string adminId, ItemDTO.AdminItemDecisionDTO dto);
    }
}
