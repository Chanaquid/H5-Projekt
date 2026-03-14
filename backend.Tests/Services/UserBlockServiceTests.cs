using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace backend.Tests.Services
{
    public class UserBlockServiceTests
    {
        private readonly Mock<IUserBlockRepository> _repoMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly UserBlockService _service;

        public UserBlockServiceTests()
        {
            _repoMock = new Mock<IUserBlockRepository>();

            //UserManager requires a mock store as minimum constructor arg
            var storeMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                storeMock.Object, null, null, null, null, null, null, null, null);

            _service = new UserBlockService(_repoMock.Object, _userManagerMock.Object);
        }

        //Helpers
        private static ApplicationUser MakeUser(string id, string fullName = "Test User") => new()
        {
            Id = id,
            FullName = fullName,
            AvatarUrl = null
        };

        private static UserBlock MakeBlock(string blockerId, string blockedId) => new()
        {
            BlockerId = blockerId,
            BlockedId = blockedId,
            CreatedAt = DateTime.UtcNow,
            Blocked = MakeUser(blockedId)
        };


        [Fact]
        public async Task BlockAsync_ValidBlock_ReturnsDTO()
        {
            var target = MakeUser("blocked-1", "Blocked User");
            _userManagerMock.Setup(m => m.FindByIdAsync("blocked-1")).ReturnsAsync(target);
            _userManagerMock.Setup(m => m.IsInRoleAsync(target, "Admin")).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetAsync("blocker-1", "blocked-1")).ReturnsAsync((UserBlock?)null);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<UserBlock>())).Returns(Task.CompletedTask);

            var result = await _service.BlockAsync("blocker-1", "blocked-1");

            result.Should().NotBeNull();
            result.BlockerId.Should().Be("blocker-1");
            result.BlockedId.Should().Be("blocked-1");
            result.BlockedUserName.Should().Be("Blocked User");
            _repoMock.Verify(r => r.AddAsync(It.IsAny<UserBlock>()), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task BlockAsync_SelfBlock_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.BlockAsync("user-1", "user-1"));
        }

        [Fact]
        public async Task BlockAsync_TargetNotFound_ThrowsKeyNotFoundException()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync("ghost")).ReturnsAsync((ApplicationUser?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.BlockAsync("blocker-1", "ghost"));
        }

        [Fact]
        public async Task BlockAsync_BlockingAdmin_ThrowsInvalidOperationException()
        {
            var admin = MakeUser("admin-1");
            _userManagerMock.Setup(m => m.FindByIdAsync("admin-1")).ReturnsAsync(admin);
            _userManagerMock.Setup(m => m.IsInRoleAsync(admin, "Admin")).ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.BlockAsync("blocker-1", "admin-1"));
        }

        [Fact]
        public async Task BlockAsync_AlreadyBlocked_ThrowsInvalidOperationException()
        {
            var target = MakeUser("blocked-1");
            _userManagerMock.Setup(m => m.FindByIdAsync("blocked-1")).ReturnsAsync(target);
            _userManagerMock.Setup(m => m.IsInRoleAsync(target, "Admin")).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetAsync("blocker-1", "blocked-1"))
                .ReturnsAsync(MakeBlock("blocker-1", "blocked-1"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.BlockAsync("blocker-1", "blocked-1"));
        }

        [Fact]
        public async Task BlockAsync_DoesNotCallAdd_WhenAlreadyBlocked()
        {
            var target = MakeUser("blocked-1");
            _userManagerMock.Setup(m => m.FindByIdAsync("blocked-1")).ReturnsAsync(target);
            _userManagerMock.Setup(m => m.IsInRoleAsync(target, "Admin")).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetAsync("blocker-1", "blocked-1"))
                .ReturnsAsync(MakeBlock("blocker-1", "blocked-1"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.BlockAsync("blocker-1", "blocked-1"));

            _repoMock.Verify(r => r.AddAsync(It.IsAny<UserBlock>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task BlockAsync_MapsFieldsCorrectly()
        {
            var target = MakeUser("blocked-1", "Jane Doe");
            target.AvatarUrl = "https://example.com/avatar.jpg";

            _userManagerMock.Setup(m => m.FindByIdAsync("blocked-1")).ReturnsAsync(target);
            _userManagerMock.Setup(m => m.IsInRoleAsync(target, "Admin")).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetAsync("blocker-1", "blocked-1")).ReturnsAsync((UserBlock?)null);
            _repoMock.Setup(r => r.AddAsync(It.IsAny<UserBlock>())).Returns(Task.CompletedTask);

            var result = await _service.BlockAsync("blocker-1", "blocked-1");

            result.BlockedUserName.Should().Be("Jane Doe");
            result.BlockedUserAvatarUrl.Should().Be("https://example.com/avatar.jpg");
        }


        [Fact]
        public async Task UnblockAsync_ExistingBlock_RemovesAndSaves()
        {
            var block = MakeBlock("blocker-1", "blocked-1");
            _repoMock.Setup(r => r.GetAsync("blocker-1", "blocked-1")).ReturnsAsync(block);

            await _service.UnblockAsync("blocker-1", "blocked-1");

            _repoMock.Verify(r => r.RemoveAsync(block), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UnblockAsync_BlockNotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetAsync("blocker-1", "blocked-1"))
                .ReturnsAsync((UserBlock?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UnblockAsync("blocker-1", "blocked-1"));
        }

        [Fact]
        public async Task UnblockAsync_BlockNotFound_DoesNotCallRemove()
        {
            _repoMock.Setup(r => r.GetAsync("blocker-1", "blocked-1"))
                .ReturnsAsync((UserBlock?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UnblockAsync("blocker-1", "blocked-1"));

            _repoMock.Verify(r => r.RemoveAsync(It.IsAny<UserBlock>()), Times.Never);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }


        [Fact]
        public async Task GetBlockedUsersAsync_ReturnsMappedList()
        {
            var blocks = new List<UserBlock>
            {
                MakeBlock("user-1", "blocked-1"),
                MakeBlock("user-1", "blocked-2")
            };
            _repoMock.Setup(r => r.GetBlocksByUserIdAsync("user-1")).ReturnsAsync(blocks);

            var result = await _service.GetBlockedUsersAsync("user-1");

            result.Should().HaveCount(2);
            result.All(b => b.BlockerId == "user-1").Should().BeTrue();
        }

        [Fact]
        public async Task GetBlockedUsersAsync_NoBlocks_ReturnsEmptyList()
        {
            _repoMock.Setup(r => r.GetBlocksByUserIdAsync("user-1"))
                .ReturnsAsync(new List<UserBlock>());

            var result = await _service.GetBlockedUsersAsync("user-1");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetBlockedUsersAsync_MapsBlockedUserFieldsCorrectly()
        {
            var blocked = MakeUser("blocked-1", "Blocked Person");
            blocked.AvatarUrl = "https://example.com/pic.jpg";

            var block = new UserBlock
            {
                BlockerId = "user-1",
                BlockedId = "blocked-1",
                Blocked = blocked,
                CreatedAt = new DateTime(2025, 1, 1)
            };

            _repoMock.Setup(r => r.GetBlocksByUserIdAsync("user-1"))
                .ReturnsAsync(new List<UserBlock> { block });

            var result = await _service.GetBlockedUsersAsync("user-1");

            var dto = result.Single();
            dto.BlockedId.Should().Be("blocked-1");
            dto.BlockedUserName.Should().Be("Blocked Person");
            dto.BlockedUserAvatarUrl.Should().Be("https://example.com/pic.jpg");
            dto.CreatedAt.Should().Be(new DateTime(2025, 1, 1));
        }
    }


}

