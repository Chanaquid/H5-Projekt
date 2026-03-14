namespace backend.Models
{
    public class UserRecentlyViewedItem
    {
        //Only one record per user per item — ViewedAt gets updated on repeat visits

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow; //Updated every time the user views it
    }
}
