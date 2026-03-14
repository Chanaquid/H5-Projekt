using backend.Models;

namespace backend.DTOs
{
    public class AppealDTO
    {
        //-------------REQUESTS---------------------

        //User files an appeal when their score drops below 20
        public class CreateScoreAppealDTO
        {
            public string Message { get; set; } = string.Empty; //Their explanation/apology
        }

        //User submits a fine appeal
        public class CreateFineAppealDTO
        {
            public int FineId { get; set; }
            public string Message { get; set; } = string.Empty;
        }


        //Admin approves or rejects the score appeal
        public class AdminScoreAppealDecisionDTO
        {
            public bool IsApproved { get; set; }
            public string? AdminNote { get; set; }
            public int? NewScore { get; set; } //Optional — defaults to 20
        }

        //Admin decides a fine appeal
        public class AdminFineAppealDecisionDTO
        {
            public bool IsApproved { get; set; }
            public string? AdminNote { get; set; }
            public FineAppealResolution? Resolution { get; set; } //Required if approved
            public decimal? CustomFineAmount { get; set; }  //Required if resolution = Custom
        }



        //-----------------RESPONSES-----------------------
        public class AppealResponseDTO
        {
            public int Id { get; set; }
            public string UserId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public int UserScore { get; set; }
            public string AppealType { get; set; } = string.Empty;
            public int? FineId { get; set; }
            public decimal? FineAmount { get; set; }
            public string Message { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string? AdminNote { get; set; }
            public int? RestoredScore { get; set; }
            public string? FineResolution { get; set; }
            public decimal? CustomFineAmount { get; set; }
            public string? ResolvedByAdminName { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ResolvedAt { get; set; }
        }

    }
}
