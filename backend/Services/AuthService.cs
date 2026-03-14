using backend.Common;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;


        public AuthService(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        //Register
        public async Task<AuthDTO.AuthResponseDTO> RegisterAsync(AuthDTO.RegisterDTO dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email.Trim().ToLower());
            if (existing != null)
            {
                //Soft-deleted accounts have their email anonymised to deleted_xxx@deleted.loanit
                //This guard catches any edge case where it does.
                if (existing.IsDeleted)
                    throw new ArgumentException("This email was previously used on a deleted account. Please contact support.");

                throw new ArgumentException("An account with this email already exists.");
            }

            //Check if username is already taken
            var usernameTaken = await _userManager.FindByNameAsync(dto.Username.Trim());
            if (usernameTaken != null)
                throw new ArgumentException("That username is already taken.");


            var user = new ApplicationUser
            {
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                UserName = dto.Username.Trim(),
                Address = dto.Address.Trim(),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender?.Trim(),
                AvatarUrl = dto.AvatarUrl?.Trim(),
                Score = 100,
                MembershipDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ArgumentException(errors);
            }

            await _userManager.AddToRoleAsync(user, Roles.User);

            //Send confirmation email — user cant log in until email is confirmed
            var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmLink = $"{_configuration["App:BaseUrl"]}/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(confirmToken)}";

            await _emailService.SendEmailAsync(
                user.Email,
                "Confirm your LoanIt account",
                $"<h2>Welcome to LoanIt, {user.FullName}!</h2>" +
                $"<p>Please confirm your email address by clicking the link below:</p>" +
                $"<p><a href='{confirmLink}'>Confirm Email</a></p>" +
                $"<p>If you did not create an account, you can ignore this email.</p>"
            );

            //Return a response without a token — ""Angular should show "check your email" screen""
            return new AuthDTO.AuthResponseDTO
            {
                UserId = user.Id,
                FullName = user.FullName,
                Username = user.UserName,
                Email = user.Email,
                Role = Roles.User
                //Token is intentionally empty — login is blocked until email is confirmed
            };
        }

        //Confirm email

        public async Task<bool> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }

        //Login
        public async Task<AuthDTO.AuthResponseDTO> LoginAsync(AuthDTO.LoginDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email.Trim().ToLower());

            if (user == null)
                throw new ArgumentException("Invalid email or password.");

            if (user.IsDeleted)
                throw new ArgumentException("This account has been deleted.");

            if (!await _userManager.IsEmailConfirmedAsync(user))
                throw new ArgumentException("Please confirm your email before logging in.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
                throw new ArgumentException("Account is locked. Please try again in 10 minutes.");

            if (!result.Succeeded)
                throw new ArgumentException("Invalid email or password.");

            return await BuildAuthResponseAsync(user);
        }

        //Refresh token
        public async Task<AuthDTO.AuthResponseDTO> RefreshTokenAsync(AuthDTO.RefreshTokenDTO dto)
        {
            //Hash the incoming token and look up by hash — never store plain text
            var tokenHash = HashToken(dto.RefreshToken);

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == tokenHash);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid refresh token.");

            if (user.IsDeleted)
                throw new UnauthorizedAccessException("This account has been deleted.");

            if (user.RefreshTokenExpiry == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");

            return await BuildAuthResponseAsync(user);
        }

        //Logout
        public async Task LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;

            await _userManager.UpdateAsync(user);
        }

        //Forgot password
        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email.Trim().ToLower());

            //Always return true — never reveal whether an email exists in the system
            if (user == null) return true;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = $"{_configuration["App:BaseUrl"]}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

            await _emailService.SendEmailAsync(
                email,
                "Reset your LoanIt password",
                "<h2>Password Reset</h2>" +
                "<p>We received a request to reset the password for your LoanIt account.</p>" +
                $"<p><a href='{resetLink}'>Reset Password</a></p>" +
                "<p>This link expires in 1 hour. If you did not request a password reset, you can ignore this email.</p>"
            );

            return true;
        }

        //Reset password
        public async Task<bool> ResetPasswordAsync(AuthDTO.ResetPasswordDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email.Trim().ToLower());
            if (user == null) return false;

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            return result.Succeeded;
        
        }

        //Resend confirmation email
        public async Task ResendConfirmationEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email.Trim().ToLower());

            //Always return silently — never reveal whether the email exists
            if (user == null) return;

            if (user.IsDeleted) return;

            if (await _userManager.IsEmailConfirmedAsync(user)) return;

            var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmLink = $"{_configuration["App:BaseUrl"]}/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(confirmToken)}";

            await _emailService.SendEmailAsync(
                user.Email!,
                "Confirm your LoanIt account (resent)",
                $"<h2>Email Confirmation</h2>" +
                $"<p>Here is your new confirmation link for your LoanIt account:</p>" +
                $"<p><a href='{confirmLink}'>Confirm Email</a></p>" +
                $"<p>If you did not request this, you can ignore this email.</p>"
            );
        }



        //JWT + refresh token builder
        private async Task<AuthDTO.AuthResponseDTO> BuildAuthResponseAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? Roles.User;
            var jwt = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = DateTime.UtcNow.AddHours(1);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, role),
                new Claim("score", user.Score.ToString()),
                new Claim("isVerified", user.IsVerified.ToString().ToLower()),
                new Claim("securityStamp", user.SecurityStamp ?? string.Empty) //Security stamp — if user changes password, all existing JWTs are immediately invalid


            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            //Rotate refresh token on every use — old token immediately invalidated
            var rawRefreshToken = GenerateRefreshToken();
            user.RefreshToken = HashToken(rawRefreshToken);
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            //if this update fails, throw before returning the token
            //so we never hand out a token the DB doesn't recognise
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException("Failed to persist refresh token. Please try again.");

            return new AuthDTO.AuthResponseDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = rawRefreshToken,
                UserId = user.Id,
                FullName = user.FullName,
                Username = user.UserName!,
                Email = user.Email!,
                Role = role,
                Score = user.Score,
                UnpaidFinesTotal = user.UnpaidFinesTotal,
                ExpiresAt = expiresAt
            };
        }

        //Generates a cryptographically random 64-byte base64 string
        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }


    }


}
