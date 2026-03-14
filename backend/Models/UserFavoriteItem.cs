namespace backend.Models
{
    public class UserFavoriteItem
    {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public bool NotifyWhenAvailable { get; set; } = false; // Notify user when item becomes available after a loan

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
