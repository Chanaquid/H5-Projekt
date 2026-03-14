namespace backend.DTOs
{
    public class DisputeDTO
    {
        //---------------REQUESTS---------------
        //Either party files a dispute on a completed or active loan
        public class CreateDisputeDTO
        {
            public int LoanId { get; set; }
            public string FiledAs { get; set; } = string.Empty; //"AsOwner" or "AsBorrower"
            public string Description { get; set; } = string.Empty;
            //Photos uploaded separately via POST /api/disputes/{id}/photos
        }


        //Other party submits their side within the 72h window
        public class DisputeResponseDTO
        {
            public string ResponseDescription { get; set; } = string.Empty;
            //Photos uploaded separately via POST /api/disputes/{id}/photos
        }

        //Admin issues their final verdict
        public class AdminVerdictDTO
        {
            public string Verdict { get; set; } = string.Empty; //"OwnerFavored", "BorrowerFavored", "PartialDamage", "Inconclusive"
            public decimal? CustomFineAmount { get; set; }       //Required only when Verdict = "PartialDamage"
            public string AdminNote { get; set; } = string.Empty;
        }

        //Upload a photo as evidence
        public class AddDisputePhotoDTO
        {
            public string PhotoUrl { get; set; } = string.Empty;
            public string? Caption { get; set; }
        }

        //-------------RESPONSES----------------
        //Full dispute detail — shown on the dispute page
        public class DisputeDetailDTO
        {
            public int Id { get; set; }
            public int LoanId { get; set; }
            public string ItemTitle { get; set; } = string.Empty;

            //Who filed
            public string FiledByName { get; set; } = string.Empty;
            public string FiledAs { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;

            //Other party response
            public string? ResponseDescription { get; set; }
            public DateTime ResponseDeadline { get; set; }

            public string Status { get; set; } = string.Empty;

            //Admin verdict
            public string? AdminVerdict { get; set; }
            public decimal? CustomFineAmount { get; set; }
            public string? AdminNote { get; set; }
            public DateTime? ResolvedAt { get; set; }

            //Evidence — split by submitter so admin sees both sides clearly
            public List<DisputePhotoDTO> FiledByPhotos { get; set; } = new();
            public List<DisputePhotoDTO> ResponsePhotos { get; set; } = new();

            //Loan snapshot for before/after comparison
            public string SnapshotCondition { get; set; } = string.Empty;
            public List<LoanDTO.LoanSnapshotPhotoDTO> SnapshotPhotos { get; set; } = new();

            public DateTime CreatedAt { get; set; }
        }

        //Compact dispute — used in admin dispute queue and user dispute list
        public class DisputeSummaryDTO
        {
            public int Id { get; set; }
            public int LoanId { get; set; }
            public string ItemTitle { get; set; } = string.Empty;
            public string FiledByName { get; set; } = string.Empty;
            public string FiledAs { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime ResponseDeadline { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        //Single dispute photo
        public class DisputePhotoDTO
        {
            public int Id { get; set; }
            public string PhotoUrl { get; set; } = string.Empty;
            public string SubmittedByName { get; set; } = string.Empty;
            public string? Caption { get; set; }
            public DateTime UploadedAt { get; set; }
        }


    }
}
