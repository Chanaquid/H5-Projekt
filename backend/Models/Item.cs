using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class Item
    {

        public int Id { get; set; }

        //Concurrency
        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        //Owner info
        public string OwnerId { get; set; } = string.Empty;
        public ApplicationUser Owner { get; set; } = null!;

        //Category info
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        //Item details
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; } 
        public ItemCondition Condition { get; set; }

        //Permanent QR code for every item — generated once on item creation. Visible to owner and admin only. Used for both pickup and return confirmation.
        [MaxLength(12)]
        public string QrCode { get; set; } = string.Empty; 


        public int? MinLoanDays { get; set; } //Owner can require minimum loan period

        public bool RequiresVerification { get; set; } = false; //If true, only verified users can request a loan

        //Pickup Location
        [MaxLength(500)]
        public string PickupAddress { get; set; } = string.Empty;
        public double PickupLatitude {  get; set; }
        public double PickupLongitude {  get; set; }

        //Availability
        public DateTime AvailableFrom { get; set; }
        public DateTime AvailableUntil { get; set; }


        //Status 
        public ItemStatus Status { get; set; } = ItemStatus.Pending;
        public string? AdminNote { get; set; } //Admin can explain why it was rejected
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set;}


        //Navigations
        public ICollection<ItemPhoto> Photos { get; set; } = new List<ItemPhoto>();
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
        public ICollection<UserFavoriteItem> FavoritedBy { get; set; } = new List<UserFavoriteItem>(); //For admin, admin can see how many users favorited this item (useful for statistics)
        public ICollection<ItemReview> Reviews { get; set; } = new List<ItemReview>();




    }
}
