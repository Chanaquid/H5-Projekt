namespace backend.Models
{
    public class UserBlock
    {
        //Composite Priamarykey — one block record per pair
        public string BlockerId { get; set; } = string.Empty;
        public ApplicationUser Blocker { get; set; } = null!;

        public string BlockedId { get; set; } = string.Empty;
        public ApplicationUser Blocked { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
