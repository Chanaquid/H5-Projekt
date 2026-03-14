using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace backend.Tests.Services
{
    public class ItemServiceTests
    {
        private readonly Mock<IItemRepository> _itemRepoMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<ICategoryRepository> _categoryRepoMock;
        private readonly Mock<ILoanService> _loanServiceMock;

        private readonly ItemService _service;

        public ItemServiceTests()
        {
            _itemRepoMock = new Mock<IItemRepository>();
            _notificationServiceMock = new Mock<INotificationService>();
            _userRepoMock = new Mock<IUserRepository>();
            _categoryRepoMock = new Mock<ICategoryRepository>();
            _loanServiceMock = new Mock<ILoanService>();

            _service = new ItemService(
                _itemRepoMock.Object,
                _notificationServiceMock.Object,
                _userRepoMock.Object,
                _loanServiceMock.Object,
                _categoryRepoMock.Object
                );
        }


        [Fact]
        public async Task CreateAsync_ValidData_ReturnsCreatedItem()
        {
            var userId = "user-1";
            var dto = new ItemDTO.CreateItemDTO
            {
                CategoryId = 1,
                Title = "Drill",
                AvailableFrom = DateTime.UtcNow.AddDays(1),
                AvailableUntil = DateTime.UtcNow.AddDays(5)
            };

            _categoryRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Category { Id = 1 });
            _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId, Address = "Home" });
            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(0)).ReturnsAsync(new Item
            {
                Id = 1,
                Title = "Drill",
                Owner = new ApplicationUser(),
                Category = new Category()
            });

            var result = await _service.CreateAsync(userId, dto);

            Assert.NotNull(result);
            _itemRepoMock.Verify(x => x.AddAsync(It.IsAny<Item>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_InvalidDates_ThrowsArgumentException()
        {
            var dto = new ItemDTO.CreateItemDTO
            {
                AvailableFrom = DateTime.UtcNow.AddDays(5),
                AvailableUntil = DateTime.UtcNow.AddDays(1) //Invalid
            };
            _categoryRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Category());

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync("u1", dto));
        }

        [Fact]
        public async Task GetNearbyAsync_FiltersCorrectlyByDistance()
        {
            var lat = 55.6761; //Copenhagen
            var lng = 12.5683;
            var items = new List<Item> {
            new Item { Id = 1, PickupLatitude = 55.6700, PickupLongitude = 12.5600 }, 
            new Item { Id = 2, PickupLatitude = 59.3293, PickupLongitude = 18.0686 } 
        };
            _itemRepoMock.Setup(x => x.GetAllApprovedAsync()).ReturnsAsync(items);

            var result = await _service.GetNearbyAsync(lat, lng, 10); //10km

            
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public async Task ScanQrCode_ApprovedLoan_ChangesToActive()
        {
            var qr = "ABCD1234EFGH";
            var borrowerId = "borrower-1";
            var loan = new Loan { Id = 10, BorrowerId = borrowerId, Status = LoanStatus.Approved };
            var item = new Item
            {
                Id = 1,
                OwnerId = "owner-1",
                Loans = new List<Loan> { loan },
                Owner = new ApplicationUser(),
                Category = new Category()
            };

            _itemRepoMock.Setup(x => x.GetByQrCodeAsync(qr)).ReturnsAsync(item);
            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);

            await _service.ScanQrCodeAsync(qr, borrowerId);

            Assert.Equal(LoanStatus.Active, loan.Status);
            _notificationServiceMock.Verify(x => x.SendAsync("owner-1", NotificationType.LoanActive, It.IsAny<string>(), 10, It.IsAny<NotificationReferenceType>()), Times.Once);
        }

        [Fact]
        public async Task ScanQrCode_ActiveLoan_ChangesToReturned()
        {
            var qr = "QR123";
            var borrowerId = "b1";
            var loan = new Loan { Id = 5, BorrowerId = borrowerId, Status = LoanStatus.Active };
            var item = new Item { Id = 1, Loans = new List<Loan> { loan }, Owner = new ApplicationUser(), Category = new Category() };

            _itemRepoMock.Setup(x => x.GetByQrCodeAsync(qr)).ReturnsAsync(item);
            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);

            await _service.ScanQrCodeAsync(qr, borrowerId);

            Assert.Equal(LoanStatus.Returned, loan.Status);
            Assert.NotNull(loan.ActualReturnDate);
        }

        [Fact]
        public async Task AdminDecideAsync_Rejection_RequiresNote()
        {
            var item = new Item { Id = 1, Status = ItemStatus.Pending };
            var dto = new ItemDTO.AdminItemDecisionDTO { IsApproved = false, AdminNote = "" };
            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);

            await Assert.ThrowsAsync<ArgumentException>(() => _service.AdminDecideAsync(1, "admin-1", dto));
        }

        [Fact]
        public async Task DeleteAsync_ItemHasActiveLoan_ThrowsInvalidOperation()
        {
            var item = new Item
            {
                Id = 1,
                OwnerId = "u1",
                Loans = new List<Loan> { new Loan { Status = LoanStatus.Active } }
            };
            _itemRepoMock.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(item);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(1, "u1"));
        }


    }
}
