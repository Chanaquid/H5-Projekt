namespace backend.Models
{
    public class DirectMessage
    {
        public int Id { get; set; }

        public int ConversationId { get; set; } //which chat it belongs to
        public DirectConversation Conversation { get; set; } = null!;

        public string SenderId { get; set; } = string.Empty;
        public ApplicationUser Sender { get; set; } = null!;

        public string Content { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
