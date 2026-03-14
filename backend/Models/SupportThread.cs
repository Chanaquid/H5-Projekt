namespace backend.Models
{
    //Chat with admin
    public class SupportThread
    {
        public int Id { get; set; }

        //The user who opened this support thread
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        //The admin who claimed this thread — null if unclaimed
        public string? ClaimedByAdminId { get; set; }
        public ApplicationUser? ClaimedByAdmin { get; set; }

        public SupportThreadStatus Status { get; set; } = SupportThreadStatus.Open;

        public DateTime? ClaimedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
    }
}
