using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using backend.Repositories;
using System.Security.Cryptography;

namespace backend.Services
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepository;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly ILoanService _loanService;
        private readonly ICategoryRepository _categoryRepository;
        public ItemService(
            IItemRepository itemRepository,
            INotificationService notificationService,
            IUserRepository userRepository,
            ILoanService loanService,
            ICategoryRepository categoryRepository)
        {
            _itemRepository = itemRepository;
            _notificationService = notificationService;
            _userRepository = userRepository;
            _loanService = loanService;
            _categoryRepository = categoryRepository;
        }

        //Get all approved and active items
        public async Task<List<ItemDTO.ItemSummaryDTO>> GetAllApprovedAsync()
        {
            var items = await _itemRepository.GetAllApprovedAsync();
            return items.Select(MapToSummaryDTO).ToList();
        }

        //Get all items - admin
        public async Task<List<ItemDTO.ItemSummaryDTO>> GetAllForAdminAsync(bool includeInactive = false)
        {
            var items = await _itemRepository.GetAllAsync(includeInactive);
            return items.Select(MapToSummaryDTO).ToList();
        }

        public async Task<List<ItemDTO.ItemSummaryDTO>> GetPublicByOwnerAsync(string ownerId)
        {
            var items = await _itemRepository.GetPublicByOwnerAsync(ownerId);
            return items.Select(MapToSummaryDTO).ToList();
        }

        //Get item by id
        public async Task<ItemDTO.ItemDetailDTO> GetByIdAsync(int itemId, string? requestingUserId, bool isAdmin = false)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} not found.");

            //Admins can see everything, owners can see their own, others only see approved
            if (!isAdmin && item.OwnerId != requestingUserId && item.Status != ItemStatus.Approved)
                throw new KeyNotFoundException($"Item {itemId} not found.");

            return MapToDetailDTO(item);
        }

        //Get item by radius
        public async Task<List<ItemDTO.ItemSummaryDTO>> GetNearbyAsync(double lat, double lng, double radiusKm)
        {
            var items = await _itemRepository.GetAllApprovedAsync();
            return items
                .Where(i => CalculateDistanceKm(lat, lng, i.PickupLatitude, i.PickupLongitude) <= radiusKm)
                .Select(MapToSummaryDTO)
                .ToList();
        }

        private static double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371; //Earth radius in km
            var dLat = ToRad(lat2 - lat1);
            var dLng = ToRad(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180;

        //Create item
        public async Task<ItemDTO.ItemDetailDTO> CreateAsync(string ownerId, ItemDTO.CreateItemDTO dto, bool isAdmin = false)
        {

            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId);
            if (category == null)
                throw new ArgumentException($"Category with ID {dto.CategoryId} does not exist.");

            if (dto.CurrentValue < 0)
                throw new ArgumentException("Current value cant be negative");


            if (dto.AvailableFrom.Date < DateTime.UtcNow.Date)
                throw new ArgumentException("The availability start date cannot be in the past.");

            if (dto.AvailableFrom >= dto.AvailableUntil)
                throw new ArgumentException("AvailableFrom must be before AvailableUntil.");

        
            //minimum loan days must be smaller than the whole loan window
            if (dto.MinLoanDays.HasValue && dto.MinLoanDays > 0)
            {
                var availableDays = (int)(dto.AvailableUntil - dto.AvailableFrom).TotalDays;
                if (availableDays < dto.MinLoanDays.Value)
                    throw new ArgumentException($"Availability window ({availableDays} days) must be at least equal to the minimum loan period ({dto.MinLoanDays} days).");
            }

            //Default pickup location to owner's address if not provided
            var owner = await _userRepository.GetByIdAsync(ownerId);
            if (owner == null)
                throw new KeyNotFoundException("Owner not found.");

            var pickupAddress = string.IsNullOrWhiteSpace(dto.PickupAddress)
                ? owner.Address
                : dto.PickupAddress.Trim();

            var pickupLat = dto.PickupLatitude ?? owner.Latitude ?? 0;
            var pickupLng = dto.PickupLongitude ?? owner.Longitude ?? 0;

            var item = new Item
            {
                OwnerId = ownerId,
                CategoryId = dto.CategoryId,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                CurrentValue = dto.CurrentValue,
                Condition = dto.Condition,
                MinLoanDays = dto.MinLoanDays,
                RequiresVerification = dto.RequiresVerification,
                PickupAddress = pickupAddress,
                PickupLatitude = pickupLat,
                PickupLongitude = pickupLng,
                AvailableFrom = dto.AvailableFrom,
                AvailableUntil = dto.AvailableUntil,
                Status = isAdmin ? ItemStatus.Approved : ItemStatus.Pending,  //Always starts pending — admin must approve
                IsActive = true,
                QrCode = await GenerateUniqueQrCodeAsync(),
                CreatedAt = DateTime.UtcNow
            };

            await _itemRepository.AddAsync(item);
            await _itemRepository.SaveChangesAsync();

            // Reload with full details for the response
            var created = await _itemRepository.GetByIdWithDetailsAsync(item.Id);
            return MapToDetailDTO(created!);
        }

        //Update item details
        public async Task<ItemDTO.ItemDetailDTO> UpdateAsync(int itemId, string ownerId, ItemDTO.UpdateItemDTO dto, bool isAdmin = false)
        {

            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} not found.");

            var categoryId = dto.CategoryId ?? item.CategoryId;
            if (dto.CategoryId.HasValue)
            {
                var category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Value);
                if (category == null)
                    throw new ArgumentException($"Category with ID {dto.CategoryId} does not exist.");
            }


            if (!isAdmin && item.OwnerId != ownerId) //Only owner can edit their own items (also admins ofc)
                throw new UnauthorizedAccessException("You can only edit your own items.");

            if(dto.CurrentValue < 0)
            {
                throw new ArgumentException($"Current value cannot be negative");
            }


            var availableFrom = dto.AvailableFrom ?? item.AvailableFrom;
            var availableUntil = dto.AvailableUntil ?? item.AvailableUntil;

            if (availableFrom >= availableUntil)
                throw new ArgumentException("AvailableFrom must be before AvailableUntil.");


            //minimum loan days must be smaller than the whole loan window
            if (dto.MinLoanDays.HasValue && dto.MinLoanDays > 0)
            {
                var availableDays = (int)(availableUntil - availableFrom).TotalDays;
                if (availableDays < dto.MinLoanDays.Value)
                    throw new ArgumentException($"Availability window ({availableDays} days) must be at least equal to the minimum loan period ({dto.MinLoanDays} days).");
            }


            item.CategoryId = categoryId;
            item.Title = dto.Title.Trim();
            item.Description = dto.Description.Trim();
            item.CurrentValue = dto.CurrentValue;
            item.Condition = dto.Condition;
            item.MinLoanDays = dto.MinLoanDays;
            item.RequiresVerification = dto.RequiresVerification;
            item.PickupAddress = string.IsNullOrWhiteSpace(dto.PickupAddress)
                ? item.PickupAddress
                : dto.PickupAddress.Trim();
            item.PickupLatitude = dto.PickupLatitude;
            item.PickupLongitude = dto.PickupLongitude;
            item.AvailableFrom = availableFrom;
            item.AvailableUntil = availableUntil;
            item.IsActive = dto.IsActive;
            item.UpdatedAt = DateTime.UtcNow;

            //Editing a rejected item resubmits it for approval
            if (item.Status == ItemStatus.Rejected)
                item.Status = ItemStatus.Pending;

            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();

            var updated = await _itemRepository.GetByIdWithDetailsAsync(item.Id);
            return MapToDetailDTO(updated!);
        }


        //Solely for admin so they can change item status
        public async Task<ItemDTO.ItemDetailDTO> UpdateStatusAsync(int itemId, ItemDTO.AdminItemStatusDTO dto)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} not found.");

            if (!Enum.TryParse<ItemStatus>(dto.Status, out var newStatus))
                throw new ArgumentException($"Invalid status '{dto.Status}'. Valid values: Pending, Approved, Rejected.");

            item.Status = newStatus;
            item.AdminNote = dto.AdminNote?.Trim();
            item.UpdatedAt = DateTime.UtcNow;

            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();

            var updated = await _itemRepository.GetByIdWithDetailsAsync(item.Id);
            return MapToDetailDTO(updated!);
        }

        //Toggle item status
        public async Task<ItemDTO.ItemDetailDTO> ToggleActiveAsync(int itemId, string ownerId, bool isActive, bool isAdmin = false)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} not found.");

            if (!isAdmin && item.OwnerId != ownerId)
                throw new UnauthorizedAccessException("You can only update your own items.");

            var isOnLoan = item.Loans?.Any(l =>
                  l.Status == LoanStatus.Active ||
                  l.Status == LoanStatus.Late) ?? false;

            if (!isActive && isOnLoan)
                throw new InvalidOperationException("Cannot deactivate an item that is currently on loan.");

            item.IsActive = isActive;
            item.UpdatedAt = DateTime.UtcNow;

            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();

            var updated = await _itemRepository.GetByIdWithDetailsAsync(item.Id);
            return MapToDetailDTO(updated!);
        }

        //Delete item
        public async Task DeleteAsync(int itemId, string ownerId, bool isAdmin = false)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} not found.");

            if (!isAdmin && item.OwnerId != ownerId)
                throw new UnauthorizedAccessException("You can only delete your own items.");

            //Block deletion if there is any pending, approved, or active loan on this item
            var ongoingStatuses = new[] { LoanStatus.Pending, LoanStatus.AdminPending, LoanStatus.Approved, LoanStatus.Active };
            var hasOngoingLoan = item.Loans?.Any(l => ongoingStatuses.Contains(l.Status)) ?? false;

            if (hasOngoingLoan)
                throw new InvalidOperationException("This item cannot be deleted while it has an ongoing loan.");

            _itemRepository.Delete(item);
            await _itemRepository.SaveChangesAsync();
        }

        //Get All items by owner
        public async Task<List<ItemDTO.ItemSummaryDTO>> GetByOwnerAsync(string ownerId)
        {
            var items = await _itemRepository.GetByOwnerAsync(ownerId);
            return items.Select(MapToSummaryDTO).ToList();
        }

        //Get qr code
        public async Task<ItemDTO.ItemQrCodeDTO> GetQrCodeAsync(int itemId, string requestingUserId, bool isAdmin = false)
        {
            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} not found.");

            //Only owner can retrieve QR 
            if (!isAdmin && item.OwnerId != requestingUserId)
                throw new UnauthorizedAccessException("Only the item owner can view the QR code.");

            return new ItemDTO.ItemQrCodeDTO
            {
                ItemId = item.Id,
                QrCode = item.QrCode!
            };
        }

        /*
        //scan qr code
        public async Task<ItemDTO.ItemDetailDTO> ScanQrCodeAsync(string qrCode, string scannedByUserId, bool isAdmin = false)
        {
            var item = await _itemRepository.GetByQrCodeAsync(qrCode);
            if (item == null)
                throw new KeyNotFoundException("Invalid QR code.");

            //Find the relevant active loan for this item
            var activeLoan = item.Loans
                .FirstOrDefault(l => l.Status == LoanStatus.Approved || l.Status == LoanStatus.Active);

            if (activeLoan == null)
                throw new InvalidOperationException("No active loan found for this item.");

            if (activeLoan.Status == LoanStatus.Approved)
            {
                //Pickup scan — borrower scans to confirm pickup
                if (!isAdmin && activeLoan.BorrowerId != scannedByUserId)
                    throw new UnauthorizedAccessException("Only the borrower can confirm pickup.");

                activeLoan.Status = LoanStatus.Active;

                await _notificationService.SendAsync(
                    item.OwnerId,
                    NotificationType.LoanActive,
                    $"'{item.Title}' has been picked up by the borrower.",
                    activeLoan.Id,
                    NotificationReferenceType.Loan
                );
            }
            else if (activeLoan.Status == LoanStatus.Active)
            {
                //Return scan — delegate to LoanService.HandleLoanReturnAsync
                if (!isAdmin && activeLoan.BorrowerId != scannedByUserId)
                    throw new UnauthorizedAccessException("Only the borrower can confirm the return.");

                //Use the loan service method for proper handling
                await _loanService.HandleLoanReturnAsync(activeLoan);

            }

            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();

            var updated = await _itemRepository.GetByIdWithDetailsAsync(item.Id);
            return MapToDetailDTO(updated!);
        }
        */

        public async Task<ItemDTO.ItemDetailDTO> ScanQrCodeAsync(string qrCode, string scannedByUserId, bool isAdmin = false)
        {
            var item = await _itemRepository.GetByQrCodeAsync(qrCode);
            if (item == null)
                throw new KeyNotFoundException("Invalid QR code.");

            // Find the relevant active or approved loan for this item
            var loan = item.Loans
                .FirstOrDefault(l => l.Status == LoanStatus.Approved ||
                                     l.Status == LoanStatus.Active ||
                                     l.Status == LoanStatus.Late);

            if (loan == null)
                throw new InvalidOperationException("No active or approved loan found for this item.");

            // Only borrower can confirm pickup or return
            if (!isAdmin && loan.BorrowerId != scannedByUserId)
                throw new UnauthorizedAccessException("Only the borrower can scan this item.");

            // --- PICKUP LOGIC ---
            if (loan.Status == LoanStatus.Approved)
            {
                if (DateTime.UtcNow < loan.StartDate)
                    throw new InvalidOperationException("This loan cannot be picked up before the start date.");

                loan.Status = LoanStatus.Active;
                loan.UpdatedAt = DateTime.UtcNow;

                _itemRepository.Update(item);
                await _itemRepository.SaveChangesAsync();

                await _notificationService.SendAsync(
                    item.OwnerId,
                    NotificationType.LoanActive,
                    $"'{item.Title}' has been picked up by the borrower.",
                    loan.Id,
                    NotificationReferenceType.Loan
                );
            }
            // --- RETURN LOGIC ---
            else if (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Late)
            {
                await _loanService.HandleLoanReturnAsync(loan);

            }
            else
            {
                throw new InvalidOperationException("Loan is not in a state that can be picked up or returned.");
            }


            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();

            // Return updated item details
            var updatedItem = await _itemRepository.GetByIdWithDetailsAsync(item.Id);
            return MapToDetailDTO(updatedItem!);
        }


        //ADmin pending approvals
        public async Task<List<ItemDTO.AdminPendingItemDTO>> GetPendingApprovalsAsync()
        {
            var items = await _itemRepository.GetPendingApprovalsAsync();
            return items.Select(i => new ItemDTO.AdminPendingItemDTO
            {
                Id = i.Id,
                Title = i.Title,
                Description = i.Description,
                CurrentValue = i.CurrentValue,
                Condition = i.Condition.ToString(),
                OwnerName = i.Owner.FullName,
                OwnerEmail = i.Owner.Email!,
                OwnerScore = i.Owner.Score,
                CategoryName = i.Category.Name,
                Photos = i.Photos.Select(MapToPhotoDTO).ToList(),
                CreatedAt = i.CreatedAt
            }).ToList();
        }

        //Admin decide
        public async Task<ItemDTO.ItemDetailDTO> AdminDecideAsync(int itemId, string adminId, ItemDTO.AdminItemDecisionDTO dto)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} not found.");

            if (item.Status != ItemStatus.Pending)
                throw new InvalidOperationException("Only pending items can be approved or rejected.");

            if (!dto.IsApproved && string.IsNullOrWhiteSpace(dto.AdminNote))
                throw new ArgumentException("A reason is required when rejecting an item.");

            item.Status = dto.IsApproved ? ItemStatus.Approved : ItemStatus.Rejected;
            item.AdminNote = dto.AdminNote?.Trim();
            item.UpdatedAt = DateTime.UtcNow;

            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                item.OwnerId,
                dto.IsApproved ? NotificationType.ItemApproved : NotificationType.ItemRejected,
                dto.IsApproved
                    ? $"Your item '{item.Title}' has been approved and is now live."
                    : $"Your item '{item.Title}' was rejected. Reason: {dto.AdminNote}",
                item.Id,
                NotificationReferenceType.Item
            );

            return MapToDetailDTO(item);
        }

        //QRcode generator
        private async Task<string> GenerateUniqueQrCodeAsync()
        {
            const int maxAttempts = 10;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const int length = 12;

            for (int i = 0; i < maxAttempts; i++)
            {
                var code = new string(
                    Enumerable.Range(0, length)
                        .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
                        .ToArray()
                );

                if (!await _itemRepository.QrCodeExistsAsync(code))
                    return code;
            }

            throw new InvalidOperationException("Failed to generate a unique QR code. Please try again.");
        }

        //DAILY BG JOB
        public async Task DeactivateExpiredItemsAsync()
        {
            var expiredItems = await _itemRepository.GetActiveItemsExpiredBeforeAsync(DateTime.UtcNow.Date);

            foreach (var item in expiredItems)
            {
                item.IsActive = false;
                item.UpdatedAt = DateTime.UtcNow;
                _itemRepository.Update(item);
            }

            await _itemRepository.SaveChangesAsync();
        }


        //PICS
        public async Task<ItemDTO.ItemPhotoDTO> AddPhotoAsync(int itemId, string ownerId, ItemDTO.AddItemPhotoDTO dto, bool isAdmin = false)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} not found.");

            if (!isAdmin && item.OwnerId != ownerId)
                throw new UnauthorizedAccessException("You can only add photos to your own items.");

            //If this is set as primary, unset all existing primary photos
            if (dto.IsPrimary)
            {
                foreach (var existingPhoto in item.Photos)
                    existingPhoto.IsPrimary = false;
            }

            //If no photos exist yet, force this one to be primary
            var isPrimary = dto.IsPrimary || !item.Photos.Any();

            var photo = new ItemPhoto
            {
                ItemId = itemId,
                PhotoUrl = dto.PhotoUrl.Trim(),
                IsPrimary = isPrimary,
                DisplayOrder = dto.DisplayOrder
            };

            item.Photos.Add(photo);
            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();

            return MapToPhotoDTO(photo);
        }

        public async Task DeletePhotoAsync(int itemId, int photoId, string ownerId, bool isAdmin = false)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} not found.");

            if (!isAdmin && item.OwnerId != ownerId)
                throw new UnauthorizedAccessException("You can only delete photos from your own items.");

            var photo = item.Photos?.FirstOrDefault(p => p.Id == photoId);
            if (photo == null)
                throw new KeyNotFoundException($"Photo {photoId} not found.");

            var wasPrimary = photo.IsPrimary;
            item.Photos.Remove(photo);

            //If deleted photo was primary, promote the first remaining photo
            if (wasPrimary && item.Photos.Any())
                item.Photos.OrderBy(p => p.DisplayOrder).First().IsPrimary = true;

            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();
        }

        public async Task<ItemDTO.ItemPhotoDTO> SetPrimaryPhotoAsync(int itemId, int photoId, string ownerId, bool isAdmin = false)
        {
            var item = await _itemRepository.GetByIdWithDetailsAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Item {itemId} not found.");

            if (!isAdmin && item.OwnerId != ownerId)
                throw new UnauthorizedAccessException("You can only update photos on your own items.");

            var photo = item.Photos?.FirstOrDefault(p => p.Id == photoId);
            if (photo == null)
                throw new KeyNotFoundException($"Photo {photoId} not found.");

            foreach (var p in item.Photos)
                p.IsPrimary = false;

            photo.IsPrimary = true;

            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();

            return MapToPhotoDTO(photo);
        }




        //MAPPERS
        private static ItemDTO.ItemDetailDTO MapToDetailDTO(Item i)
        {
            var approvedReviews = i.Reviews?.ToList() ?? new();
            var activeLoans = i.Loans?.Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Late).ToList() ?? new();
            return new ItemDTO.ItemDetailDTO
            {
                Id = i.Id,
                Title = i.Title,
                Description = i.Description,
                CurrentValue = i.CurrentValue,
                Condition = i.Condition.ToString(),
                MinLoanDays = i.MinLoanDays,
                RequiresVerification = i.RequiresVerification,
                PickupAddress = i.PickupAddress,
                PickupLatitude = i.PickupLatitude,
                PickupLongitude = i.PickupLongitude,
                AvailableFrom = i.AvailableFrom,
                AvailableUntil = i.AvailableUntil,
                Status = i.Status.ToString(),
                IsActive = i.IsActive,
                AdminNote = i.AdminNote,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                Owner = new UserDTO.UserSummaryDTO
                {
                    Id = i.Owner.Id,
                    FullName = i.Owner.FullName,
                    Username = i.Owner.UserName ?? string.Empty,
                    Score = i.Owner.Score,
                    AvatarUrl = i.Owner.AvatarUrl,
                    IsVerified = i.Owner.IsVerified
                },
                Category = new CategoryDTO.CategoryResponseDTO
                {
                    Id = i.Category.Id,
                    Name = i.Category.Name,
                    Icon = i.Category.Icon,
                    IsActive = i.Category.IsActive
                },
                Photos = i.Photos?.OrderBy(p => p.DisplayOrder).Select(MapToPhotoDTO).ToList() ?? new(),
                AverageRating = approvedReviews.Any() ? Math.Round(approvedReviews.Average(r => r.Rating), 1) : 0,
                ReviewCount = approvedReviews.Count,
                IsCurrentlyOnLoan = i.Loans?.Any(l =>
                    l.Status == LoanStatus.Active ||
                    l.Status == LoanStatus.Late) ?? false
                };
        }

        private static ItemDTO.ItemSummaryDTO MapToSummaryDTO(Item i)
        {
            var reviews = i.Reviews?.ToList() ?? new();
            var activeLoans = i.Loans?.Where(l => l.Status == LoanStatus.Active).ToList() ?? new();

            return new ItemDTO.ItemSummaryDTO
            {
                Id = i.Id,
                Title = i.Title,
                Description = i.Description,
                Condition = i.Condition.ToString(),
                Status = i.Status.ToString(),
                PickupAddress = i.PickupAddress,
                PickupLatitude = i.PickupLatitude,
                PickupLongitude = i.PickupLongitude,
                AvailableFrom = i.AvailableFrom,
                AvailableUntil = i.AvailableUntil,
                PrimaryPhotoUrl = i.Photos?.FirstOrDefault(p => p.IsPrimary)?.PhotoUrl,
                CategoryName = i.Category?.Name ?? string.Empty,
                CategoryIcon = i.Category?.Icon,
                OwnerId = i.OwnerId,
                OwnerName = i.Owner?.FullName ?? string.Empty,
                AverageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0,
                ReviewCount = reviews.Count,
                IsActive = i.IsActive,
                IsCurrentlyOnLoan = activeLoans.Any()
            };
        }

        private static ItemDTO.ItemPhotoDTO MapToPhotoDTO(ItemPhoto p)
        {
            return new ItemDTO.ItemPhotoDTO
            {
                Id = p.Id,
                PhotoUrl = p.PhotoUrl,
                IsPrimary = p.IsPrimary,
                DisplayOrder = p.DisplayOrder
            };
        }







    }
}
