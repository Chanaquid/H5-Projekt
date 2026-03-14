namespace backend.Models
{
    public class DirectConversation
    {
        public int Id { get; set; }

        //Two participants
        public string InitiatedById { get; set; } = string.Empty;
        public ApplicationUser InitiatedBy { get; set; } = null!;

        public string OtherUserId { get; set; } = string.Empty;
        public ApplicationUser OtherUser { get; set; } = null!;


        //User convo deletion soft hide — conversation disappears from list but is not deleted
        public bool HiddenForInitiator { get; set; } = false;
        public bool HiddenForOther { get; set; } = false;

        //messages sent before this timestamp are not shown to that user - user deletes the convo
        //Set when a user deletes the conversation. Cleared when the conversation reappears.
        public DateTime? InitiatorDeletedAt { get; set; }
        public DateTime? OtherDeletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<DirectMessage> Messages { get; set; } = new List<DirectMessage>();


    }
}
