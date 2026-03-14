namespace backend.Models
{
    public class SupportMessage
    {
        //Basically direct message but with Admin
        public int Id { get; set; }

        public int SupportThreadId { get; set; } 
        public SupportThread SupportThread { get; set; } = null!;

        public string SenderId { get; set; } = string.Empty;
        public ApplicationUser Sender { get; set; } = null!;

        public string Content { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
