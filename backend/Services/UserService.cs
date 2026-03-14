using backend.Common;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using static backend.DTOs.ChatDTO;
using static backend.DTOs.LoanDTO;

namespace backend.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserBlockRepository _userBlockRepository;
        private readonly ILoanMessageRepository _loanMessageRepository;
        private readonly IDirectMessageRepository _directMessageRepository;
        private readonly IFineRepository _fineRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IAppealRepository _appealRepository;
        private readonly IVerificationRepository _verificationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public UserService(
            IUserRepository userRepository,
            IUserBlockRepository userBlockRepository,
            ILoanMessageRepository loanMessageRepository,
            IDirectMessageRepository directMessageRepository,
            IFineRepository fineRepository,
            ILoanRepository loanRepository,
            IItemRepository itemRepository,
            IAppealRepository appealRepository,
            IVerificationRepository verificationRepository,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _userBlockRepository = userBlockRepository;
            _loanMessageRepository = loanMessageRepository;
            _directMessageRepository = directMessageRepository;
            _fineRepository = fineRepository;
            _loanRepository = loanRepository;
            _itemRepository = itemRepository;
            _appealRepository = appealRepository;
            _verificationRepository = verificationRepository;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        // Get own profile
        public async Task<UserDTO.UserProfileDTO> GetProfileAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            return MapToProfileDTO(user);
        }

        // Get public profile — safe subset, accessible by any authenticated user
        public async Task<UserDTO.UserSummaryDTO> GetPublicProfileAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            return new UserDTO.UserSummaryDTO
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                Score = user.Score,
                IsVerified = user.IsVerified
            };
        }

        // Update own profile
        public async Task<UserDTO.UserProfileDTO> UpdateProfileAsync(string userId, UserDTO.UpdateProfileDTO dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            // Username change — check it isn't taken by someone else
            var newUsername = dto.UserName.Trim();
            if (!string.Equals(user.UserName, newUsername, StringComparison.OrdinalIgnoreCase))
            {
                var taken = await _userManager.FindByNameAsync(newUsername);
                if (taken != null)
                    throw new ArgumentException("That username is already taken.");

                var usernameResult = await _userManager.SetUserNameAsync(user, newUsername);
                if (!usernameResult.Succeeded)
                {
                    var errors = string.Join(", ", usernameResult.Errors.Select(e => e.Description));
                    throw new ArgumentException(errors);
                }
            }

            user.FullName = dto.FullName.Trim();
            user.Address = dto.Address?.Trim();
            user.Latitude = dto.Latitude;
            user.Longitude = dto.Longitude;
            user.Gender = dto.Gender?.Trim();
            user.AvatarUrl = dto.AvatarUrl?.Trim();

            await _userRepository.SaveChangesAsync();

            return MapToProfileDTO(user);
        }

        // Delete own account
        public async Task DeleteAccountAsync(string userId, UserDTO.DeleteAccountDTO dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Password is required.");

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
                throw new ArgumentException("Incorrect password.");

            if (user.UnpaidFinesTotal > 0)
                throw new InvalidOperationException("You have unpaid fines. Please settle them before deleting your account.");

            var ongoingStatuses = new[] { LoanStatus.Pending, LoanStatus.AdminPending, LoanStatus.Approved, LoanStatus.Active, LoanStatus.Late };

            var borrowedLoans = await _loanRepository.GetByBorrowerIdAsync(userId);
            if (borrowedLoans.Any(l => ongoingStatuses.Contains(l.Status)))
                throw new InvalidOperationException("You have ongoing loans as a borrower. Return all items before deleting your account.");

            var ownedLoans = await _loanRepository.GetByOwnerIdAsync(userId);
            if (ownedLoans.Any(l => ongoingStatuses.Contains(l.Status)))
                throw new InvalidOperationException("Someone is currently borrowing one of your items. Wait for all loans to complete before deleting your account.");

            SoftDelete(user);

            await _userRepository.SaveChangesAsync();
        }

        // Get score history
        public async Task<List<UserDTO.ScoreHistoryDTO>> GetScoreHistoryAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var history = await _userRepository.GetScoreHistoryAsync(userId);

            return history.Select(s => new UserDTO.ScoreHistoryDTO
            {
                Id = s.Id,
                PointsChanged = s.PointsChanged,
                ScoreAfterChange = s.ScoreAfterChange,
                Reason = s.Reason.ToString(),
                Note = s.Note,
                LoanId = s.LoanId,
                CreatedAt = s.CreatedAt
            }).ToList();
        }

        // Admin manually adjusts a user's score
        public async Task AdminAdjustScoreAsync(string targetUserId, UserDTO.AdminScoreAdjustDTO dto)
        {
            var user = await _userRepository.GetByIdAsync(targetUserId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var newScore = user.Score + dto.PointsChanged;

            if (newScore > 100)
                throw new ArgumentException($"Cannot adjust score. Current score is {user.Score} — adding {dto.PointsChanged} would exceed the maximum of 100.");

            if (newScore < 0)
                throw new ArgumentException($"Cannot adjust score. Current score is {user.Score} — adding {dto.PointsChanged} would go below the minimum of 0.");

            var entry = new ScoreHistory
            {
                UserId = user.Id,
                PointsChanged = dto.PointsChanged,
                ScoreAfterChange = newScore,
                Reason = ScoreChangeReason.AdminAdjustment,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow
            };

            user.Score = newScore;

            await _userRepository.AddScoreHistoryAsync(entry);
            await _userRepository.SaveChangesAsync();
        }

        // Admin gets all users — list view, no chat data
        public async Task<List<UserDTO.AdminUserDTO>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var result = new List<UserDTO.AdminUserDTO>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(MapToAdminDTO(user, roles.FirstOrDefault() ?? Roles.User));
            }

            return result;
        }

        // Admin gets single user — full detail view including all related data
        public async Task<UserDTO.AdminUserDetailDTO> GetUserByIdAsync(string userId)
        {
            var user = await _userRepository.GetByIdIgnoreFiltersAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? Roles.User;

            var blocks = await _userBlockRepository.GetBlocksByUserIdAsync(userId);
            var loanMessages = await _loanMessageRepository.GetByUserIdAsync(userId);
            var conversations = await _directMessageRepository.GetConversationsByUserIdAsync(userId);
            var fines = await _fineRepository.GetByUserIdAsync(userId);
            var scoreHistory = await _userRepository.GetScoreHistoryAsync(userId);
            var loansAsBorrower = await _loanRepository.GetByBorrowerIdAsync(userId);
            var items = await _itemRepository.GetByOwnerAsync(userId);
            var appeals = await _appealRepository.GetAllByUserIdAsync(userId);
            var verifications = await _verificationRepository.GetAllByUserIdAsync(userId);

            // Build direct conversation summaries — admin sees all messages, no cutoff applied
            var convSummaries = new List<DirectMessageDTO.DirectConversationSummaryDTO>();
            foreach (var c in conversations)
            {
                var isInitiator = c.InitiatedById == userId;
                var msgs = await _directMessageRepository.GetMessagesAsync(c.Id, null);
                var otherUser = isInitiator ? c.OtherUser : c.InitiatedBy;
                var lastMessage = msgs.OrderByDescending(m => m.SentAt).FirstOrDefault();

                convSummaries.Add(new DirectMessageDTO.DirectConversationSummaryDTO
                {
                    Id = c.Id,
                    OtherUserId = otherUser.Id,
                    OtherUserName = otherUser.FullName,
                    OtherUserAvatarUrl = otherUser.AvatarUrl,
                    LastMessageContent = lastMessage?.Content,
                    LastMessageAt = lastMessage?.SentAt,
                    UnreadCount = msgs.Count(m => m.SenderId != userId && !m.IsRead),
                    IsHidden = isInitiator ? c.HiddenForInitiator : c.HiddenForOther,
                    CreatedAt = c.CreatedAt
                });
            }

            var detail = MapToAdminDetailDTO(user, role);

            detail.Fines = fines.Select(f => new FineDTO.FineResponseDTO
            {
                Id = f.Id,
                LoanId = f.LoanId,
                ItemTitle = f.Loan?.Item?.Title ?? string.Empty,
                Type = f.Type.ToString(),
                Status = f.Status.ToString(),
                Amount = f.Amount,
                ItemValueAtTimeOfFine = f.ItemValueAtTimeOfFine,
                PaymentProofImageUrl = f.PaymentProofImageUrl,
                PaymentDescription = f.PaymentDescription,
                RejectionReason = f.RejectionReason,
                PaidAt = f.PaidAt,
                VerifiedAt = f.VerifiedAt,
                DisputeId = f.DisputeId,
                CreatedAt = f.CreatedAt
            }).ToList();

            detail.ScoreHistory = scoreHistory.Select(s => new UserDTO.ScoreHistoryDTO
            {
                Id = s.Id,
                PointsChanged = s.PointsChanged,
                ScoreAfterChange = s.ScoreAfterChange,
                Reason = s.Reason.ToString(),
                Note = s.Note,
                LoanId = s.LoanId,
                CreatedAt = s.CreatedAt
            }).ToList();

            detail.LoansAsBorrower = loansAsBorrower.Select(l => new LoanSummaryDTO
            {
                Id = l.Id,
                ItemTitle = l.Item?.Title ?? string.Empty,
                ItemPrimaryPhoto = l.Item?.Photos?.FirstOrDefault(p => p.IsPrimary)?.PhotoUrl,
                OtherPartyName = l.Item?.Owner?.FullName ?? string.Empty,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Status = l.Status.ToString(),
                HasUnreadMessages = false,
                DaysOverdue = l.Status == LoanStatus.Active && l.EndDate < DateTime.UtcNow
                    ? (int)(DateTime.UtcNow - l.EndDate).TotalDays
                    : null
            }).ToList();

            detail.Items = items.Select(i => new ItemDTO.ItemSummaryDTO
            {
                Id = i.Id,
                Title = i.Title,
                Condition = i.Condition.ToString(),
                PickupAddress = i.PickupAddress,
                PickupLatitude = i.PickupLatitude,
                PickupLongitude = i.PickupLongitude,
                AvailableFrom = i.AvailableFrom,
                AvailableUntil = i.AvailableUntil,
                PrimaryPhotoUrl = i.Photos?.FirstOrDefault(p => p.IsPrimary)?.PhotoUrl,
                CategoryName = i.Category?.Name ?? string.Empty,
                CategoryIcon = i.Category?.Icon,
                OwnerName = user.FullName,
                AverageRating = i.Reviews?.Any() == true ? i.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = i.Reviews?.Count ?? 0,
                IsCurrentlyOnLoan = i.Loans?.Any(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Late) ?? false
            }).ToList();

            detail.Appeals = appeals.Select(a => new AppealDTO.AppealResponseDTO
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = user.FullName,
                UserScore = user.Score,
                Message = a.Message,
                Status = a.Status.ToString(),
                AdminNote = a.AdminNote,
                ResolvedByAdminName = a.ResolvedByAdmin?.FullName,
                CreatedAt = a.CreatedAt,
                ResolvedAt = a.ResolvedAt
            }).ToList();

            detail.VerificationRequests = verifications.Select(v => new VerificationDTO.VerificationRequestResponseDTO
            {
                Id = v.Id,
                UserId = v.UserId,
                UserName = user.FullName,
                UserEmail = user.Email!,
                DocumentUrl = v.DocumentUrl,
                DocumentType = v.DocumentType.ToString(),
                Status = v.Status.ToString(),
                AdminNote = v.AdminNote,
                ReviewedByAdminName = v.ReviewedByAdmin?.FullName,
                SubmittedAt = v.SubmittedAt,
                ReviewedAt = v.ReviewedAt
            }).ToList();

            detail.BlockedUsers = blocks.Select(b => new UserBlockDTO.BlockResponseDTO
            {
                BlockerId = b.BlockerId,
                BlockedId = b.BlockedId,
                BlockedUserName = b.Blocked?.FullName ?? string.Empty,
                BlockedUserAvatarUrl = b.Blocked?.AvatarUrl,
                CreatedAt = b.CreatedAt
            }).ToList();

            detail.LoanMessages = loanMessages.Select(m => new LoanMessageDTO.LoanMessageResponseDTO
            {
                Id = m.Id,
                LoanId = m.LoanId,
                SenderId = m.SenderId,
                SenderName = m.Sender?.FullName ?? string.Empty,
                SenderAvatarUrl = m.Sender?.AvatarUrl,
                Content = m.Content,
                IsRead = m.IsRead,
                SentAt = m.SentAt
            }).ToList();

            detail.DirectConversations = convSummaries;

            return detail;
        }

        // Admin edits a user's profile, account fields, score, and unpaid fines total
        public async Task<UserDTO.AdminUserDTO> AdminEditUserAsync(string targetUserId, string adminId, UserDTO.AdminEditUserDTO dto)
        {
            var user = await _userRepository.GetByIdIgnoreFiltersAsync(targetUserId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            // Profile fields — only update if provided
            if (!string.IsNullOrWhiteSpace(dto.FullName))
                user.FullName = dto.FullName.Trim();

            if (dto.Address != null)
                user.Address = dto.Address.Trim();

            if (dto.Latitude.HasValue)
                user.Latitude = dto.Latitude;

            if (dto.Longitude.HasValue)
                user.Longitude = dto.Longitude;

            if (dto.Gender != null)
                user.Gender = dto.Gender.Trim();

            if (dto.AvatarUrl != null)
                user.AvatarUrl = dto.AvatarUrl.Trim();

            if (dto.IsVerified.HasValue)
                user.IsVerified = dto.IsVerified.Value;

            //Username change — check uniqueness
            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                var newUsername = dto.Username.Trim();
                if (!string.Equals(user.UserName, newUsername, StringComparison.OrdinalIgnoreCase))
                {
                    var taken = await _userManager.FindByNameAsync(newUsername);
                    if (taken != null && taken.Id != targetUserId)
                        throw new ArgumentException("That username is already taken.");

                    var usernameResult = await _userManager.SetUserNameAsync(user, newUsername);
                    if (!usernameResult.Succeeded)
                    {
                        var errors = string.Join(", ", usernameResult.Errors.Select(e => e.Description));
                        throw new ArgumentException(errors);
                    }
                }
            }

            //Email change — resets confirmation, sends new verification email
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
                if (!string.Equals(user.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
                {
                    var emailTaken = await _userManager.FindByEmailAsync(dto.Email.Trim());
                    if (emailTaken != null && emailTaken.Id != targetUserId)
                        throw new ArgumentException("That email is already in use.");

                    var emailResult = await _userManager.SetEmailAsync(user, dto.Email.Trim());
                    if (!emailResult.Succeeded)
                    {
                        var errors = string.Join(", ", emailResult.Errors.Select(e => e.Description));
                        throw new ArgumentException(errors);
                    }

                    user.EmailConfirmed = false;


                    //Send confirmation email to new address
                    var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmLink = $"{_configuration["App:BaseUrl"]}/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(confirmToken)}";

                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Confirm your new LoanIt email address",
                        $"<h2>Email address updated</h2>" +
                        $"<p>An admin has updated the email address on your LoanIt account.</p>" +
                        $"<p>Please confirm your new email address by clicking the link below:</p>" +
                        $"<p><a href='{confirmLink}'>Confirm Email</a></p>" +
                        $"<p>If you did not request this change, please contact support immediately.</p>"
                    );
                }
            }

            //Password change
            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                    throw new ArgumentException(errors);
                }
            }

            //Role change
            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                var currentRole = currentRoles.FirstOrDefault() ?? Roles.User;

                if (!string.Equals(currentRole, dto.Role, StringComparison.OrdinalIgnoreCase))
                {
                    if (currentRoles.Any())
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);

                    await _userManager.AddToRoleAsync(user, dto.Role);
                }
            }

            //Score change — log to ScoreHistory
            if (dto.Score.HasValue)
            {
                var newScore = dto.Score.Value;

                if (newScore < 0 || newScore > 100)
                    throw new ArgumentException("Score must be between 0 and 100.");

                if (newScore != user.Score)
                {
                    var pointsChanged = newScore - user.Score;

                    await _userRepository.AddScoreHistoryAsync(new ScoreHistory
                    {
                        UserId = user.Id,
                        PointsChanged = pointsChanged,
                        ScoreAfterChange = newScore,
                        Reason = ScoreChangeReason.AdminAdjustment,
                        Note = dto.ScoreNote?.Trim() ?? $"Score set to {newScore} by admin.",
                        CreatedAt = DateTime.UtcNow
                    });

                    user.Score = newScore;
                }
            }

            //Unpaid fines total override
            if (dto.UnpaidFinesTotal.HasValue)
            {
                if (dto.UnpaidFinesTotal.Value < 0)
                    throw new ArgumentException("Unpaid fines total cannot be negative.");

                user.UnpaidFinesTotal = Math.Round(dto.UnpaidFinesTotal.Value, 2);
            }

            await _userRepository.SaveChangesAsync();

            var updatedUser = await _userRepository.GetByIdIgnoreFiltersAsync(targetUserId)
                ?? throw new KeyNotFoundException("User not found after update.");


            var roles = await _userManager.GetRolesAsync(updatedUser);
            return MapToAdminDTO(updatedUser, roles.FirstOrDefault() ?? Roles.User);
        }

        //Admin soft delete
        public async Task<UserDTO.AdminDeleteResultDTO> AdminSoftDeleteUserAsync(string targetUserId, string adminId)
        {
            var user = await _userRepository.GetByIdAsync(targetUserId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var warnings = new List<string>();
            var ongoingStatuses = new[] { LoanStatus.Pending, LoanStatus.AdminPending, LoanStatus.Approved, LoanStatus.Active, LoanStatus.Late };

            //Unpaid fines — warn but allow
            if (user.UnpaidFinesTotal > 0)
                warnings.Add($"User had {user.UnpaidFinesTotal:C} in unpaid fines at time of deletion.");

            //Borrowed loans — block if active/late (item is physically with them)
            var borrowedLoans = await _loanRepository.GetByBorrowerIdAsync(targetUserId);
            var activeBorrowedLoans = borrowedLoans
                .Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Late)
                .ToList();

            if (activeBorrowedLoans.Any())
                throw new InvalidOperationException(
                    $"Cannot delete: user currently has {activeBorrowedLoans.Count} active/late borrowed loan(s). " +
                    "The item is physically with them — resolve these loans first before deleting the account.");

            // Borrowed loans that are pending/approved — cancel them (item not picked up yet)
            var pendingBorrowedLoans = borrowedLoans
                .Where(l => l.Status == LoanStatus.Pending || l.Status == LoanStatus.AdminPending || l.Status == LoanStatus.Approved)
                .ToList();

            foreach (var loan in pendingBorrowedLoans)
            {
                loan.Status = LoanStatus.Cancelled;
            }

            if (pendingBorrowedLoans.Any())
                warnings.Add($"{pendingBorrowedLoans.Count} pending/approved borrowed loan(s) were automatically cancelled.");

            //Owned loans — transfer item ownership to admin
            var ownedItems = await _itemRepository.GetByOwnerAsync(targetUserId);
            foreach (var item in ownedItems)
            {
                //Guard against item.Loans not being loaded
                if (item.Loans == null)
                    throw new InvalidOperationException(
                        $"Loan data missing for item '{item.Title}'. " +
                        "Ensure GetByOwnerAsync eagerly loads item.Loans.");

                // Check if the item has any loan with status Active or Late
                bool isCurrentlyOut = item.Loans?.Any(l =>
                    l.Status == LoanStatus.Active || l.Status == LoanStatus.Late) ?? false;

                if (isCurrentlyOut)
                {
                    //Transfer ownership to admin so they can oversee the return
                    item.OwnerId = adminId;
                    _itemRepository.Update(item);
                    warnings.Add($"Item '{item.Title}' was transferred to admin because it is currently on loan.");
                }
                else
                {
                    //Item is NOT on loan. 
                    item.IsActive = false;
                    _itemRepository.Update(item);

                    //Cancel any pending loan requests on this item
                    var pendingItemLoans = item.Loans?.Where(l =>
                        l.Status == LoanStatus.Pending ||
                        l.Status == LoanStatus.AdminPending ||
                        l.Status == LoanStatus.Approved).ToList() ?? new();

                    foreach (var loan in pendingItemLoans)
                        loan.Status = LoanStatus.Cancelled;

                    if (pendingItemLoans.Any())
                        warnings.Add($"Item '{item.Title}' deactivated — {pendingItemLoans.Count} pending loan request(s) cancelled.");
                }
            }

            SoftDelete(user, adminId);

            await _userRepository.SaveChangesAsync();

            return new UserDTO.AdminDeleteResultDTO
            {
                Success = true,
                Warnings = warnings
            };
        }

        //Mappers

        private static UserDTO.UserProfileDTO MapToProfileDTO(ApplicationUser u)
        {
            return new UserDTO.UserProfileDTO
            {
                Id = u.Id,
                FullName = u.FullName,
                Username = u.UserName ?? string.Empty,
                Email = u.Email!,
                Address = u.Address ?? string.Empty,
                Latitude = u.Latitude,
                Longitude = u.Longitude,
                Gender = u.Gender,
                DateOfBirth = u.DateOfBirth,
                Age = u.Age,
                AvatarUrl = u.AvatarUrl,
                Score = u.Score,
                UnpaidFinesTotal = u.UnpaidFinesTotal,
                IsVerified = u.IsVerified,
                BorrowingStatus = GetBorrowingStatus(u.Score),
                MembershipDate = u.MembershipDate,
                CreatedAt = u.MembershipDate
            };
        }

        private static UserDTO.AdminUserDTO MapToAdminDTO(ApplicationUser u, string role)
        {
            return new UserDTO.AdminUserDTO
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!,
                Username = u.UserName ?? string.Empty,
                Address = u.Address ?? string.Empty,
                Latitude = u.Latitude,
                Longitude = u.Longitude,
                Gender = u.Gender,
                Age = u.Age,
                AvatarUrl = u.AvatarUrl,
                Score = u.Score,
                UnpaidFinesTotal = u.UnpaidFinesTotal,
                IsVerified = u.IsVerified,
                Role = role,
                IsDeleted = u.IsDeleted,
                DeletedAt = u.DeletedAt,
                MembershipDate = u.MembershipDate,
                CreatedAt = u.MembershipDate
            };
        }

        private static UserDTO.AdminUserDetailDTO MapToAdminDetailDTO(ApplicationUser u, string role)
        {
            return new UserDTO.AdminUserDetailDTO
            {
                Id = u.Id,
                FullName = u.FullName,
                Username = u.UserName ?? string.Empty,
                Email = u.Email!,
                Address = u.Address ?? string.Empty,
                Latitude = u.Latitude,
                Longitude = u.Longitude,
                Gender = u.Gender,
                Age = u.Age,
                AvatarUrl = u.AvatarUrl,
                Score = u.Score,
                UnpaidFinesTotal = u.UnpaidFinesTotal,
                IsVerified = u.IsVerified,
                Role = role,
                IsDeleted = u.IsDeleted,
                DeletedAt = u.DeletedAt,
                MembershipDate = u.MembershipDate,
                CreatedAt = u.MembershipDate
            };
        }

        //Helpers

        private static void SoftDelete(ApplicationUser user, string? adminId = null)
        {
            var shortId = Guid.NewGuid().ToString("N")[..8];

            user.UserName = $"deleted_{shortId}";
            user.NormalizedUserName = user.UserName.ToUpper();
            user.Email = $"deleted_{shortId}@deleted.loanit";
            user.NormalizedEmail = user.Email.ToUpper();
            user.FullName = $"Deleted User";
            user.AvatarUrl = null;
            user.PasswordHash = null;
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedByAdminId = adminId;
        }

        private static string GetBorrowingStatus(int score)
        {
            if (score >= 50) return "Free";
            if (score >= 20) return "AdminApproval";
            return "Blocked";
        }
    }
}