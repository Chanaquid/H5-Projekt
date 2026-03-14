using backend.DTOs;

namespace backend.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDTO.AdminDashboardDTO> GetDashboardAsync();
        Task<AdminDTO.ItemHistoryDTO> GetItemHistoryAsync(int itemId);
    }
}
