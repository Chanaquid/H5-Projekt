namespace backend.Models
{
    public class LoanMessage
    {
        public int Id { get; set; }

        //The loan this message belongs to
        //Each loan has its own message thread. 
        public int LoanId { get; set; }
        public Loan Loan { get; set; } = null!;

        //Content
        public string SenderId { get; set; } = string.Empty;
        public ApplicationUser Sender { get; set; } = null!;
        public string Content { get; set; } = string.Empty;

        //Read state
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

    }
}
