using backend.DTOs;

namespace backend.DTOs
{
    public class RecentlyViewedDTO
    {
        public class RecentlyViewedResponseDTO
        {
            public ItemDTO.ItemSummaryDTO Item { get; set; } = null!;
            public DateTime ViewedAt { get; set; }
        }
    }
}