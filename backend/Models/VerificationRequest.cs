namespace backend.Models
{
    public class VerificationRequest
    {
        public int Id { get; set; }

        //The user requesting verification
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        //Document uploaded by the user — URL to the uploaded ID image
        //Stored securely, only accessible by admin
        public string DocumentUrl { get; set; } = string.Empty;
        public VerificationDocumentType DocumentType { get; set; } // Passport, NationalId, DrivingLicense

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        //Admin's response
        public string? AdminNote { get; set; } //Reason of rejection
        public string? ReviewedByAdminId { get; set; }
        public ApplicationUser? ReviewedByAdmin { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }
}
