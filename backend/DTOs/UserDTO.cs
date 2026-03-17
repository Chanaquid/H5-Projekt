using static backend.DTOs.ChatDTO;
using static backend.DTOs.LoanDTO;

namespace backend.DTOs
{
    public class UserDTO
    {
        //---------------Requests----------------
        //User upadte their own profile
        public class UpdateProfileDTO
        {
            public string FullName { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string? Address { get; set; } = string.Empty;
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public string? Gender { get; set; }
            public string? AvatarUrl { get; set; }
        }

        //User delete their own account - need password confirmation
        public class DeleteAccountDTO
        {
            public string Password { get; set; } = string.Empty;
        }


        //Admin manually adjust a user's score
        public class AdminScoreAdjustDTO
        {
            public int PointsChanged { get; set; } //Signed: +10 or -10
            public string Note { get; set; } = string.Empty;
        }


        //Admin edits a user's profile and account fields
        public class AdminEditUserDTO
        {
            public string? FullName { get; set; }
            public string? Username { get; set; }// null = no change
            public string? Email { get; set; }
            public string? NewPassword { get; set; } // null = no change
            public string? Address { get; set; }
            public string? Gender { get; set; }
            public string? AvatarUrl { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public bool? IsVerified { get; set; }
            public string? Role { get; set; } = string.Empty; // "User" or "Admin"
            public int? Score { get; set; } // null = no change
            public string? ScoreNote { get; set; } // Required if Score is set
            public decimal? UnpaidFinesTotal { get; set; } // null = no change
        }

        public class AdminDeleteResultDTO
        {
            public bool Success { get; set; }
            public List<string> Warnings { get; set; } = new();
        }



        //------------------RESPONSES---------------

        //User profile
        public class UserProfileDTO
        {
            public string Id { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public string? Gender { get; set; }
            public DateTime DateOfBirth { get; set; }
            public int Age { get; set; }
            public string? AvatarUrl { get; set; }
            public int Score { get; set; }
            public decimal UnpaidFinesTotal { get; set; }
            public bool IsVerified { get; set; }
            public string BorrowingStatus { get; set; } = string.Empty; //"Free", "AdminApproval", "Blocked"
            public DateTime MembershipDate { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // Minimal user info exposed
        public class UserSummaryDTO
        {
            public string Id { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? AvatarUrl { get; set; }
            public int Score { get; set; }
            public bool IsVerified { get; set; }
            public int CompletedLoansCount { get; set; }

        }

        //Admin view of a user - can see everything
        public class AdminUserDTO
        {
            public string Id { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public string? Gender { get; set; }
            public int Age { get; set; }
            public string? AvatarUrl { get; set; }
            public int Score { get; set; }
            public decimal UnpaidFinesTotal { get; set; }
            public bool IsVerified { get; set; }
            public string Role { get; set; } = string.Empty;
            public bool IsDeleted { get; set; }
            public DateTime? DeletedAt { get; set; }
            public DateTime MembershipDate { get; set; }
            public DateTime CreatedAt { get; set; }
        }


        //Admin detail view — full user info including chat history and blocks
        public class AdminUserDetailDTO : AdminUserDTO
        {
            public List<FineDTO.FineResponseDTO> Fines { get; set; } = new();
            public List<ScoreHistoryDTO> ScoreHistory { get; set; } = new();
            public List<LoanSummaryDTO> LoansAsBorrower { get; set; } = new();
            public List<ItemDTO.ItemSummaryDTO> Items { get; set; } = new();
            public List<AppealDTO.AppealResponseDTO> Appeals { get; set; } = new();
            public List<VerificationDTO.VerificationRequestResponseDTO> VerificationRequests { get; set; } = new();
            public List<UserBlockDTO.BlockResponseDTO> BlockedUsers { get; set; } = new();
            public List<LoanMessageDTO.LoanMessageResponseDTO> LoanMessages { get; set; } = new();
            public List<DirectMessageDTO.DirectConversationSummaryDTO> DirectConversations { get; set; } = new();
        }


        //Score history entry — shown on user profile timeline
        public class ScoreHistoryDTO
        {
            public int Id { get; set; }
            public int PointsChanged { get; set; }
            public int ScoreAfterChange { get; set; }
            public string Reason { get; set; } = string.Empty; //"OnTimeReturn", "LateReturn", "AdminAdjustment"
            public string? Note { get; set; }
            public int? LoanId { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
