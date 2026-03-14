using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Gender { get; set; }

        [Required, MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime DateOfBirth { get; set; }

        //Calculated property so we don't have to store Age in the DB
        public int Age
        {
            get
            {
                var today = DateTime.UtcNow.Date;
                int age = today.Year - DateOfBirth.Year;
                if (DateOfBirth.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        public bool IsVerified { get; set; } = false; //IS VERIFIED??? real identity? or maybe scammer?


        public string? AvatarUrl { get; set; }

        public DateTime MembershipDate { get; set; } = DateTime.UtcNow;

        public int Score { get; set; } = 100;
        public decimal UnpaidFinesTotal { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //Soft delete
        //When user is deleted, their username and email is freed so they can create another acc with the same email and username
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByAdminId { get; set; }

        //Refresh tokens
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }

        //Navigations
        public ICollection<Item> OwnedItems { get; set; } = new List<Item>();
        public ICollection<Loan> BorrowedLoans { get; set; } = new List<Loan>();
        public ICollection<Fine> Fines {  get; set; } = new List<Fine>();
        public ICollection<ScoreHistory> ScoreHistory { get; set; } = new List<ScoreHistory>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Appeal> Appeals { get; set; } = new List<Appeal>();
        public ICollection<LoanMessage> SentLoanMessages { get; set; } = new List<LoanMessage>();
        public ICollection<DirectMessage> SentDirectMessages { get; set; } = new List<DirectMessage>();
        public ICollection<SupportMessage> SentSupportMessages { get; set; } = new List<SupportMessage>(); public ICollection<UserFavoriteItem> FavoriteItems { get; set; } = new List<UserFavoriteItem>();
        public ICollection<UserRecentlyViewedItem> RecentlyViewed { get; set; } = new List<UserRecentlyViewedItem>();
        public ICollection<ItemReview> ItemReviews { get; set; } = new List<ItemReview>();
        public ICollection<UserReview> ReviewsGiven { get; set; } = new List<UserReview>(); //Reviews this user wrote
        public ICollection<UserReview> ReviewsReceived { get; set; } = new List<UserReview>();  //Reviews written about this user
        public ICollection<VerificationRequest> VerificationRequests { get; set; } = new List<VerificationRequest>();
        public ICollection<UserBlock> BlockedUsers { get; set; } = new List<UserBlock>(); //Blocks this user has placed
        public ICollection<UserBlock> BlockedBy { get; set; } = new List<UserBlock>(); //All users that has blocked this user


    }
}
