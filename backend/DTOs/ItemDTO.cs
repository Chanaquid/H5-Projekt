using backend.Models;

namespace backend.DTOs
{
    public class ItemDTO
    {
        //---------------REQUESTS---------------------
        //Creating an item
        public class CreateItemDTO
        {
            public int CategoryId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal CurrentValue { get; set; }
            public ItemCondition Condition { get; set; }
            public int? MinLoanDays { get; set; }
            public bool RequiresVerification { get; set; } = false;
            public string? PickupAddress { get; set; } = string.Empty;
            public double? PickupLatitude { get; set; }
            public double? PickupLongitude { get; set; }
            public DateTime AvailableFrom { get; set; }
            public DateTime AvailableUntil { get; set; }
        }


        //Updating an item
        //cant change ownerId and Photos since photos have their own endpoint
        public class UpdateItemDTO
        {
            public int? CategoryId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal CurrentValue { get; set; }
            public ItemCondition Condition { get; set; }
            public int? MinLoanDays { get; set; }
            public bool RequiresVerification { get; set; }
            public string PickupAddress { get; set; } = string.Empty;
            public double PickupLatitude { get; set; }
            public double PickupLongitude { get; set; }
            public DateTime? AvailableFrom { get; set; }
            public DateTime? AvailableUntil { get; set; }
            public bool IsActive { get; set; }
        }

        //Admin approves or rejects
        public class AdminItemDecisionDTO
        {
            public bool IsApproved { get; set; }
            public string? AdminNote { get; set; } //if rejected — explained why
        }


        //------------------RESPONSES-----------------------
        //Full item detail — shown on item detail page
        public class ItemDetailDTO
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal CurrentValue { get; set; }
            public string Condition { get; set; } = string.Empty;
            public int? MinLoanDays { get; set; }
            public bool RequiresVerification { get; set; }
            public string PickupAddress { get; set; } = string.Empty;
            public double PickupLatitude { get; set; }
            public double PickupLongitude { get; set; }
            public DateTime AvailableFrom { get; set; }
            public DateTime AvailableUntil { get; set; }
            public string Status { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public string? AdminNote { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }

            //Related
            public UserDTO.UserSummaryDTO Owner { get; set; } = null!;
            public CategoryDTO.CategoryResponseDTO Category { get; set; } = null!;
            public List<ItemPhotoDTO> Photos { get; set; } = new();
            public double AverageRating { get; set; }
            public int ReviewCount { get; set; }

            //Is this item currently out on an active loan?
            public bool IsCurrentlyOnLoan { get; set; }
        }

        //Compact item card — used in browse grid and map pins
        public class ItemSummaryDTO
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string Condition { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string PickupAddress { get; set; } = string.Empty;
            public double PickupLatitude { get; set; }
            public double PickupLongitude { get; set; }
            public DateTime AvailableFrom { get; set; }
            public DateTime AvailableUntil { get; set; }
            public string? PrimaryPhotoUrl { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string? CategoryIcon { get; set; }
            public string OwnerId { get; set; } = string.Empty;

            public string OwnerName { get; set; } = string.Empty;
            public string OwnerUsername { get; set; } = string.Empty;
            public string OwnerAvatarUrl {  get; set; } = string.Empty;
            public double AverageRating { get; set; }
            public int ReviewCount { get; set; }
            public bool IsActive { get; set; }
            public bool IsCurrentlyOnLoan { get; set; }
        }


        //Single photo
        public class ItemPhotoDTO
        {
            public int Id { get; set; }
            public string PhotoUrl { get; set; } = string.Empty;
            public bool IsPrimary { get; set; }
            public int DisplayOrder { get; set; }
        }

        //Add/upload an item photo
        public class AddItemPhotoDTO
        {
            public string PhotoUrl { get; set; } = string.Empty;
            public bool IsPrimary { get; set; } = false;
            public int DisplayOrder { get; set; } = 0;
        }



        public class ToggleActiveStatusDTO
        {
            public bool IsActive { get; set; }
        }

        //QRcode response - returned only to admin and owner
        public class ItemQrCodeDTO
        {
            public int ItemId { get; set; }
            public string QrCode { get; set; } = string.Empty;
        }

        //Admin pending approval queue
        public class AdminPendingItemDTO
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal CurrentValue { get; set; }
            public string Condition { get; set; } = string.Empty;
            public string OwnerName { get; set; } = string.Empty;
            public string OwnerEmail { get; set; } = string.Empty;
            public int OwnerScore { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public List<ItemPhotoDTO> Photos { get; set; } = new();
            public DateTime CreatedAt { get; set; }
        }


        //Admin can change item status
        public class AdminItemStatusDTO
        {
            public string Status { get; set; } = string.Empty; // "Pending", "Approved", "Rejected"
            public string? AdminNote { get; set; }
        }
    }
}
