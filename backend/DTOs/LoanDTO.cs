namespace backend.DTOs
{
    public class LoanDTO
    {
        //------------REQUESTS------------
        //Borrower requests a loan on an item
        public class CreateLoanDTO
        {
            public int ItemId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; } //Service validates that this is not beyond Item.AvailableUntil
        }

        //Owner or admin approves/rejects a loan request
        public class LoanDecisionDTO
        {
            public bool IsApproved { get; set; }
            public string? DecisionNote { get; set; }
        }

        //Borrower cancels their own pending/approved loan before pickup
        public class CancelLoanDTO
        {
            public string? Reason { get; set; }
        }

        //Borrower requests a loan extension
        public class RequestExtensionDTO
        {
            public DateTime RequestedExtensionDate { get; set; } // New end date requested
        }


        //Owner or admin approves/rejects an extension request
        public class ExtensionDecisionDTO
        {
            public bool IsApproved { get; set; }
        }


        //---------------RESPONSES--------------
        //Full loan detail — shown on the loan detail page
        public class LoanDetailDTO
        {
            public int Id { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime? ActualReturnDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public string SnapshotCondition { get; set; } = string.Empty;
            public string? DecisionNote { get; set; }

            // Extension info
            public DateTime? RequestedExtensionDate { get; set; }
            public string? ExtensionRequestStatus { get; set; } //"Pending", "Approved", "Rejected"

            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }

            // Related
            public ItemDTO.ItemSummaryDTO Item { get; set; } = null!;
            public UserDTO.UserSummaryDTO Owner { get; set; } = null!;
            public UserDTO.UserSummaryDTO Borrower { get; set; } = null!;
            public List<LoanSnapshotPhotoDTO> SnapshotPhotos { get; set; } = new();
            public List<FineDTO.FineResponseDTO> Fines { get; set; } = new();

            // Computed
            public bool HasOpenDispute { get; set; }
            public int? DaysOverdue { get; set; } //Null if not late
            public bool HasUnreadMessages { get; set; }
        }

        //Compact loan — used in user's loan list (as borrower or owner)
        public class LoanSummaryDTO
        {
            public int Id { get; set; }
            public string ItemTitle { get; set; } = string.Empty;
            public string? ItemPrimaryPhoto { get; set; }
            public string OtherPartyName { get; set; } = string.Empty; //Owner name if viewing as borrower, borrower name if viewing as owner
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public bool HasUnreadMessages { get; set; }
            public int? DaysOverdue { get; set; }
        }

        //Snapshot photo saved at loan creation
        public class LoanSnapshotPhotoDTO
        {
            public int Id { get; set; }
            public string PhotoUrl { get; set; } = string.Empty;
            public int DisplayOrder { get; set; }
        }

        //Admin pending loan queue — low-score users waiting for admin approval
        public class AdminPendingLoanDTO
        {
            public int Id { get; set; }
            public string ItemTitle { get; set; } = string.Empty;
            public string OwnerName { get; set; } = string.Empty;
            public string BorrowerName { get; set; } = string.Empty;
            public string BorrowerEmail { get; set; } = string.Empty;
            public int BorrowerScore { get; set; }
            public decimal BorrowerUnpaidFines { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime CreatedAt { get; set; }
        }



    }
}
