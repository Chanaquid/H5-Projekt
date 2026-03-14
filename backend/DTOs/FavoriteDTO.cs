using backend.DTOs;

namespace backend.DTOs
{
    public class FavoriteDTO
    {
        //Response — returned when listing favorites
        public class FavoriteResponseDTO
        {
            public ItemDTO.ItemSummaryDTO Item { get; set; } = null!;
            public bool NotifyWhenAvailable { get; set; }
            public DateTime SavedAt { get; set; }
        }

        //Request — PATCH /api/favorites/{itemId}/notify
        public class ToggleNotifyDTO
        {
            public bool NotifyWhenAvailable { get; set; }
        }
    }
}