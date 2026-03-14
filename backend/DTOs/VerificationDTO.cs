namespace backend.DTOs
{
    public class VerificationDTO
    {
        //---------------REQUESTS--------------------
        //User submits a verification request with their government ID
        public class CreateVerificationRequestDTO
        {
            public string DocumentUrl { get; set; } = string.Empty;    //URL to uploaded ID image
            public string DocumentType { get; set; } = string.Empty;  //"Passport", "NationalId", "DrivingLicense"
        }

        //Admin approves or rejects a verification request
        public class AdminVerificationDecisionDTO
        {
            public bool IsApproved { get; set; }
            public string? AdminNote { get; set; } //explains why rejected
        }

        //---------------RESPONSES--------------------

        public class VerificationRequestResponseDTO
        {
            public int Id { get; set; }
            public string UserId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string UserEmail { get; set; } = string.Empty;
            public string DocumentUrl { get; set; } = string.Empty;
            public string DocumentType { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty; //"Pending", "Approved", "Rejected"
            public string? AdminNote { get; set; }
            public string? ReviewedByAdminName { get; set; }
            public DateTime SubmittedAt { get; set; }
            public DateTime? ReviewedAt { get; set; }
        }


    }
}
