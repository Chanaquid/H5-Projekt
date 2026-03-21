namespace backend.DTOs
{
    public class AdminDTO
    {
        //---------------RESPONSES--------------------
        //Returned when admin opens the dashboard — all queue counts in one call
        public class AdminDashboardDTO
        {
            //Action queues
            public int PendingItemApprovals { get; set; }
            public int PendingLoanApprovals { get; set; }  //Low-score users waiting for admin approval
            public int OpenDisputes { get; set; }
            public int PendingAppeals { get; set; }
            public int PendingUserVerifications { get; set; } //New users waiting to be verified
            public int PendingPaymentVerifications { get; set; }

            //Platform stats
            public int TotalUsers { get; set; }
            public int TotalActiveItems { get; set; }
            public int TotalActiveLoans { get; set; }
            public int TotalUnpaidFines { get; set; }
            public decimal TotalUnpaidFinesAmount { get; set; }
        }

        //Admin looks up an item's full audit trail by item ID
        public class ItemHistoryDTO
        {
            public int ItemId { get; set; }
            public string ItemTitle { get; set; } = string.Empty;
            public string OwnerName { get; set; } = string.Empty;
            public double AverageRating { get; set; }
            public int ReviewCount { get; set; }

            public List<ItemReviewEntryDTO> Reviews { get; set; } = new();

            public List<LoanHistoryEntryDTO> Loans { get; set; } = new();
        }

        public class ItemReviewEntryDTO
        {
            public int Id { get; set; }
            public string ReviewerId { get; set; } = string.Empty;
            public string? ReviewerAvatarUrl { get; set; }

            public string ReviewerName { get; set; } = string.Empty;
            public int Rating { get; set; }
            public string? Comment { get; set; }
            public bool IsAdminReview { get; set; }

            public DateTime CreatedAt { get; set; }
        }

        public class LoanHistoryEntryDTO
        {
            public int LoanId { get; set; }
            public string BorrowerName { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime? ActualReturnDate { get; set; }
            public string Status { get; set; } = string.Empty;

            //Frozen item condition at time of loan
            public string SnapshotCondition { get; set; } = string.Empty;
            public List<LoanDTO.LoanSnapshotPhotoDTO> SnapshotPhotos { get; set; } = new();

            //Fines and disputes tied to this loan
            public List<FineDTO.FineResponseDTO> Fines { get; set; } = new();
            public List<DisputeDTO.DisputeSummaryDTO> Disputes { get; set; } = new();
        }
    }
}