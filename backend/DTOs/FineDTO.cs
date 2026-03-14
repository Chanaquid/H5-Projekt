using backend.Models;

namespace backend.DTOs
{
    public class FineDTO
    {
        //--------------REQUESTS---------------
        //User marks a fine as paid and provides their payment reference
        public class PayFineDTO
        {
            public int FineId { get; set; }
            public string PaymentProofImageUrl { get; set; } = string.Empty;
            public string PaymentDescription { get; set; } = string.Empty;
        }

        //Admin issue custom fine 
        public class AdminIssueFineDTO
        {
            public string UserId { get; set; } = string.Empty;
            public int? LoanId { get; set; }
            public decimal Amount { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        //Admin update fine
        public class AdminUpdateFineDTO
        {
            public decimal? Amount { get; set; }
            public string? Reason { get; set; }
            public FineStatus? Status { get; set; }

        }

        //Admin confirms a fine has been paid (manual override)
        public class AdminFineVerificationDTO
        {
            public int FineId { get; set; }
            public bool IsApproved { get; set; }
            public string? RejectionReason { get; set; } //Required if rejected
        }

        //--------------RESPONSES---------------
        public class FineResponseDTO
        {
            public int Id { get; set; }
            public int? LoanId { get; set; }
            public string ItemTitle { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;   //"Late", "Damaged", "Lost"
            public string Status { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public decimal ItemValueAtTimeOfFine { get; set; }
            public string? PaymentProofImageUrl { get; set; }
            public string? PaymentDescription { get; set; }
            public string? RejectionReason { get; set; }
            public DateTime? PaidAt { get; set; }
            public DateTime? VerifiedAt { get; set; }
            public int? DisputeId { get; set; }  //Set if fine was created via a dispute verdict
            public DateTime CreatedAt { get; set; }
        }


    }
}
