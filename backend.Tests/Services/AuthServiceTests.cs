using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace backend.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfig;

        private readonly AuthService _authService;


        public AuthServiceTests()
        {
            //UserManager needs a special mock setup helper
            _mockUserManager = MockUserManager<ApplicationUser>();
            _mockEmailService = new Mock<IEmailService>();
            _mockConfig = new Mock<IConfiguration>();

            //SignInManager also needs a special mock setup
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);

            _authService = new AuthService(
                _mockUserManager.Object,
                _mockEmailService.Object,
                _mockSignInManager.Object,
                _mockConfig.Object);
        }

        //Helper to mock UserManager
        private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            return new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
        }


        //TESTS

        [Fact]
        public async Task RegisterAsync_EmailAlreadyRegistered_ThrowsArgumentException()
        {
            var dto = new AuthDTO.RegisterDTO
            {
                Email = "test@gmail.com",
                Username = "testuser",
                Password = "SecurePassword123!",
                FullName = "Test User",
                Address = "123 Main St"
            };

            var existingUser = new ApplicationUser { Email = dto.Email, IsDeleted = false };

            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email.Trim().ToLower()))
                .ReturnsAsync(existingUser);


            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _authService.RegisterAsync(dto));


            Assert.Equal("An account with this email already exists.", exception.Message);

            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }


        [Fact]
        public async Task RegisterAsync_UsernameAlreadyExists_ThrowsArgumentException()
        {
            var dto = new AuthDTO.RegisterDTO
            {
                Email = "test@gmail.com",
                Username = "testuser",
                Password = "SecurePassword123!",
                FullName = "Test User",
                Address = "123 Main St"
            };

            var existingUserWithSameUsername = new ApplicationUser { UserName = dto.Username };

            //STEP 1: Tell the mock the EMAIL is available (returns null)
            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email.Trim().ToLower()))
                .ReturnsAsync((ApplicationUser)null);

            //STEP 2: Tell the mock the USERNAME is taken
            _mockUserManager.Setup(x => x.FindByNameAsync(dto.Username.Trim()))
                .ReturnsAsync(existingUserWithSameUsername);


            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _authService.RegisterAsync(dto));


            Assert.Equal("That username is already taken.", exception.Message);

            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }


        [Fact]
        public async Task RegisterAsync_ValidEmail_ReturnsAuthResponseAndSendsEmailConfirmation()
        {
            var dto = new AuthDTO.RegisterDTO
            {
                Email = "test@gmail.com",
                Username = "testuser",
                Password = "SecurePassword123!",
                FullName = "Test User",
                Address = "123 Main St"
            };

            //Email not found
            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email.Trim().ToLower()))
                .ReturnsAsync((ApplicationUser)null);

            //Username not found
            _mockUserManager.Setup(x => x.FindByNameAsync(dto.Username.Trim()))
                .ReturnsAsync((ApplicationUser)null);

            //Mock User Creation (Success)
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), dto.Password))
                .ReturnsAsync(IdentityResult.Success);

            //Mock Role Assignment
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                .ReturnsAsync(IdentityResult.Success);

            //Mock Token Generation
            _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("fake-token");

            //Mock Configuration for the BaseUrl
            _mockConfig.Setup(x => x["App:BaseUrl"]).Returns("https://localhost:5000");


            var result = await _authService.RegisterAsync(dto);


            Assert.NotNull(result);
            Assert.Equal(dto.Email, result.Email);
            Assert.Equal(dto.FullName, result.FullName);
            Assert.Equal("User", result.Role);
            Assert.Empty(result.Token);


            _mockUserManager.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u =>
                u.Email == dto.Email.ToLower() &&
                u.FullName == dto.FullName &&
                u.Score == 100
            ), dto.Password), Times.Once);

            //Verify confirmation email was sent
            _mockEmailService.Verify(x => x.SendEmailAsync(
                dto.Email,
                It.IsAny<string>(),
                It.Is<string>(body => body.Contains("fake-token"))
            ), Times.Once);


        }

        [Fact]
        public async Task RegisterAsync_EmailWasDeleted_ThrowsSupportMessage()
        {
            var dto = new AuthDTO.RegisterDTO { Email = "deleteduser@gmail.com" };
            var existingUser = new ApplicationUser {  Email = dto.Email, IsDeleted = true };

            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUser);


            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(dto));
            Assert.Equal("This email was previously used on a deleted account. Please contact support.", ex.Message);
        }


        [Fact]
        public async Task RegisterAsync_IdentityFails_ThrowsIdentityMessage()
        {
            var dto = new AuthDTO.RegisterDTO { Email = "test@gmail.com" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null);

            _mockUserManager.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null);


            var identityError = IdentityResult.Failed(new IdentityError { Description = "Password too short" });
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                 .ReturnsAsync(identityError);


            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _authService.RegisterAsync(dto));
            Assert.Equal("Password too short", ex.Message);
        }


        [Fact]
        public async Task ConfirmEmailAsync_UserNotFound_ReturnsFalse()
        {
            string userId = "userId";
            string token = "token";

            _mockUserManager.Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null);


            var result = await _authService.ConfirmEmailAsync(userId, token);

            Assert.False(result);

            _mockUserManager.Verify(x => x.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);

        }

        [Fact]
        public async Task ConfirmEmailAsync_ValidUserAndToken_ReturnsTrue()
        {
            string userId = "userId";
            string token = "token";

            var user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _mockUserManager.Setup(x => x.ConfirmEmailAsync(user, token))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _authService.ConfirmEmailAsync(userId, token);


            Assert.True(result);

        }


        [Fact]
        public async Task LoginAsync_UserNotFound_ThrowsArgumentException()
        {
            var dto = new AuthDTO.LoginDTO { Email = "test@gmail.com", Password = "Password1234" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null);


            var error = await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(dto));

            Assert.Equal("Invalid email or password.", error.Message);


        }


        [Fact]
        public async Task LoginAsync_AccountIsDeleted_ThrowsArgumentException()
        {
            var dto = new AuthDTO.LoginDTO { Email = "deletedtest@gmail.com", Password = "Password1234" };
            var user = new ApplicationUser { Email = dto.Email, IsDeleted = true };

            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);


            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(dto));


            Assert.Equal("This account has been deleted.", ex.Message);

        }


        [Fact]
        public async Task LoginAsync_UnconfirmedEmail_ThrowsArgumentException()
        {
            var dto = new AuthDTO.LoginDTO { Email = "test@gmail.com", Password = "Password1234" };
            var user = new ApplicationUser { Email = dto.Email, IsDeleted = false };

            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
                .ReturnsAsync(false);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(dto));


            Assert.Equal("Please confirm your email before logging in.", ex.Message);

        }


        [Fact]
        public async Task LoginAsync_AccountLocked_ThrowsArgumentException()
        {
            var dto = new AuthDTO.LoginDTO { Email = "test@gmail.com", Password = "Password1234" };
            var user = new ApplicationUser { Email = dto.Email, IsDeleted = false };

            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
                .ReturnsAsync(true);

            //Force the Lockout result
            _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, dto.Password, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut); ;


            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(dto));


            Assert.Equal("Account is locked. Please try again in 10 minutes.", ex.Message);

        }


        [Fact]
        public async Task LoginAsync_InvalidEmailOrPass_ThrowsArgumentException()
        {
            var dto = new AuthDTO.LoginDTO { Email = "test@gmail.com", Password = "Password1234" };
            var user = new ApplicationUser { Email = dto.Email, IsDeleted = false };

            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
                .ReturnsAsync(true);

            _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, dto.Password, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);


            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(dto));


            Assert.Equal("Invalid email or password.", ex.Message);

        }


        [Fact]
        public async Task LoginAsync_ValidEmailPass_ReturnsAuthResponse()
        {
            var dto = new AuthDTO.LoginDTO
            {
                Email = "success@gmail.com",
                Password = "CorrectPassword123!"
            };

            var user = new ApplicationUser
            {
                Id = "user-123",
                Email = dto.Email,
                FullName = "Test User",
                UserName = "testuser",
                Score = 100,
                UnpaidFinesTotal = 0,
                IsDeleted = false
            };

            //Find user
            _mockUserManager
                .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            //Email confirmed
            _mockUserManager
                .Setup(x => x.IsEmailConfirmedAsync(user))
                .ReturnsAsync(true);

            //Password check
            _mockSignInManager
                .Setup(x => x.CheckPasswordSignInAsync(user, dto.Password, true))
                .ReturnsAsync(SignInResult.Success);

            //Roles
            _mockUserManager
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });

            //Update user (refresh token save)
            _mockUserManager
                .Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            //Mock JWT configuration section
            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x["Key"])
                .Returns("a_very_long_secret_key_at_least_32_chars");
            jwtSection.Setup(x => x["Issuer"])
                .Returns("TestIssuer");
            jwtSection.Setup(x => x["Audience"])
                .Returns("TestAudience");

            _mockConfig
                .Setup(x => x.GetSection("Jwt"))
                .Returns(jwtSection.Object);

            var result = await _authService.LoginAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.FullName, result.FullName);
            Assert.Equal("User", result.Role);
            Assert.NotEmpty(result.Token);        
            Assert.NotEmpty(result.RefreshToken);
        }


        [Fact]
        public async Task RefreshTokenAsync_InvalidRefreshToken_UnauthorizedAccessException()
        {
            var dto = new AuthDTO.RefreshTokenDTO { RefreshToken = "refreshtoken" };

            var userList = new List<ApplicationUser>();

            var mockUsers = userList.BuildMock();

            _mockUserManager
                .Setup(x => x.Users)
                .Returns(mockUsers);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.RefreshTokenAsync(dto));

            Assert.Equal("Invalid refresh token.", ex.Message);
        }


        [Fact]
        public async Task RefreshTokenAsync_AccountDeleted_UnauthorizedAccessException()
        {

            var dto = new AuthDTO.RefreshTokenDTO { RefreshToken = "refreshtoken" };

            var userList = new List<ApplicationUser>()
            {
                new ApplicationUser { Email = "test@gmail.com", IsDeleted = true, RefreshToken = AuthService.HashToken(dto.RefreshToken)}
            };

            var mockUsers = userList.BuildMock();

            _mockUserManager
                .Setup(x => x.Users)
                .Returns(mockUsers);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.RefreshTokenAsync(dto));

            Assert.Equal("This account has been deleted.", ex.Message);
        }


        [Fact]
        public async Task RefreshTokenAsync_TokenExpired_UnauthorizedAccessException()
        {

            var dto = new AuthDTO.RefreshTokenDTO { RefreshToken = "refreshtoken" };

            var userList = new List<ApplicationUser>()
            {
                new ApplicationUser { Email = 
                "test@gmail.com", 
                IsDeleted = false,
                RefreshToken = AuthService.HashToken(dto.RefreshToken),
                RefreshTokenExpiry = null
                }
            };

            var mockUsers = userList.BuildMock();

            _mockUserManager
                .Setup(x => x.Users)
                .Returns(mockUsers);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.RefreshTokenAsync(dto));

            Assert.Equal("Refresh token has expired. Please log in again.", ex.Message);
        }


        [Fact]
        public async Task RefreshTokenAsync_ValidToken_ReturnsAuthResponse()
        {
            // --- 1. Arrange Data ---
            var rawToken = "validrefreshtoken";
            var dto = new AuthDTO.RefreshTokenDTO { RefreshToken = rawToken };
            var hashedToken = AuthService.HashToken(rawToken);

            var user = new ApplicationUser
            {
                Id = "user-1",
                Email = "test@gmail.com",
                FullName = "Test User",
                UserName = "testuser",
                IsDeleted = false,
                RefreshToken = hashedToken,
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(7),
                SecurityStamp = Guid.NewGuid().ToString(),
                Score = 100,
                IsVerified = true
            };

            // --- 2. Mock Configuration (Fixes NullReferenceException) ---
            var mockJwtSection = new Mock<IConfigurationSection>();
            mockJwtSection.Setup(s => s["Key"]).Returns("super_secret_key_at_least_32_chars_long");
            mockJwtSection.Setup(s => s["Issuer"]).Returns("TestIssuer");
            mockJwtSection.Setup(s => s["Audience"]).Returns("TestAudience");

            _mockConfig
                .Setup(x => x.GetSection("Jwt"))
                .Returns(mockJwtSection.Object);

            // --- 3. Mock UserManager Methods ---
            var mockUsers = new List<ApplicationUser> { user }.BuildMock();

            _mockUserManager.Setup(x => x.Users).Returns(mockUsers);

            _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "User" });

            _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // --- 4. Act ---
            var result = await _authService.RefreshTokenAsync(dto);

            // --- 5. Assert ---
            Assert.NotNull(result);
            Assert.NotEmpty(result.Token);
            Assert.NotEqual(rawToken, result.RefreshToken); // Should be a NEW rotated token
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal("User", result.Role);
        }


        [Fact]
        public async Task LogoutAsync_ValidUser_ClearsTokenAndUpdate()
        {

            var userId = "active-user";
            var user = new ApplicationUser
            {
                Id = userId,
                RefreshToken = "active-token",

            };

            _mockUserManager.Setup(z => z.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            await _authService.LogoutAsync(userId);

            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiry);
            _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);


        }


        [Fact]
        public async Task LogoutAsync_UserNotFound_ReturnsGracefully()
        {

            var userId = "nonexistent-user";;

            _mockUserManager.Setup(z => z.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null);

            var expection = await Record.ExceptionAsync(() => _authService.LogoutAsync(userId));

            Assert.Null(expection);

            _mockUserManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);


        }


        [Fact]
        public async Task ResetPassword_ValidUser_ReturnsTrue()
        {

            var dto = new AuthDTO.ResetPasswordDTO {Email = "ValidUserEmail@com", Token = "validToken", NewPassword="Newpass2213" };
            var user = new ApplicationUser { Email = "validuseremail@com" };

            _mockUserManager.Setup(z => z.FindByEmailAsync(user.Email)).ReturnsAsync(user);

            _mockUserManager.Setup(x => x.ResetPasswordAsync(user, dto.Token, dto.NewPassword))
                    .ReturnsAsync(IdentityResult.Success);

            var result = await _authService.ResetPasswordAsync(dto);

            // Assert
            Assert.True(result);

        }



        [Fact]
        public async Task ResetPasswordAsync_InvalidToken_ReturnsFalse()
        {
            var dto = new AuthDTO.ResetPasswordDTO { Email = "test@gmail.com", Token = "expired-token" };
            var user = new ApplicationUser { Email = "test@gmail.com" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            _mockUserManager.Setup(x => x.ResetPasswordAsync(user, dto.Token, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid Token" }));

            var result = await _authService.ResetPasswordAsync(dto);

            Assert.False(result);
        }


        [Fact]
        public async Task ResetPasswordAsync_InvalidUser_ReturnsFalse()
        {
            var dto = new AuthDTO.ResetPasswordDTO { Email = "invalidUser@gmail.com", Token = "invalidToekn" };
            var user = new ApplicationUser { Email = "invaliduser@gmail.com" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null);


            var result = await _authService.ResetPasswordAsync(dto);

            _mockUserManager.Verify(x => x.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.False(result);
        }


        [Fact]
        public async Task ResendConfirmationEmail_InvalidUser_DoesNotSendEmail()
        {
            var dto = new AuthDTO.ResetPasswordDTO { Email = "userNotFound@gmail.com" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email.Trim().ToLower()))
                .ReturnsAsync((ApplicationUser)null);


            await _authService.ResendConfirmationEmailAsync(dto.Email.Trim().ToLower());

            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);


        }

        [Fact]
        public async Task ResendConfirmationEmail_EmailAlreadyConfirmed_DoesNotSendEmail()
        {
            var dto = new AuthDTO.ResetPasswordDTO { Email = "emailNotConfirmed@gmail.com" };

            var user = new ApplicationUser { Email = "emailnotconfirmed@gmail.com", EmailConfirmed = true };

            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email.Trim().ToLower()))
                .ReturnsAsync(user);

            _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

            await _authService.ResendConfirmationEmailAsync(dto.Email);

            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);


        }


        [Fact]
        public async Task ResendConfirmationEmail_UserDeleted_DoesNotSendEmail()
        {
            var dto = new AuthDTO.ResetPasswordDTO { Email = "deletedUser@gmail.com" };

            var user = new ApplicationUser { Email = "deleteduser@gmail.com", IsDeleted =  true };

            _mockUserManager.Setup(x => x.FindByEmailAsync(dto.Email.Trim().ToLower()))
                .ReturnsAsync(user);


            await _authService.ResendConfirmationEmailAsync(dto.Email);

            _mockUserManager.Verify(x => x.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>()), Times.Never);


            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }


        [Fact]
        public async Task ResendConfirmationEmail_ValidUser_SendsEmail()
        {
            var email = "test@gmail.com";
            var user = new ApplicationUser
            {
                Id = "user-123",
                Email = email,
                IsDeleted = false
            };
            var mockToken = "secure-confirmation-token";
            var baseUrl = "https://myapp.com";

            _mockUserManager.Setup(x => x.FindByEmailAsync(email.Trim().ToLower()))
                .ReturnsAsync(user);

            _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
                .ReturnsAsync(false);

            //Mock UserManager: Generate the token (Prevents stringToEscape error)
            _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
                .ReturnsAsync(mockToken);

            //Mock Configuration: Provide the BaseUrl
            _mockConfig.Setup(x => x["App:BaseUrl"])
                .Returns(baseUrl);

            await _authService.ResendConfirmationEmailAsync(email);

            _mockEmailService.Verify(x => x.SendEmailAsync(
                user.Email,
                It.Is<string>(s => s.Contains("Confirm your LoanIt account")),
                It.Is<string>(body => body.Contains(mockToken))
            ), Times.Once);
        }




    }
}
