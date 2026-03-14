using backend.Controllers;
using backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using static backend.DTOs.ChatDTO;

namespace backend.Tests.Controllers
{
    public class UserBlockControllerTests
    {
        private readonly Mock<IUserBlockService> _userBlockServiceMock;
        private readonly UserBlockController _controller;

        public UserBlockControllerTests()
        {
            _userBlockServiceMock = new Mock<IUserBlockService>();
            _controller = new UserBlockController(_userBlockServiceMock.Object);
            SetUser("user-1");
        }
        private void SetUser(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private static UserBlockDTO.BlockResponseDTO MakeBlockResponse(
            string blockerId = "user-1",
            string blockedId = "user-2") => new()
            {
                BlockerId = blockerId,
                BlockedId = blockedId,
                BlockedUserName = "Blocked User",
                CreatedAt = DateTime.UtcNow
            };

        [Fact]
        public async Task GetBlockedUsers_ReturnsOk_WithBlockedUsers()
        {
            var blocks = new List<UserBlockDTO.BlockResponseDTO>
            {
                MakeBlockResponse("user-1", "user-2"),
                MakeBlockResponse("user-1", "user-3")
            };
            _userBlockServiceMock
                .Setup(s => s.GetBlockedUsersAsync("user-1"))
                .ReturnsAsync(blocks);

            var result = await _controller.GetBlockedUsers();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<UserBlockDTO.BlockResponseDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetBlockedUsers_ReturnsOk_WithEmptyList()
        {
            _userBlockServiceMock
                .Setup(s => s.GetBlockedUsersAsync("user-1"))
                .ReturnsAsync(new List<UserBlockDTO.BlockResponseDTO>());

            var result = await _controller.GetBlockedUsers();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Empty((List<UserBlockDTO.BlockResponseDTO>)ok.Value!);
        }

        [Fact]
        public async Task GetBlockedUsers_CallsServiceWithCorrectUserId()
        {
            SetUser("specific-user");
            _userBlockServiceMock
                .Setup(s => s.GetBlockedUsersAsync("specific-user"))
                .ReturnsAsync(new List<UserBlockDTO.BlockResponseDTO>());

            await _controller.GetBlockedUsers();

            _userBlockServiceMock.Verify(s =>
                s.GetBlockedUsersAsync("specific-user"), Times.Once);
        }

        [Fact]
        public async Task GetBlockedUsers_ServiceThrows_ExceptionPropagates()
        {
            _userBlockServiceMock
                .Setup(s => s.GetBlockedUsersAsync("user-1"))
                .ThrowsAsync(new InvalidOperationException("Service error."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.GetBlockedUsers());
        }

        [Fact]
        public async Task Block_ReturnsOk_WithBlockResponse()
        {
            var dto = new UserBlockDTO.BlockUserDTO { BlockedUserId = "user-2" };
            var response = MakeBlockResponse();
            _userBlockServiceMock
                .Setup(s => s.BlockAsync("user-1", "user-2"))
                .ReturnsAsync(response);

            var result = await _controller.Block(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<UserBlockDTO.BlockResponseDTO>(ok.Value);
            Assert.Equal("user-1", returned.BlockerId);
            Assert.Equal("user-2", returned.BlockedId);
        }

        [Fact]
        public async Task Block_CallsServiceWithCorrectArguments()
        {
            var dto = new UserBlockDTO.BlockUserDTO { BlockedUserId = "user-3" };
            _userBlockServiceMock
                .Setup(s => s.BlockAsync("user-1", "user-3"))
                .ReturnsAsync(MakeBlockResponse("user-1", "user-3"));

            await _controller.Block(dto);

            _userBlockServiceMock.Verify(s =>
                s.BlockAsync("user-1", "user-3"), Times.Once);
        }

        [Fact]
        public async Task Block_ServiceThrows_InvalidOperation_ExceptionPropagates()
        {
            var dto = new UserBlockDTO.BlockUserDTO { BlockedUserId = "user-1" };
            _userBlockServiceMock
                .Setup(s => s.BlockAsync("user-1", "user-1"))
                .ThrowsAsync(new InvalidOperationException("You cannot block yourself."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.Block(dto));
        }

        [Fact]
        public async Task Block_ServiceThrows_KeyNotFound_ExceptionPropagates()
        {
            var dto = new UserBlockDTO.BlockUserDTO { BlockedUserId = "nonexistent" };
            _userBlockServiceMock
                .Setup(s => s.BlockAsync("user-1", "nonexistent"))
                .ThrowsAsync(new KeyNotFoundException("User not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.Block(dto));
        }

        [Fact]
        public async Task Block_AlreadyBlocked_ServiceThrows_ExceptionPropagates()
        {
            var dto = new UserBlockDTO.BlockUserDTO { BlockedUserId = "user-2" };
            _userBlockServiceMock
                .Setup(s => s.BlockAsync("user-1", "user-2"))
                .ThrowsAsync(new InvalidOperationException("User is already blocked."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.Block(dto));
        }

        [Fact]
        public async Task Unblock_ReturnsNoContent()
        {
            _userBlockServiceMock
                .Setup(s => s.UnblockAsync("user-1", "user-2"))
                .Returns(Task.CompletedTask);

            var result = await _controller.Unblock("user-2");

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Unblock_CallsServiceWithCorrectArguments()
        {
            SetUser("specific-user");
            _userBlockServiceMock
                .Setup(s => s.UnblockAsync("specific-user", "user-3"))
                .Returns(Task.CompletedTask);

            await _controller.Unblock("user-3");

            _userBlockServiceMock.Verify(s =>
                s.UnblockAsync("specific-user", "user-3"), Times.Once);
        }

        [Fact]
        public async Task Unblock_ServiceThrows_KeyNotFound_ExceptionPropagates()
        {
            _userBlockServiceMock
                .Setup(s => s.UnblockAsync("user-1", "nonexistent"))
                .ThrowsAsync(new KeyNotFoundException("Block record not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.Unblock("nonexistent"));
        }

        [Fact]
        public async Task Unblock_ServiceThrows_InvalidOperation_ExceptionPropagates()
        {
            _userBlockServiceMock
                .Setup(s => s.UnblockAsync("user-1", "user-2"))
                .ThrowsAsync(new InvalidOperationException("This user is not blocked."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.Unblock("user-2"));
        }
    }
}