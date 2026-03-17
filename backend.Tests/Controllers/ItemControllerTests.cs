using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class ItemControllerTests
    {
        private readonly Mock<IItemService> _itemServiceMock;
        private readonly Mock<IUserRecentlyViewedService> _recentlyViewedServiceMock;
        private readonly ItemController _controller;

        public ItemControllerTests()
        {
            _itemServiceMock = new Mock<IItemService>();
            _recentlyViewedServiceMock = new Mock<IUserRecentlyViewedService>();
            _controller = new ItemController(_itemServiceMock.Object, _recentlyViewedServiceMock.Object);
            SetUser("user-1", "User");
        }

        private void SetUser(string? userId, string role = "User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, role)
            };
            if (userId != null)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private void SetAnonymousUser()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };
        }

        private static ItemDTO.ItemDetailDTO MakeItemDetail(int id = 1) => new()
        {
            Id = id,
            Title = "Drill",
            Description = "A good drill",
            Condition = "Good",
            Status = "Approved",
            IsActive = true,
            PickupAddress = "Test Address",
            AvailableFrom = DateTime.UtcNow.Date,
            AvailableUntil = DateTime.UtcNow.Date.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            Owner = new UserDTO.UserSummaryDTO { Id = "owner-1", FullName = "Owner" },
            Category = new CategoryDTO.CategoryResponseDTO { Id = 1, Name = "Tools" }
        };

        private static ItemDTO.ItemSummaryDTO MakeItemSummary(int id = 1) => new()
        {
            Id = id,
            Title = "Drill",
            Condition = "Good",
            Status = "Approved",
            CategoryName = "Tools",
            OwnerName = "Owner"
        };

        private static ItemDTO.ItemQrCodeDTO MakeQrCode(int itemId = 1) => new()
        {
            ItemId = itemId,
            QrCode = "TESTQR123456"
        };

        [Fact]
        public async Task GetAll_ReturnsOk_WithItems()
        {
            SetAnonymousUser();
            var items = new List<ItemDTO.ItemSummaryDTO>
            {
                MakeItemSummary(1),
                MakeItemSummary(2)
            };
            _itemServiceMock
                .Setup(s => s.GetAllApprovedAsync())
                .ReturnsAsync(items);

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<ItemDTO.ItemSummaryDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithEmptyList()
        {
            _itemServiceMock
                .Setup(s => s.GetAllApprovedAsync())
                .ReturnsAsync(new List<ItemDTO.ItemSummaryDTO>());

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Empty((List<ItemDTO.ItemSummaryDTO>)ok.Value!);
        }

        [Fact]
        public async Task GetAllAdmin_ReturnsOk_WithAllItems()
        {
            SetUser("admin-1", "Admin");
            var items = new List<ItemDTO.ItemSummaryDTO>
            {
                MakeItemSummary(1),
                MakeItemSummary(2)
            };
            _itemServiceMock
                .Setup(s => s.GetAllForAdminAsync(false))
                .ReturnsAsync(items);

            var result = await _controller.GetAllAdmin(false);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<ItemDTO.ItemSummaryDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetAllAdmin_IncludeInactive_PassesCorrectFlag()
        {
            SetUser("admin-1", "Admin");
            _itemServiceMock
                .Setup(s => s.GetAllForAdminAsync(true))
                .ReturnsAsync(new List<ItemDTO.ItemSummaryDTO>());

            await _controller.GetAllAdmin(true);

            _itemServiceMock.Verify(s => s.GetAllForAdminAsync(true), Times.Once);
        }

        [Fact]
        public async Task GetById_AuthenticatedUser_ReturnsOk_AndTracksView()
        {
            SetUser("user-1", "User");
            var item = MakeItemDetail(1);
            _itemServiceMock
                .Setup(s => s.GetByIdAsync(1, "user-1", false))
                .ReturnsAsync(item);
            _recentlyViewedServiceMock
                .Setup(s => s.TrackViewAsync("user-1", 1))
                .Returns(Task.CompletedTask);

            var result = await _controller.GetById(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ItemDTO.ItemDetailDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
            _recentlyViewedServiceMock.Verify(s => s.TrackViewAsync("user-1", 1), Times.Once);
        }

        [Fact]
        public async Task GetById_AnonymousUser_ReturnsOk_DoesNotTrackView()
        {
            SetAnonymousUser();
            var item = MakeItemDetail(1);
            _itemServiceMock
                .Setup(s => s.GetByIdAsync(1, null, false))
                .ReturnsAsync(item);

            var result = await _controller.GetById(1);

            Assert.IsType<OkObjectResult>(result);
            _recentlyViewedServiceMock.Verify(s =>
                s.TrackViewAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetById_AdminUser_ReturnsOk_DoesNotTrackView()
        {
            SetUser("admin-1", "Admin");
            _itemServiceMock
                .Setup(s => s.GetByIdAsync(1, "admin-1", true))
                .ReturnsAsync(MakeItemDetail(1));

            var result = await _controller.GetById(1);

            Assert.IsType<OkObjectResult>(result);
            _recentlyViewedServiceMock.Verify(s =>
                s.TrackViewAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetById_ServiceThrows_ExceptionPropagates()
        {
            _itemServiceMock
                .Setup(s => s.GetByIdAsync(999, "user-1", false))
                .ThrowsAsync(new KeyNotFoundException("Item 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.GetById(999));
        }

        [Fact]
        public async Task GetMyItems_ReturnsOk_WithOwnerItems()
        {
            var items = new List<ItemDTO.ItemSummaryDTO>
            {
                MakeItemSummary(1),
                MakeItemSummary(2)
            };
            _itemServiceMock
                .Setup(s => s.GetByOwnerAsync("user-1"))
                .ReturnsAsync(items);

            var result = await _controller.GetMyItems();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<ItemDTO.ItemSummaryDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetMyItems_CallsServiceWithCorrectUserId()
        {
            SetUser("specific-user");
            _itemServiceMock
                .Setup(s => s.GetByOwnerAsync("specific-user"))
                .ReturnsAsync(new List<ItemDTO.ItemSummaryDTO>());

            await _controller.GetMyItems();

            _itemServiceMock.Verify(s => s.GetByOwnerAsync("specific-user"), Times.Once);
        }

        [Fact]
        public async Task GetByUser_ReturnsOk_WithUserItems()
        {
            SetUser("admin-1", "Admin");
            var items = new List<ItemDTO.ItemSummaryDTO> { MakeItemSummary(1) };
            _itemServiceMock
                .Setup(s => s.GetByOwnerAsync("target-user"))
                .ReturnsAsync(items);

            var result = await _controller.GetByUser("target-user");

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<ItemDTO.ItemSummaryDTO>>(ok.Value);
            Assert.Single(returned);
        }

        [Fact]
        public async Task GetByUser_CallsServiceWithCorrectUserId()
        {
            SetUser("admin-1", "Admin");
            _itemServiceMock
                .Setup(s => s.GetByOwnerAsync("target-user"))
                .ReturnsAsync(new List<ItemDTO.ItemSummaryDTO>());

            await _controller.GetByUser("target-user");

            _itemServiceMock.Verify(s => s.GetByOwnerAsync("target-user"), Times.Once);
        }


        [Fact]
        public async Task GetByUserPublic_ReturnsOk_WithPublicItems()
        {
            SetAnonymousUser();
            var items = new List<ItemDTO.ItemSummaryDTO> { MakeItemSummary(1) };
            _itemServiceMock
                .Setup(s => s.GetPublicByOwnerAsync("owner-1"))
                .ReturnsAsync(items);

            var result = await _controller.GetByUserPublic("owner-1");

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<ItemDTO.ItemSummaryDTO>>(ok.Value);
            Assert.Single(returned);
        }

        [Fact]
        public async Task GetNearby_ReturnsOk_WithNearbyItems()
        {
            SetAnonymousUser();
            var items = new List<ItemDTO.ItemSummaryDTO> { MakeItemSummary(1) };
            _itemServiceMock
                .Setup(s => s.GetNearbyAsync(55.6761, 12.5683, 10))
                .ReturnsAsync(items);

            var result = await _controller.GetNearby(55.6761, 12.5683, 10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<ItemDTO.ItemSummaryDTO>>(ok.Value);
            Assert.Single(returned);
        }

        [Fact]
        public async Task GetNearby_CallsServiceWithCorrectArguments()
        {
            _itemServiceMock
                .Setup(s => s.GetNearbyAsync(55.0, 12.0, 5))
                .ReturnsAsync(new List<ItemDTO.ItemSummaryDTO>());

            await _controller.GetNearby(55.0, 12.0, 5);

            _itemServiceMock.Verify(s => s.GetNearbyAsync(55.0, 12.0, 5), Times.Once);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtAction_WithItem()
        {
            var dto = new ItemDTO.CreateItemDTO
            {
                Title = "Drill",
                CategoryId = 1,
                AvailableFrom = DateTime.UtcNow.AddDays(1),
                AvailableUntil = DateTime.UtcNow.AddDays(30)
            };
            var detail = MakeItemDetail(1);
            _itemServiceMock
                .Setup(s => s.CreateAsync("user-1", dto, false))
                .ReturnsAsync(detail);

            var result = await _controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetById), created.ActionName);
            Assert.Equal(1, ((ItemDTO.ItemDetailDTO)created.Value!).Id);
        }

        [Fact]
        public async Task Create_AsAdmin_PassesIsAdminTrue()
        {
            SetUser("admin-1", "Admin");
            var dto = new ItemDTO.CreateItemDTO { Title = "Drill", CategoryId = 1 };
            _itemServiceMock
                .Setup(s => s.CreateAsync("admin-1", dto, true))
                .ReturnsAsync(MakeItemDetail());

            await _controller.Create(dto);

            _itemServiceMock.Verify(s => s.CreateAsync("admin-1", dto, true), Times.Once);
        }

        [Fact]
        public async Task Create_ServiceThrows_ExceptionPropagates()
        {
            var dto = new ItemDTO.CreateItemDTO { CategoryId = 999 };
            _itemServiceMock
                .Setup(s => s.CreateAsync("user-1", dto, false))
                .ThrowsAsync(new ArgumentException("Category does not exist."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.Create(dto));
        }

        [Fact]
        public async Task Update_ReturnsOk_WithUpdatedItem()
        {
            var dto = new ItemDTO.UpdateItemDTO { Title = "Updated Drill", IsActive = true };
            var detail = MakeItemDetail(1);
            _itemServiceMock
                .Setup(s => s.UpdateAsync(1, "user-1", dto, false))
                .ReturnsAsync(detail);

            var result = await _controller.Update(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<ItemDTO.ItemDetailDTO>(ok.Value);
        }

        [Fact]
        public async Task Update_CallsServiceWithCorrectArguments()
        {
            var dto = new ItemDTO.UpdateItemDTO { Title = "New Title", IsActive = true };
            _itemServiceMock
                .Setup(s => s.UpdateAsync(2, "user-1", dto, false))
                .ReturnsAsync(MakeItemDetail());

            await _controller.Update(2, dto);

            _itemServiceMock.Verify(s => s.UpdateAsync(2, "user-1", dto, false), Times.Once);
        }

        [Fact]
        public async Task Update_ServiceThrows_ExceptionPropagates()
        {
            var dto = new ItemDTO.UpdateItemDTO { Title = "Title", IsActive = true };
            _itemServiceMock
                .Setup(s => s.UpdateAsync(999, "user-1", dto, false))
                .ThrowsAsync(new KeyNotFoundException("Item 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Update(999, dto));
        }

        [Fact]
        public async Task UpdateStatus_ReturnsOk_WithUpdatedItem()
        {
            SetUser("admin-1", "Admin");
            var dto = new ItemDTO.AdminItemStatusDTO { Status = "Approved" };
            _itemServiceMock
                .Setup(s => s.UpdateStatusAsync(1, dto))
                .ReturnsAsync(MakeItemDetail());

            var result = await _controller.UpdateStatus(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdateStatus_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            var dto = new ItemDTO.AdminItemStatusDTO { Status = "Rejected", AdminNote = "Low quality." };
            _itemServiceMock
                .Setup(s => s.UpdateStatusAsync(3, dto))
                .ReturnsAsync(MakeItemDetail());

            await _controller.UpdateStatus(3, dto);

            _itemServiceMock.Verify(s => s.UpdateStatusAsync(3, dto), Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _itemServiceMock
                .Setup(s => s.DeleteAsync(1, "user-1", false))
                .Returns(Task.CompletedTask);

            var result = await _controller.Delete(1);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_CallsServiceWithCorrectArguments()
        {
            _itemServiceMock
                .Setup(s => s.DeleteAsync(2, "user-1", false))
                .Returns(Task.CompletedTask);

            await _controller.Delete(2);

            _itemServiceMock.Verify(s => s.DeleteAsync(2, "user-1", false), Times.Once);
        }

        [Fact]
        public async Task Delete_ServiceThrows_ExceptionPropagates()
        {
            _itemServiceMock
                .Setup(s => s.DeleteAsync(999, "user-1", false))
                .ThrowsAsync(new InvalidOperationException("Item has an ongoing loan."));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Delete(999));
        }

        [Fact]
        public async Task GetQrCode_ReturnsOk_WithQrCode()
        {
            var qr = MakeQrCode(1);
            _itemServiceMock
                .Setup(s => s.GetQrCodeAsync(1, "user-1", false))
                .ReturnsAsync(qr);

            var result = await _controller.GetQrCode(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ItemDTO.ItemQrCodeDTO>(ok.Value);
            Assert.Equal("TESTQR123456", returned.QrCode);
        }

        [Fact]
        public async Task GetQrCode_ServiceThrows_ExceptionPropagates()
        {
            _itemServiceMock
                .Setup(s => s.GetQrCodeAsync(1, "user-1", false))
                .ThrowsAsync(new UnauthorizedAccessException("Only the item owner can view the QR code."));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.GetQrCode(1));
        }

   
        [Fact]
        public async Task Scan_ReturnsOk_WithItemDetail()
        {
            var detail = MakeItemDetail(1);
            _itemServiceMock
                .Setup(s => s.ScanQrCodeAsync("TESTQR123456", "user-1", false))
                .ReturnsAsync(detail);

            var result = await _controller.Scan("TESTQR123456");

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ItemDTO.ItemDetailDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
        }

        [Fact]
        public async Task Scan_CallsServiceWithCorrectArguments()
        {
            _itemServiceMock
                .Setup(s => s.ScanQrCodeAsync("QRCODE123456", "user-1", false))
                .ReturnsAsync(MakeItemDetail());

            await _controller.Scan("QRCODE123456");

            _itemServiceMock.Verify(s =>
                s.ScanQrCodeAsync("QRCODE123456", "user-1", false), Times.Once);
        }

        [Fact]
        public async Task Scan_ServiceThrows_ExceptionPropagates()
        {
            _itemServiceMock
                .Setup(s => s.ScanQrCodeAsync("INVALID", "user-1", false))
                .ThrowsAsync(new KeyNotFoundException("Invalid QR code."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Scan("INVALID"));
        }

 
        [Fact]
        public async Task GetPendingApprovals_ReturnsOk_WithPendingItems()
        {
            SetUser("admin-1", "Admin");
            var items = new List<ItemDTO.AdminPendingItemDTO>
            {
                new() { Id = 1, Title = "Drill", OwnerName = "Owner 1" },
                new() { Id = 2, Title = "Camera", OwnerName = "Owner 2" }
            };
            _itemServiceMock
                .Setup(s => s.GetPendingApprovalsAsync())
                .ReturnsAsync(items);

            var result = await _controller.GetPendingApprovals();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<ItemDTO.AdminPendingItemDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task AdminDecide_Approve_ReturnsOk()
        {
            SetUser("admin-1", "Admin");
            var dto = new ItemDTO.AdminItemDecisionDTO { IsApproved = true };
            _itemServiceMock
                .Setup(s => s.AdminDecideAsync(1, "admin-1", dto))
                .ReturnsAsync(MakeItemDetail());

            var result = await _controller.AdminDecide(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AdminDecide_Reject_ReturnsOk()
        {
            SetUser("admin-1", "Admin");
            var dto = new ItemDTO.AdminItemDecisionDTO
            {
                IsApproved = false,
                AdminNote = "Low quality photos."
            };
            _itemServiceMock
                .Setup(s => s.AdminDecideAsync(1, "admin-1", dto))
                .ReturnsAsync(MakeItemDetail());

            var result = await _controller.AdminDecide(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AdminDecide_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            var dto = new ItemDTO.AdminItemDecisionDTO { IsApproved = true };
            _itemServiceMock
                .Setup(s => s.AdminDecideAsync(5, "admin-1", dto))
                .ReturnsAsync(MakeItemDetail());

            await _controller.AdminDecide(5, dto);

            _itemServiceMock.Verify(s => s.AdminDecideAsync(5, "admin-1", dto), Times.Once);
        }

        [Fact]
        public async Task AdminDecide_ServiceThrows_ExceptionPropagates()
        {
            SetUser("admin-1", "Admin");
            var dto = new ItemDTO.AdminItemDecisionDTO { IsApproved = false };
            _itemServiceMock
                .Setup(s => s.AdminDecideAsync(1, "admin-1", dto))
                .ThrowsAsync(new ArgumentException("A reason is required when rejecting an item."));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.AdminDecide(1, dto));
        }
    }
}