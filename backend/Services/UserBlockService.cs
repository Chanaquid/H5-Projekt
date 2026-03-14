using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using static backend.DTOs.ChatDTO;

namespace backend.Services
{
    public class UserBlockService : IUserBlockService
    {
        private readonly IUserBlockRepository _userBlockRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserBlockService(
            IUserBlockRepository userBlockRepository,
            UserManager<ApplicationUser> userManager)
        {
            _userBlockRepository = userBlockRepository;
            _userManager = userManager;
        }

        public async Task<UserBlockDTO.BlockResponseDTO> BlockAsync(string blockerId, string blockedUserId)
        {
            // FIX (LOW): Guard against null/empty input before any DB calls
            if (string.IsNullOrWhiteSpace(blockedUserId))
                throw new ArgumentException("Blocked user ID is required.");

            if (blockerId == blockedUserId)
                throw new ArgumentException("You cannot block yourself.");

            var target = await _userManager.FindByIdAsync(blockedUserId);
            if (target == null)
                throw new KeyNotFoundException("User not found.");

            // Admins cannot be blocked
            var isAdmin = await _userManager.IsInRoleAsync(target, "Admin");
            if (isAdmin)
                throw new InvalidOperationException("You cannot block an admin.");

            // Already blocked — throw error
            var existing = await _userBlockRepository.GetAsync(blockerId, blockedUserId);
            if (existing != null)
                throw new InvalidOperationException("You have already blocked this user.");

            var block = new UserBlock
            {
                BlockerId = blockerId,
                BlockedId = blockedUserId,
                CreatedAt = DateTime.UtcNow
            };

            await _userBlockRepository.AddAsync(block);
            await _userBlockRepository.SaveChangesAsync();

            return MapToUserBlockDTO(block, target);
        }

        // Unblock the user
        public async Task UnblockAsync(string blockerId, string blockedUserId)
        {
            var block = await _userBlockRepository.GetAsync(blockerId, blockedUserId);
            if (block == null)
                throw new KeyNotFoundException("Block not found.");

            // FIX (SECURITY): Verify the caller owns the block — prevents one user
            // from removing another user's block if IDs are known.
            // Note: blockerId should always come from the authenticated user's claims
            // in the controller, not from user input.
            if (block.BlockerId != blockerId)
                throw new UnauthorizedAccessException("You can only remove your own blocks.");

            await _userBlockRepository.RemoveAsync(block);
            await _userBlockRepository.SaveChangesAsync();
        }

        // Get all blocked users for userId
        public async Task<List<UserBlockDTO.BlockResponseDTO>> GetBlockedUsersAsync(string userId)
        {
            var blocks = await _userBlockRepository.GetBlocksByUserIdAsync(userId);

            return blocks.Select(b =>
            {
                // FIX (MEDIUM): Guard against missing Blocked navigation property —
                // repository must Include(b => b.Blocked), but we fail clearly here
                // rather than throwing a NullReferenceException inside the mapper.
                if (b.Blocked == null)
                    throw new InvalidOperationException(
                        $"Blocked user data missing for block {b.BlockedId}. " +
                        $"Ensure the repository eagerly loads the Blocked navigation property.");

                return MapToUserBlockDTO(b, b.Blocked);
            }).ToList();
        }

        private static UserBlockDTO.BlockResponseDTO MapToUserBlockDTO(UserBlock block, ApplicationUser blocked)
        {
            return new UserBlockDTO.BlockResponseDTO
            {
                BlockerId = block.BlockerId,
                BlockedId = block.BlockedId,
                BlockedUserName = blocked.FullName,
                BlockedUserAvatarUrl = blocked.AvatarUrl,
                CreatedAt = block.CreatedAt
            };
        }
    }
}