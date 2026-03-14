using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace backend.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly AuthController _AuthController;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _AuthController = new AuthController(_mockAuthService.Object);
        }


        [Fact]
        public async Task Register_ValidDTO_ReturnsOk()
        {
            var dto = new AuthDTO.RegisterDTO { Email = "test@gmail.com", Password = "Password1234" };
            var expectedResult = new AuthDTO.AuthResponseDTO { Token = "jwt token", RefreshToken = "regreshtoken" };


            _mockAuthService.Setup(x => x.RegisterAsync(dto)).ReturnsAsync(expectedResult);
            

            var result = await _AuthController.Register(dto);


            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
        }


        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            var dto = new AuthDTO.RegisterDTO { Email = "test@gmail.com", Password = "Password1234" };


            _mockAuthService.Setup(x => x.RegisterAsync(dto))
                .ThrowsAsync(new ArgumentException("An account with this email already exists."));


            var result = await _AuthController.Register(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);



        }


        [Fact]
        public async Task ConfirmEmail_ValidToken_ReturnsOk()
        {

            var dto = new { userId = "test@123", token = "jwtToken" };

            _mockAuthService.Setup(x => x.ConfirmEmailAsync(dto.userId, dto.token))
                .ReturnsAsync(true);
        

            var result = await _AuthController.ConfirmEmail(dto.userId, dto.token);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiDTO.ApiResponse>(okResult.Value);

            Assert.Equal("Email confirmed. You can now log in.", response.Message);


        
        }


        [Fact]
        public async Task ConfirmEmail_InvalidToken_ReturnsBadRequest()
        {
            _mockAuthService.Setup(x => x.ConfirmEmailAsync("testgmail", "invalidtoken"))
                .ReturnsAsync(false);


            var result = await _AuthController.ConfirmEmail("testgmail", "invalidtoken");


            var badreq = Assert.IsType<BadRequestObjectResult>(result);

            var response = Assert.IsType<ApiDTO.ApiResponse>(badreq.Value);

            Assert.Equal("Email confirmation failed. The link may have expired.", response.Message);
        }


        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            var dto = new AuthDTO.LoginDTO { Email = "test@gmail.com", Password = "password1234" };
            var expectedResult = new AuthDTO.AuthResponseDTO { Token = "asd", RefreshToken = "asdwd" };
            _mockAuthService.Setup(x => x.LoginAsync(dto)).ReturnsAsync(expectedResult);


            var result = await _AuthController.Login(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Equal(expectedResult, okResult.Value);
        }


        [Fact]
        public async Task Login_InvalidCredentials_ReturnsBadRequest()
        {
            var dto = new AuthDTO.LoginDTO { Email = "test@gmail.com", Password = "testpass123" };

            _mockAuthService.Setup(x => x.LoginAsync(dto))
                .ThrowsAsync(new ArgumentException("Invalid email or password."));


            var result = await _AuthController.Login(dto);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiDTO.ApiResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid email or password.", response.Message);




        }


        [Fact]
        public async Task Refresh_ValidToken_ReturnsOk()
        {
            var dto = new AuthDTO.RefreshTokenDTO { RefreshToken = "refreshToken" };
            var expectedResult = new AuthDTO.AuthResponseDTO { Token="token", UserId = "123455", RefreshToken = "refreshToken" };

            _mockAuthService.Setup(x => x.RefreshTokenAsync(dto)).ReturnsAsync(expectedResult);


            var result = await _AuthController.Refresh(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
        }


        [Fact]
        public async Task Refresh_InvalidToken_ReturnsBadRequest()
        {
            var dto = new AuthDTO.RefreshTokenDTO { RefreshToken = "token1234" };

            var errorMessage = "Invalid refresh token.";

            _mockAuthService.Setup(x => x.RefreshTokenAsync(dto))
                .ThrowsAsync(new UnauthorizedAccessException(errorMessage));

            var result = await _AuthController.Refresh(dto);


            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiDTO.ApiResponse>(unauthorizedResult.Value);

            Assert.Equal(errorMessage, response.Message);


        }


        [Fact]
        public async Task Logout_AuthenticatedUser_ReturnsOk()
        {
            var userId = "test-userId";
            
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) }; 
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);


            _AuthController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal } 
            };

            //Mock the logout service
            _mockAuthService.Setup(x => x.LogoutAsync(userId))
                .Returns(Task.CompletedTask);

            var result = await _AuthController.Logout();


            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiDTO.ApiResponse>(okResult.Value);
            Assert.Equal("Logged out successfully.", response.Message);

            _mockAuthService.Verify(x => x.LogoutAsync(userId), Times.Once);




        }


        [Fact]
        public async Task Logout_UnauthenticatedUser_ReturnsUnauthorized()
        {
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
            _AuthController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _AuthController.Logout();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = Assert.IsType<ApiDTO.ApiResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token.", response.Message);

            // Verify LogoutAsync was never called
            _mockAuthService.Verify(x => x.LogoutAsync(It.IsAny<string>()), Times.Never);
        }


        [Fact]
        public async Task ForgotPassword_AnyEmail_AlwaysReturnsOk()
        {
            var dto = new AuthDTO.ForgotPasswordDTO { Email = "test@gmail.com" };

            _mockAuthService.Setup(x => x.ForgotPasswordAsync(dto.Email))
                .ReturnsAsync(true);

            var result = await _AuthController.ForgotPassword(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiDTO.ApiResponse>(okResult.Value);


            Assert.Equal("If an account with that email exists, a reset link has been sent." , response.Message.ToString());

            _mockAuthService.Verify(x => x.ForgotPasswordAsync(dto.Email), Times.Once);
        }


        [Fact]
        public async Task ForgotPassword_NonexistantEmail_AlwaysReturnsOk()
        {
            var dto = new AuthDTO.ForgotPasswordDTO { Email = "testmail" };


            _mockAuthService.Setup(z => z.ForgotPasswordAsync(dto.Email))
                .ReturnsAsync(true);


            var result = await _AuthController.ForgotPassword(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiDTO.ApiResponse>(okResult.Value);


            Assert.Equal("If an account with that email exists, a reset link has been sent.", response.Message);


            _mockAuthService.Verify(x => x.ForgotPasswordAsync(dto.Email), Times.Once);


        }


        [Fact]
        public async Task ResetPassword_ValidToken_ReturnsOk()
        {
            var dto = new AuthDTO.ResetPasswordDTO { Email = "testemail@gmail.com", NewPassword = "test12345", Token = "validToken" };
            var expectedResult = new ApiDTO.ApiResponse { Message = "Password reset successfully. You can now log in." };


            _mockAuthService.Setup(s => s.ResetPasswordAsync(dto))
                .ReturnsAsync(true);

            
            var result = await _AuthController.ResetPassword(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiDTO.ApiResponse>(okResult.Value);


            Assert.Equal(expectedResult.Message, response.Message);                          
        
        }


        [Fact]
        public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
        {
            var dto = new AuthDTO.ResetPasswordDTO { Email = "testemail@gmail.com", NewPassword = "test12345", Token = "validToken" };

            _mockAuthService.Setup(x => x.ResetPasswordAsync(dto))
                .ReturnsAsync(false);


            var result = await _AuthController.ResetPassword(dto);


            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiDTO.ApiResponse>(badResult.Value);

            Assert.Equal("Password reset failed. The link may have expired.", response.Message);             
        }


        [Fact]
        public async Task ResendConfirmation_CorrectEmail_AlwaysReturnOk()
        {
            var dto = new AuthDTO.ForgotPasswordDTO { Email = "test@gmail.com" };


            _mockAuthService.Setup(x => x.ResendConfirmationEmailAsync(dto.Email))
                .Returns(Task.CompletedTask);


            var result = await _AuthController.ResendConfirmation(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiDTO.ApiResponse>(okResult.Value);

            Assert.Equal("If your email is registered and unconfirmed, a new confirmation link has been sent.", 
                response.Message);

            _mockAuthService.Verify(x => x.ResendConfirmationEmailAsync(dto.Email), Times.Once);


        }


    }
}
