using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _controller = new UserController(_userServiceMock.Object);
        }

        private void SetUser(string userId, string role = "User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        private static UserDTO.UserProfileDTO MakeProfile(string id = "user-1")
        {
            return new UserDTO.UserProfileDTO
            {
                Id = id,
                FullName = "Test User",
                Username = "testuser",
                Email = "test@test.com",
                Score = 10,
                UnpaidFinesTotal = 0,
                BorrowingStatus = "Free",
                CreatedAt = DateTime.UtcNow
            };
        }

        // ---------------- GET PROFILE ----------------

        [Fact]
        public async Task GetProfile_ReturnsOk()
        {
            SetUser("user-1");

            var profile = MakeProfile();

            _userServiceMock
                .Setup(x => x.GetProfileAsync("user-1"))
                .ReturnsAsync(profile);

            var result = await _controller.GetProfile();

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<UserDTO.UserProfileDTO>(ok.Value);

            Assert.Equal("user-1", data.Id);
        }

        [Fact]
        public async Task GetProfile_UserNotFound_ReturnsNotFound()
        {
            SetUser("user-1");

            _userServiceMock
                .Setup(x => x.GetProfileAsync("user-1"))
                .ThrowsAsync(new KeyNotFoundException("User not found"));

            var result = await _controller.GetProfile();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ---------------- PUBLIC PROFILE ----------------

        [Fact]
        public async Task GetPublicProfile_ReturnsOk()
        {
            var profile = new UserDTO.UserSummaryDTO
            {
                Id = "user-2",
                Username = "user2",
                FullName = "Test User",
                Score = 10,
                IsVerified = true
            };

            _userServiceMock
                .Setup(x => x.GetPublicProfileAsync("user-2"))
                .ReturnsAsync(profile);

            var result = await _controller.GetPublicProfile("user-2");

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<UserDTO.UserSummaryDTO>(ok.Value);

            Assert.Equal("user-2", data.Id);
        }

        [Fact]
        public async Task GetPublicProfile_NotFound_Returns404()
        {
            _userServiceMock
                .Setup(x => x.GetPublicProfileAsync("user-2"))
                .ThrowsAsync(new KeyNotFoundException());

            var result = await _controller.GetPublicProfile("user-2");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ---------------- UPDATE PROFILE ----------------

        [Fact]
        public async Task UpdateProfile_ReturnsUpdatedProfile()
        {
            SetUser("user-1");

            var dto = new UserDTO.UpdateProfileDTO
            {
                FullName = "Updated User"
            };

            var updated = MakeProfile("user-1");

            _userServiceMock
                .Setup(x => x.UpdateProfileAsync("user-1", dto))
                .ReturnsAsync(updated);

            var result = await _controller.UpdateProfile(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<UserDTO.UserProfileDTO>(ok.Value);

            Assert.Equal("user-1", data.Id);
        }

        [Fact]
        public async Task UpdateProfile_InvalidData_ReturnsBadRequest()
        {
            SetUser("user-1");

            var dto = new UserDTO.UpdateProfileDTO();

            _userServiceMock
                .Setup(x => x.UpdateProfileAsync("user-1", dto))
                .ThrowsAsync(new ArgumentException("Invalid"));

            var result = await _controller.UpdateProfile(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ---------------- DELETE ACCOUNT ----------------

        [Fact]
        public async Task DeleteAccount_ReturnsOk()
        {
            SetUser("user-1");

            var dto = new UserDTO.DeleteAccountDTO
            {
                Password = "123456"
            };

            var result = await _controller.DeleteAccount(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
        }

        // ---------------- SCORE HISTORY ----------------

        [Fact]
        public async Task GetScoreHistory_ReturnsHistory()
        {
            SetUser("user-1");

            var history = new List<UserDTO.ScoreHistoryDTO>
            {
                new UserDTO.ScoreHistoryDTO
                {
                    Id = 1,
                    PointsChanged = 5,
                    Reason = "OnTimeReturn"
                }
            };

            _userServiceMock
                .Setup(x => x.GetScoreHistoryAsync("user-1"))
                .ReturnsAsync(history);

            var result = await _controller.GetScoreHistory();

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<UserDTO.ScoreHistoryDTO>>(ok.Value);

            Assert.Single(data);
        }

        // ---------------- ADMIN ----------------

        [Fact]
        public async Task GetAllUsers_Admin_ReturnsUsers()
        {
            SetUser("admin-1", "Admin");

            var users = new List<UserDTO.AdminUserDTO>
            {
                new UserDTO.AdminUserDTO { Id = "user-1", Username = "test" }
            };

            _userServiceMock
                .Setup(x => x.GetAllUsersAsync())
                .ReturnsAsync(users);

            var result = await _controller.GetAllUsers();

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<UserDTO.AdminUserDTO>>(ok.Value);

            Assert.Single(data);
        }

        [Fact]
        public async Task GetUserById_Admin_ReturnsUser()
        {
            SetUser("admin-1", "Admin");

            var user = new UserDTO.AdminUserDetailDTO
            {
                Id = "user-2",
                Username = "user2",
                Email = "user@test.com",
                Score = 20
            };

            _userServiceMock
                .Setup(x => x.GetUserByIdAsync("user-2"))
                .ReturnsAsync(user);

            var result = await _controller.GetUserById("user-2");

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<UserDTO.AdminUserDetailDTO>(ok.Value);

            Assert.Equal("user-2", data.Id);
        }
    }
}