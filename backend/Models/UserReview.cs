namespace backend.Models
{
    public class UserReview
    {
        public int Id { get; set; }

        //The loan this review belongs to
        //Both owner and borrower can leave one review each after loan is completed
        public int? LoanId { get; set; } //Optional for admin
        public Loan? Loan { get; set; } = null!;

        //Who wrote the review??
        public string ReviewerId { get; set; } = string.Empty;
        public ApplicationUser Reviewer { get; set; } = null!;

        //Who is being reviewed??
        public string ReviewedUserId { get; set; } = string.Empty;
        public ApplicationUser ReviewedUser { get; set; } = null!;

        //If admin reviews an user
        public bool IsAdminReview { get; set; } = false;
        public bool IsEdited { get; set; } = false;
        public DateTime? EditedAt { get; set; }

        public int Rating { get; set; } //1–5 stars
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
