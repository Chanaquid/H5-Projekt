using backend.Common;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace backend.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IUserBlockRepository> _userBlockRepoMock;
        private readonly Mock<ILoanMessageRepository> _loanMessageRepoMock;
        private readonly Mock<IDirectMessageRepository> _directMessageRepoMock;
        private readonly Mock<IFineRepository> _fineRepoMock;
        private readonly Mock<ILoanRepository> _loanRepoMock;
        private readonly Mock<IItemRepository> _itemRepoMock;
        private readonly Mock<IAppealRepository> _appealRepoMock;
        private readonly Mock<IVerificationRepository> _verificationRepoMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _userBlockRepoMock = new Mock<IUserBlockRepository>();
            _loanMessageRepoMock = new Mock<ILoanMessageRepository>();
            _directMessageRepoMock = new Mock<IDirectMessageRepository>();
            _fineRepoMock = new Mock<IFineRepository>();
            _loanRepoMock = new Mock<ILoanRepository>();
            _itemRepoMock = new Mock<IItemRepository>();
            _appealRepoMock = new Mock<IAppealRepository>();
            _verificationRepoMock = new Mock<IVerificationRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _configMock = new Mock<IConfiguration>();

            var storeMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                storeMock.Object, null, null, null, null, null, null, null, null);

            _service = new UserService(
                _userRepoMock.Object,
                _userBlockRepoMock.Object,
                _loanMessageRepoMock.Object,
                _directMessageRepoMock.Object,
                _fineRepoMock.Object,
                _loanRepoMock.Object,
                _itemRepoMock.Object,
                _appealRepoMock.Object,
                _verificationRepoMock.Object,
                _userManagerMock.Object,
                _emailServiceMock.Object,
                _configMock.Object);
        }

        [Fact]
        public async Task GetProfileAsync_ValidUser_ReturnsProfileDTO()
        {
            var user = MakeUser("user-1");
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);

            var result = await _service.GetProfileAsync("user-1");

            result.Should().NotBeNull();
            result.Id.Should().Be("user-1");
            result.FullName.Should().Be("Test User");
        }

        [Fact]
        public async Task GetProfileAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync("ghost")).ReturnsAsync((ApplicationUser?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetProfileAsync("ghost"));
        }

        [Theory]
        [InlineData(75, "Free")]
        [InlineData(50, "Free")]
        [InlineData(49, "AdminApproval")]
        [InlineData(20, "AdminApproval")]
        [InlineData(19, "Blocked")]
        [InlineData(0, "Blocked")]
        public async Task GetProfileAsync_BorrowingStatus_CorrectlyMapped(int score, string expectedStatus)
        {
            var user = MakeUser("user-1", score);
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);

            var result = await _service.GetProfileAsync("user-1");

            result.BorrowingStatus.Should().Be(expectedStatus);
        }


        [Fact]
        public async Task GetPublicProfileAsync_ValidUser_ReturnsSafeSubset()
        {
            var user = MakeUser("user-1");
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);

            var result = await _service.GetPublicProfileAsync("user-1");

            result.Should().NotBeNull();
            result.Id.Should().Be("user-1");
            result.FullName.Should().Be("Test User");
        }

        [Fact]
        public async Task GetPublicProfileAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync("ghost")).ReturnsAsync((ApplicationUser?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetPublicProfileAsync("ghost"));
        }

  
        [Fact]
        public async Task UpdateProfileAsync_ValidUpdate_ReturnsUpdatedDTO()
        {
            var user = MakeUser("user-1");
            var dto = new UserDTO.UpdateProfileDTO
            {
                FullName = "New Name",
                UserName = "testuser", 
                Address = "New Address"
            };

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);

            var result = await _service.UpdateProfileAsync("user-1", dto);

            result.FullName.Should().Be("New Name");
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateProfileAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync("ghost")).ReturnsAsync((ApplicationUser?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateProfileAsync("ghost", new UserDTO.UpdateProfileDTO
                {
                    FullName = "X",
                    UserName = "x"
                }));
        }

        [Fact]
        public async Task UpdateProfileAsync_UsernameTaken_ThrowsArgumentException()
        {
            var user = MakeUser("user-1");
            user.UserName = "oldname";

            var dto = new UserDTO.UpdateProfileDTO { FullName = "Name", UserName = "takenname" };
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.FindByNameAsync("takenname"))
                .ReturnsAsync(MakeUser("other-user"));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateProfileAsync("user-1", dto));
        }

        [Fact]
        public async Task UpdateProfileAsync_UsernameChanged_CallsSetUserNameAsync()
        {
            var user = MakeUser("user-1");
            user.UserName = "oldname";

            var dto = new UserDTO.UpdateProfileDTO { FullName = "Name", UserName = "newname" };
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.FindByNameAsync("newname"))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(m => m.SetUserNameAsync(user, "newname"))
                .ReturnsAsync(IdentityResult.Success);

            await _service.UpdateProfileAsync("user-1", dto);

            _userManagerMock.Verify(m => m.SetUserNameAsync(user, "newname"), Times.Once);
        }

        [Fact]
        public async Task UpdateProfileAsync_FieldsAreTrimmed()
        {
            var user = MakeUser("user-1");
            var dto = new UserDTO.UpdateProfileDTO
            {
                FullName = "  John Doe  ",
                UserName = "testuser",
                Address = "  123 Main St  "
            };

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);

            await _service.UpdateProfileAsync("user-1", dto);

            user.FullName.Should().Be("John Doe");
            user.Address.Should().Be("123 Main St");
        }


        [Fact]
        public async Task DeleteAccountAsync_ValidRequest_SoftDeletesUser()
        {
            var user = MakeUser("user-1");
            var dto = new UserDTO.DeleteAccountDTO { Password = "correct" };

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
            _loanRepoMock.Setup(r => r.GetByBorrowerIdAsync("user-1")).ReturnsAsync(new List<Loan>());
            _loanRepoMock.Setup(r => r.GetByOwnerIdAsync("user-1")).ReturnsAsync(new List<Loan>());

            await _service.DeleteAccountAsync("user-1", dto);

            user.IsDeleted.Should().BeTrue();
            user.DeletedAt.Should().NotBeNull();
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAccountAsync_WrongPassword_ThrowsArgumentException()
        {
            var user = MakeUser("user-1");
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.DeleteAccountAsync("user-1", new UserDTO.DeleteAccountDTO { Password = "wrong" }));
        }

        [Fact]
        public async Task DeleteAccountAsync_UnpaidFines_ThrowsInvalidOperationException()
        {
            var user = MakeUser("user-1");
            user.UnpaidFinesTotal = 50;

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DeleteAccountAsync("user-1", new UserDTO.DeleteAccountDTO { Password = "correct" }));
        }

        [Theory]
        [InlineData(LoanStatus.Pending)]
        [InlineData(LoanStatus.AdminPending)]
        [InlineData(LoanStatus.Approved)]
        [InlineData(LoanStatus.Active)]
        [InlineData(LoanStatus.Late)]
        public async Task DeleteAccountAsync_OngoingBorrowedLoan_ThrowsInvalidOperationException(LoanStatus status)
        {
            var user = MakeUser("user-1");
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
            _loanRepoMock.Setup(r => r.GetByBorrowerIdAsync("user-1"))
                .ReturnsAsync(new List<Loan> { MakeLoan(1, "user-1", "owner-1", status) });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DeleteAccountAsync("user-1", new UserDTO.DeleteAccountDTO { Password = "correct" }));
        }

        [Theory]
        [InlineData(LoanStatus.Pending)]
        [InlineData(LoanStatus.AdminPending)]
        [InlineData(LoanStatus.Approved)]
        [InlineData(LoanStatus.Active)]
        [InlineData(LoanStatus.Late)]
        public async Task DeleteAccountAsync_OngoingOwnedLoan_ThrowsInvalidOperationException(LoanStatus status)
        {
            var user = MakeUser("user-1");
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
            _loanRepoMock.Setup(r => r.GetByBorrowerIdAsync("user-1")).ReturnsAsync(new List<Loan>());
            _loanRepoMock.Setup(r => r.GetByOwnerIdAsync("user-1"))
                .ReturnsAsync(new List<Loan> { MakeLoan(1, "borrower-1", "user-1", status) });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DeleteAccountAsync("user-1", new UserDTO.DeleteAccountDTO { Password = "correct" }));
        }

        [Fact]
        public async Task DeleteAccountAsync_SoftDeleteScramblesPII()
        {
            var user = MakeUser("user-1");
            user.Email = "real@example.com";
            user.AvatarUrl = "https://example.com/avatar.jpg";

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
            _loanRepoMock.Setup(r => r.GetByBorrowerIdAsync("user-1")).ReturnsAsync(new List<Loan>());
            _loanRepoMock.Setup(r => r.GetByOwnerIdAsync("user-1")).ReturnsAsync(new List<Loan>());

            await _service.DeleteAccountAsync("user-1", new UserDTO.DeleteAccountDTO { Password = "correct" });

            user.Email.Should().NotBe("real@example.com");
            user.AvatarUrl.Should().BeNull();
            user.PasswordHash.Should().BeNull();
        }


        [Fact]
        public async Task AdminAdjustScoreAsync_ValidAdjustment_UpdatesScore()
        {
            var user = MakeUser("user-1", score: 50);
            var dto = new UserDTO.AdminScoreAdjustDTO { PointsChanged = 10, Note = "Good behavior" };

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);

            await _service.AdminAdjustScoreAsync("user-1", dto);

            user.Score.Should().Be(60);
            _userRepoMock.Verify(r => r.AddScoreHistoryAsync(It.Is<ScoreHistory>(s =>
                s.PointsChanged == 10 &&
                s.ScoreAfterChange == 60 &&
                s.Reason == ScoreChangeReason.AdminAdjustment)), Times.Once);
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AdminAdjustScoreAsync_WouldExceed100_ThrowsArgumentException()
        {
            var user = MakeUser("user-1", score: 95);
            var dto = new UserDTO.AdminScoreAdjustDTO { PointsChanged = 10 };
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.AdminAdjustScoreAsync("user-1", dto));
        }

        [Fact]
        public async Task AdminAdjustScoreAsync_WouldGoBelowZero_ThrowsArgumentException()
        {
            var user = MakeUser("user-1", score: 5);
            var dto = new UserDTO.AdminScoreAdjustDTO { PointsChanged = -10 };
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.AdminAdjustScoreAsync("user-1", dto));
        }

        [Fact]
        public async Task AdminAdjustScoreAsync_ExactlyAt100_Succeeds()
        {
            var user = MakeUser("user-1", score: 90);
            var dto = new UserDTO.AdminScoreAdjustDTO { PointsChanged = 10 };
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);

            await _service.AdminAdjustScoreAsync("user-1", dto);

            user.Score.Should().Be(100);
        }

        [Fact]
        public async Task AdminAdjustScoreAsync_ExactlyAt0_Succeeds()
        {
            var user = MakeUser("user-1", score: 10);
            var dto = new UserDTO.AdminScoreAdjustDTO { PointsChanged = -10 };
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);

            await _service.AdminAdjustScoreAsync("user-1", dto);

            user.Score.Should().Be(0);
        }

        [Fact]
        public async Task AdminAdjustScoreAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync("ghost")).ReturnsAsync((ApplicationUser?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.AdminAdjustScoreAsync("ghost", new UserDTO.AdminScoreAdjustDTO { PointsChanged = 5 }));
        }

        [Fact]
        public async Task GetScoreHistoryAsync_ReturnsOrderedMappedHistory()
        {
            var user = MakeUser("user-1");
            var history = new List<ScoreHistory>
            {
                new() { Id = 1, PointsChanged = 5, ScoreAfterChange = 55, Reason = ScoreChangeReason.OnTimeReturn, CreatedAt = DateTime.UtcNow },
                new() { Id = 2, PointsChanged = -5, ScoreAfterChange = 50, Reason = ScoreChangeReason.LateReturn, CreatedAt = DateTime.UtcNow }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _userRepoMock.Setup(r => r.GetScoreHistoryAsync("user-1")).ReturnsAsync(history);

            var result = await _service.GetScoreHistoryAsync("user-1");

            result.Should().HaveCount(2);
            result[0].Reason.Should().Be("OnTimeReturn");
            result[1].Reason.Should().Be("LateReturn");
        }

        [Fact]
        public async Task GetScoreHistoryAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync("ghost")).ReturnsAsync((ApplicationUser?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetScoreHistoryAsync("ghost"));
        }


        [Fact]
        public async Task AdminSoftDeleteUserAsync_NoIssues_SoftDeletesAndReturnsSuccess()
        {
            var user = MakeUser("user-1");
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _loanRepoMock.Setup(r => r.GetByBorrowerIdAsync("user-1")).ReturnsAsync(new List<Loan>());
            _itemRepoMock.Setup(r => r.GetByOwnerAsync("user-1")).ReturnsAsync(new List<Item>());

            var result = await _service.AdminSoftDeleteUserAsync("user-1", "admin-1");

            result.Success.Should().BeTrue();
            user.IsDeleted.Should().BeTrue();
            user.DeletedByAdminId.Should().Be("admin-1");
            _userRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AdminSoftDeleteUserAsync_ActiveBorrowedLoan_ThrowsInvalidOperationException()
        {
            var user = MakeUser("user-1");
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _loanRepoMock.Setup(r => r.GetByBorrowerIdAsync("user-1"))
                .ReturnsAsync(new List<Loan> { MakeLoan(1, "user-1", "owner-1", LoanStatus.Active) });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AdminSoftDeleteUserAsync("user-1", "admin-1"));
        }

        [Fact]
        public async Task AdminSoftDeleteUserAsync_LateBorrowedLoan_ThrowsInvalidOperationException()
        {
            var user = MakeUser("user-1");
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _loanRepoMock.Setup(r => r.GetByBorrowerIdAsync("user-1"))
                .ReturnsAsync(new List<Loan> { MakeLoan(1, "user-1", "owner-1", LoanStatus.Late) });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AdminSoftDeleteUserAsync("user-1", "admin-1"));
        }

        [Fact]
        public async Task AdminSoftDeleteUserAsync_PendingBorrowedLoans_AreCancelled()
        {
            var user = MakeUser("user-1");
            var loan = MakeLoan(1, "user-1", "owner-1", LoanStatus.Pending);

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _loanRepoMock.Setup(r => r.GetByBorrowerIdAsync("user-1"))
                .ReturnsAsync(new List<Loan> { loan });
            _itemRepoMock.Setup(r => r.GetByOwnerAsync("user-1")).ReturnsAsync(new List<Item>());

            var result = await _service.AdminSoftDeleteUserAsync("user-1", "admin-1");

            loan.Status.Should().Be(LoanStatus.Cancelled);
            result.Warnings.Should().ContainMatch("*pending/approved borrowed loan(s) were automatically cancelled*");
        }

        [Fact]
        public async Task AdminSoftDeleteUserAsync_UnpaidFines_WarnsButProceeds()
        {
            var user = MakeUser("user-1");
            user.UnpaidFinesTotal = 50;

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _loanRepoMock.Setup(r => r.GetByBorrowerIdAsync("user-1")).ReturnsAsync(new List<Loan>());
            _itemRepoMock.Setup(r => r.GetByOwnerAsync("user-1")).ReturnsAsync(new List<Item>());

            var result = await _service.AdminSoftDeleteUserAsync("user-1", "admin-1");

            result.Success.Should().BeTrue();
            result.Warnings.Should().ContainMatch("*unpaid fines*");
        }

        [Fact]
        public async Task AdminSoftDeleteUserAsync_ItemOnActiveLoan_TransferredToAdmin()
        {
            var user = MakeUser("user-1");
            var item = new Item
            {
                Id = 1,
                OwnerId = "user-1",
                Title = "Drill",
                Loans = new List<Loan> { MakeLoan(1, "borrower-1", "user-1", LoanStatus.Active) }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _loanRepoMock.Setup(r => r.GetByBorrowerIdAsync("user-1")).ReturnsAsync(new List<Loan>());
            _itemRepoMock.Setup(r => r.GetByOwnerAsync("user-1")).ReturnsAsync(new List<Item> { item });

            var result = await _service.AdminSoftDeleteUserAsync("user-1", "admin-1");

            item.OwnerId.Should().Be("admin-1");
            result.Warnings.Should().ContainMatch("*transferred to admin*");
        }

        [Fact]
        public async Task AdminSoftDeleteUserAsync_ItemNotOnLoan_Deactivated()
        {
            var user = MakeUser("user-1");
            var item = new Item
            {
                Id = 1,
                OwnerId = "user-1",
                Title = "Drill",
                IsActive = true,
                Loans = new List<Loan>()
            };

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _loanRepoMock.Setup(r => r.GetByBorrowerIdAsync("user-1")).ReturnsAsync(new List<Loan>());
            _itemRepoMock.Setup(r => r.GetByOwnerAsync("user-1")).ReturnsAsync(new List<Item> { item });

            await _service.AdminSoftDeleteUserAsync("user-1", "admin-1");

            item.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task AdminSoftDeleteUserAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync("ghost")).ReturnsAsync((ApplicationUser?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.AdminSoftDeleteUserAsync("ghost", "admin-1"));
        }


        [Fact]
        public async Task AdminEditUserAsync_ScoreChange_LogsToScoreHistory()
        {
            var user = MakeUser("user-1", score: 50);
            var dto = new UserDTO.AdminEditUserDTO { Score = 70, ScoreNote = "Manual fix" };
            var updatedUser = MakeUser("user-1", score: 70);

            _userRepoMock.Setup(r => r.GetByIdIgnoreFiltersAsync("user-1")).ReturnsAsync(user);
            _userRepoMock.SetupSequence(r => r.GetByIdIgnoreFiltersAsync("user-1"))
                .ReturnsAsync(user)
                .ReturnsAsync(updatedUser);
            _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { Roles.User });

            await _service.AdminEditUserAsync("user-1", "admin-1", dto);

            _userRepoMock.Verify(r => r.AddScoreHistoryAsync(It.Is<ScoreHistory>(s =>
                s.PointsChanged == 20 &&
                s.ScoreAfterChange == 70)), Times.Once);
        }

        [Fact]
        public async Task AdminEditUserAsync_ScoreOutOfRange_ThrowsArgumentException()
        {
            var user = MakeUser("user-1");
            var dto = new UserDTO.AdminEditUserDTO { Score = 150 };
            _userRepoMock.Setup(r => r.GetByIdIgnoreFiltersAsync("user-1")).ReturnsAsync(user);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.AdminEditUserAsync("user-1", "admin-1", dto));
        }

        [Fact]
        public async Task AdminEditUserAsync_NegativeScore_ThrowsArgumentException()
        {
            var user = MakeUser("user-1");
            var dto = new UserDTO.AdminEditUserDTO { Score = -1 };
            _userRepoMock.Setup(r => r.GetByIdIgnoreFiltersAsync("user-1")).ReturnsAsync(user);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.AdminEditUserAsync("user-1", "admin-1", dto));
        }

        [Fact]
        public async Task AdminEditUserAsync_NegativeUnpaidFines_ThrowsArgumentException()
        {
            var user = MakeUser("user-1");
            var dto = new UserDTO.AdminEditUserDTO { UnpaidFinesTotal = -10 };
            _userRepoMock.Setup(r => r.GetByIdIgnoreFiltersAsync("user-1")).ReturnsAsync(user);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.AdminEditUserAsync("user-1", "admin-1", dto));
        }

        [Fact]
        public async Task AdminEditUserAsync_UsernameTakenByOtherUser_ThrowsArgumentException()
        {
            var user = MakeUser("user-1");
            user.UserName = "oldname";
            var dto = new UserDTO.AdminEditUserDTO { Username = "takenname" };

            _userRepoMock.Setup(r => r.GetByIdIgnoreFiltersAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.FindByNameAsync("takenname"))
                .ReturnsAsync(MakeUser("other-user")); //different user owns this username

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.AdminEditUserAsync("user-1", "admin-1", dto));
        }

        [Fact]
        public async Task AdminEditUserAsync_EmailTakenByOtherUser_ThrowsArgumentException()
        {
            var user = MakeUser("user-1");
            user.NormalizedEmail = "OLD@EXAMPLE.COM";
            var dto = new UserDTO.AdminEditUserDTO { Email = "taken@example.com" };

            _userRepoMock.Setup(r => r.GetByIdIgnoreFiltersAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.FindByEmailAsync("taken@example.com"))
                .ReturnsAsync(MakeUser("other-user"));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.AdminEditUserAsync("user-1", "admin-1", dto));
        }


        //Helpers
        private static ApplicationUser MakeUser(string id, int score = 75) => new()
        {
            Id = id,
            FullName = "Test User",
            UserName = "testuser",
            Email = "test@example.com",
            Score = score,
            UnpaidFinesTotal = 0,
            IsVerified = false,
            MembershipDate = DateTime.UtcNow
        };

        private static Loan MakeLoan(int id, string borrowerId, string ownerId, LoanStatus status) => new()
        {
            Id = id,
            BorrowerId = borrowerId,
            Item = new Item { OwnerId = ownerId, Title = "Test Item" },
            Status = status,
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow.AddDays(5)
        };








    }
}
