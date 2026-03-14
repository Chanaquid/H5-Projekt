
namespace backend.Models
{
    public class ScoreHistory
    {
        public int Id { get; set; }

        //User
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public int PointsChanged { get; set; } //Total score change (can be both positive or negative)

        public int ScoreAfterChange { get; set; } //Score after the change is applied

        public ScoreChangeReason Reason { get; set; }


        //Loan details that triggered this score change
        public int? LoanId { get; set; }
        public Loan? Loan { get; set; }

        public string? Note { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
