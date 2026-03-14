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
    public class AppealServiceTests
    {
        private readonly Mock<IAppealRepository> _mockAppealRepo = new();
        private readonly Mock<IUserRepository> _mockUserRepo = new();
        private readonly Mock<IFineRepository> _mockFineRepo = new();
        private readonly Mock<INotificationService> _mockNotification = new();

        private readonly AppealService _appealService;

        public AppealServiceTests()
        {
            _appealService = new AppealService(
                _mockAppealRepo.Object,
                _mockUserRepo.Object,
                _mockFineRepo.Object,
                _mockNotification.Object
            );
        }


        [Fact]
        public async Task CreateScoreAppealAsync_CreateAppealSuccess()
        {
            var userId = "userId";
            var dto = new AppealDTO.CreateScoreAppealDTO
            {
                Message = "Score Appeal message"
            };

            var user = new ApplicationUser
            {
                Id = userId,
                IsDeleted = false,
                Score = 15
             
            };

            _mockUserRepo.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _mockAppealRepo.Setup(x => x.GetPendingByUserIdAsync(userId))
                .ReturnsAsync((Appeal)null);

            _mockAppealRepo.Setup(x => x.AddAsync(It.IsAny<Appeal>()))
                .Returns(Task.CompletedTask);

            _mockAppealRepo.Setup(x => x.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync(new Appeal
                {
                    UserId = userId,
                    Message = dto.Message
                });


            var result = await _appealService.CreateScoreAppealAsync(userId, dto);
            var response = Assert.IsType<AppealDTO.AppealResponseDTO>(result);


            Assert.NotNull(response);
            Assert.Equal(userId, response.UserId);
            Assert.Equal(dto.Message, response.Message);
            Assert.Equal(AppealStatus.Pending.ToString(), response.Status);

            _mockNotification.Verify(x => x.SendAsync(userId,
                NotificationType.AppealSubmitted,
                "Your score appeal has been submitted and is under review.",
                It.IsAny<int>(),
                NotificationReferenceType.Appeal), Times.Once);


        }


        [Fact]
        public async Task CreateScoreAppealAsync_UserNotFound_ThrowKeyNotFoundException()
        {
            var userId = "nonexistentUserId";

            _mockUserRepo.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null);


            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _appealService.CreateScoreAppealAsync(userId, new AppealDTO.CreateScoreAppealDTO
                {
                    Message = "I wanna appeal for my score"
                }));


            Assert.Equal("User not found.", ex.Message);

            _mockAppealRepo.Verify(x => x.GetPendingByUserIdAsync(It.IsAny<string>()), Times.Never);
            _mockAppealRepo.Verify(x => x.AddAsync(It.IsAny<Appeal>()), Times.Never);
            _mockNotification.Verify(x => x.SendAsync(
                It.IsAny<string>(),
                NotificationType.AppealSubmitted,
                It.IsAny<string>(),
                It.IsAny<int>(),
                NotificationReferenceType.Appeal
                ), Times.Never);
        }

        [Fact]
        public async Task CreateScoreAppealAsync_ValidScore_ThrowInvalidOperationException()
        {
            var userId = "UserId";

            var user = new ApplicationUser
            {
                Id = userId,
                Score = 20
            };

            _mockUserRepo.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);


            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _appealService.CreateScoreAppealAsync(userId, new AppealDTO.CreateScoreAppealDTO
                {
                    Message = "I wanna appeal for my score"
                }));


            Assert.Equal("Your score is 20 or above. You do not need a score appeal.", ex.Message);

            _mockAppealRepo.Verify(x => x.GetPendingByUserIdAsync(It.IsAny<string>()), Times.Never);
            _mockAppealRepo.Verify(x => x.AddAsync(It.IsAny<Appeal>()), Times.Never);
            _mockNotification.Verify(x => x.SendAsync(
                It.IsAny<string>(),
                NotificationType.AppealSubmitted,
                It.IsAny<string>(),
                It.IsAny<int>(),
                NotificationReferenceType.Appeal
                ), Times.Never);
        }


        [Fact]
        public async Task CreateScoreAppealAsync_AlreadyHavePendingAppeal_ThrowInvalidOperationException()
        {
            var userId = "UserId";

            var user = new ApplicationUser
            {
                Id = userId,
                Score = 14
            };

            var appeal = new Appeal
            {
                Id = 1,
                UserId = userId,
                AppealType = AppealType.Score

            };

            _mockUserRepo.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _mockAppealRepo.Setup(x => x.GetPendingByUserIdAsync(userId))
                .ReturnsAsync(appeal);


            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _appealService.CreateScoreAppealAsync(userId, new AppealDTO.CreateScoreAppealDTO
                {
                    Message = "I wanna appeal for my score"
                }));


            Assert.Equal("You already have a pending appeal.", ex.Message);

            _mockAppealRepo.Verify(x => x.GetPendingByUserIdAsync(It.IsAny<string>()), Times.Once);
            _mockAppealRepo.Verify(x => x.AddAsync(It.IsAny<Appeal>()), Times.Never);
            _mockNotification.Verify(x => x.SendAsync(
                It.IsAny<string>(),
                NotificationType.AppealSubmitted,
                It.IsAny<string>(),
                It.IsAny<int>(),
                NotificationReferenceType.Appeal
                ), Times.Never);
        }


        [Fact]
        public async Task CreateScoreAppealAsync_AppealMessageEmpty_ThrowArgumentException()
        {
            var userId = "UserId";

            var user = new ApplicationUser
            {
                Id = userId,
                Score = 14
            };

            var dto = new AppealDTO.CreateScoreAppealDTO
            {
                Message = ""
            };

            _mockUserRepo.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _mockAppealRepo.Setup(x => x.GetPendingByUserIdAsync(userId))
                .ReturnsAsync((Appeal)null);


            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _appealService.CreateScoreAppealAsync(userId, dto));


            Assert.Equal("Appeal message cannot be empty.", ex.Message);

            _mockAppealRepo.Verify(x => x.GetPendingByUserIdAsync(It.IsAny<string>()), Times.Once);
            _mockAppealRepo.Verify(x => x.AddAsync(It.IsAny<Appeal>()), Times.Never);
            _mockNotification.Verify(x => x.SendAsync(
                It.IsAny<string>(),
                NotificationType.AppealSubmitted,
                It.IsAny<string>(),
                It.IsAny<int>(),
                NotificationReferenceType.Appeal
                ), Times.Never);
        }



        [Fact]
        public async Task CreateFineAppealAsync_ValidInfo_Success()
        {

            var userId = "userId";
            var user = new ApplicationUser
            {
                UserName = userId,
            };
            var dto = new AppealDTO.CreateFineAppealDTO
            {
                FineId = 1,
                Message = "I NEED FINE APPEAL"

            };

            var fine = new Fine
            {
                Id = 1,
                UserId = userId,
                Status = FineStatus.Unpaid 
            };

            _mockUserRepo.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _mockFineRepo.Setup(x => x.GetByIdWithDetailsAsync(dto.FineId))
                .ReturnsAsync(fine);

            _mockAppealRepo.Setup(x => x.GetPendingFineAppealByFineIdAsync(dto.FineId))
                .ReturnsAsync((Appeal)null);
            
            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync(new Appeal { Id = 1, Message = dto.Message });

            var result = await _appealService.CreateFineAppealAsync(userId, dto);


            Assert.NotNull(result);
            _mockAppealRepo.Verify(x => x.AddAsync(It.IsAny<Appeal>()), Times.Once);
            _mockAppealRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
            _mockNotification.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<NotificationReferenceType>()), Times.Once);
        




    }


        [Fact]
        public async Task CreateFineAppealAsync_UserNotFound_ThrowKeyNotFoundException()
        {

            var userId = "userId";
                 

            _mockUserRepo.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null);

            var result = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _appealService.CreateFineAppealAsync(userId, new AppealDTO.CreateFineAppealDTO
            {

            }));

            Assert.NotNull(result);
            Assert.Equal("User not found.", result.Message);

            _mockAppealRepo.Verify(x => x.AddAsync(It.IsAny<Appeal>()), Times.Never);
            _mockAppealRepo.Verify(x => x.SaveChangesAsync(), Times.Never);
            _mockNotification.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<NotificationReferenceType>()), Times.Never);


        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateFineAppealAsync_InvalidMessage_ThrowsArgumentException(string invalidMessage)
        {

            var userId = "userId";
            var dto = new AppealDTO.CreateFineAppealDTO
            {
                FineId = 1,
                Message = invalidMessage
            };

            _mockUserRepo.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(new ApplicationUser { Id = userId });


            var result = await Assert.ThrowsAsync<ArgumentException>(() =>
            _appealService.CreateFineAppealAsync(userId, dto));

            Assert.NotNull(result);
            Assert.Equal("Appeal message cannot be empty.", result.Message);

            _mockAppealRepo.Verify(x => x.AddAsync(It.IsAny<Appeal>()), Times.Never);
            _mockAppealRepo.Verify(x => x.SaveChangesAsync(), Times.Never);
            _mockNotification.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<NotificationReferenceType>()), Times.Never);


        }


        [Fact]
        public async Task CreateFineAppealAsync_ValidMessageWithSpaces_TrimsBeforeSaving()
        {
            var userId = "userId";
            var rawMessage = "   I am innocent   ";
            var expectedTrimmedMessage = "I am innocent";

            var dto = new AppealDTO.CreateFineAppealDTO { FineId = 1, Message = rawMessage };

            _mockUserRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser());
            _mockFineRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(new Fine { UserId = userId });
            _mockAppealRepo.Setup(x => x.GetPendingFineAppealByFineIdAsync(1)).ReturnsAsync((Appeal)null);

            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync(new Appeal { Message = expectedTrimmedMessage });

            await _appealService.CreateFineAppealAsync(userId, dto);

            _mockAppealRepo.Verify(x => x.AddAsync(It.Is<Appeal>(a =>
                a.Message == expectedTrimmedMessage)), Times.Once);
        }


        [Fact]
        public async Task CreateFineAppealAsync_WrongUser_ThrowsUnauthorizedAccessException()
        {
            var authenticatedUserId = "user-123";
            var actualFineOwnerId = "user-456"; //Different ID
            var dto = new AppealDTO.CreateFineAppealDTO { FineId = 1, Message = "Not my fine" };

            _mockUserRepo.Setup(x => x.GetByIdAsync(authenticatedUserId)).ReturnsAsync(new ApplicationUser());

            _mockFineRepo.Setup(x => x.GetByIdWithDetailsAsync(dto.FineId))
                .ReturnsAsync(new Fine { Id = 1, UserId = actualFineOwnerId });

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _appealService.CreateFineAppealAsync(authenticatedUserId, dto));

            Assert.Equal("You can only appeal your own fines.", ex.Message);
            _mockAppealRepo.Verify(x => x.AddAsync(It.IsAny<Appeal>()), Times.Never);
        }


        [Theory]
        [InlineData(FineStatus.Paid)]
        [InlineData(FineStatus.Waived)]
        public async Task CreateFineAppealAsync_FineIsClosed_ThrowsInvalidOperationException(FineStatus closedStatus)
        {
            var userId = "userId";
            var dto = new AppealDTO.CreateFineAppealDTO { FineId = 1, Message = "I want to appeal" };

            _mockUserRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser());

            _mockFineRepo.Setup(x => x.GetByIdWithDetailsAsync(1))
                .ReturnsAsync(new Fine { Id = 1, UserId = userId, Status = closedStatus });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _appealService.CreateFineAppealAsync(userId, dto));

            Assert.Equal("This fine is already closed and cannot be appealed.", ex.Message);
            _mockAppealRepo.Verify(x => x.AddAsync(It.IsAny<Appeal>()), Times.Never);
        }


        [Fact]
        public async Task CreateFineAppealAsync_AlreadyHasPendingAppeal_ThrowsInvalidOperationException()
        {
            var userId = "userId";
            var fineId = 1;
            var dto = new AppealDTO.CreateFineAppealDTO { FineId = fineId, Message = "Spamming appeal" };

            _mockUserRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser());

            _mockFineRepo.Setup(x => x.GetByIdWithDetailsAsync(fineId))
                .ReturnsAsync(new Fine { Id = fineId, UserId = userId, Status = FineStatus.Unpaid });

            _mockAppealRepo.Setup(x => x.GetPendingFineAppealByFineIdAsync(fineId))
                .ReturnsAsync(new Appeal { Id = 99, Status = AppealStatus.Pending });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _appealService.CreateFineAppealAsync(userId, dto));

            Assert.Equal("This fine already has a pending appeal.", ex.Message);
            _mockAppealRepo.Verify(x => x.AddAsync(It.IsAny<Appeal>()), Times.Never);
        }



        [Fact]
        public async Task GetMyAppealsAsync_AppealsExist_ReturnsMappedDtoList()
        {
            var userId = "user-123";
            var mockAppeals = new List<Appeal>
            {
                new Appeal { Id = 1, UserId = userId, Message = "Appeal 1", Status = AppealStatus.Pending },
                new Appeal { Id = 2, UserId = userId, Message = "Appeal 2", Status = AppealStatus.Approved }
            };

            _mockAppealRepo.Setup(x => x.GetAllByUserIdAsync(userId))
                .ReturnsAsync(mockAppeals);

            var result = await _appealService.GetMyAppealsAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Appeal 1", result[0].Message);
            Assert.Equal("Appeal 2", result[1].Message);

            _mockAppealRepo.Verify(x => x.GetAllByUserIdAsync(userId), Times.Once);
        }


        [Fact]
        public async Task GetMyAppealsAsync_NoAppeals_ReturnsEmptyList()
        {
            var userId = "user-999";
            _mockAppealRepo.Setup(x => x.GetAllByUserIdAsync(userId))
                .ReturnsAsync(new List<Appeal>()); 

            var result = await _appealService.GetMyAppealsAsync(userId);

            Assert.NotNull(result);
            Assert.Empty(result);
            _mockAppealRepo.Verify(x => x.GetAllByUserIdAsync(userId), Times.Once);
        }


        [Fact]
        public async Task GetMyAppealsAsync_CallsRepoWithCorrectUserId()
        {
            var userId = "specific-user-id";
            _mockAppealRepo.Setup(x => x.GetAllByUserIdAsync(userId))
                .ReturnsAsync(new List<Appeal>());

            await _appealService.GetMyAppealsAsync(userId);

            //This specifically checks that we didn't pass a hardcoded string or null to the repo
            _mockAppealRepo.Verify(x => x.GetAllByUserIdAsync(It.Is<string>(id => id == userId)), Times.Once);
        }


        [Fact]
        public async Task GetAllPendingAsync_AppealsFound_ReturnsMappedDtoList()
        {
            var pendingAppeals = new List<Appeal>
            {
                new Appeal { Id = 10, Message = "Appeal A", Status = AppealStatus.Pending },
                new Appeal { Id = 11, Message = "Appeal B", Status = AppealStatus.Pending }
            };

            _mockAppealRepo.Setup(x => x.GetAllPendingAsync())
                .ReturnsAsync(pendingAppeals);

            var result = await _appealService.GetAllPendingAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Message == "Appeal A");
            Assert.Contains(result, x => x.Message == "Appeal B");

            _mockAppealRepo.Verify(x => x.GetAllPendingAsync(), Times.Once);
        }


        [Fact]
        public async Task GetAllPendingAsync_NoPendingAppeals_ReturnsEmptyList()
        {
            _mockAppealRepo.Setup(x => x.GetAllPendingAsync())
                .ReturnsAsync(new List<Appeal>()); 

            var result = await _appealService.GetAllPendingAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
            _mockAppealRepo.Verify(x => x.GetAllPendingAsync(), Times.Once);
        }


        [Fact]
        public async Task GetAllPendingAsync_Mapping_CorrectlyTransfersData()
        {
            var pendingAppeals = new List<Appeal>
            {
                new Appeal { Id = 5, Message = "Test Message", Status = AppealStatus.Pending }
            };

            _mockAppealRepo.Setup(x => x.GetAllPendingAsync())
                .ReturnsAsync(pendingAppeals);

            var result = await _appealService.GetAllPendingAsync();

            var item = result.First();
            Assert.Equal(5, item.Id);
            Assert.Equal("Test Message", item.Message);
        }


        [Fact]
        public async Task DecideScoreAppealAsync_AppealNotFound_ThrowsKeyNotFoundException()
        {
            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync((Appeal)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _appealService.DecideScoreAppealAsync(1, "adminId", new AppealDTO.AdminScoreAppealDecisionDTO()));
        }

        [Fact]
        public async Task DecideScoreAppealAsync_WrongType_ThrowsInvalidOperationException()
        {
            var appeal = new Appeal { Id = 1, AppealType = AppealType.Fine };
            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(appeal);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _appealService.DecideScoreAppealAsync(1, "adminId", new AppealDTO.AdminScoreAppealDecisionDTO()));
        }

        [Fact]
        public async Task DecideScoreAppealAsync_AlreadyResolved_ThrowsInvalidOperationException()
        {
            var appeal = new Appeal { Id = 1, AppealType = AppealType.Score, Status = AppealStatus.Approved };
            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(appeal);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _appealService.DecideScoreAppealAsync(1, "adminId", new AppealDTO.AdminScoreAppealDecisionDTO()));
        }

        [Fact]
        public async Task DecideScoreAppealAsync_RejectingWithoutNote_ThrowsArgumentException()
        {
            var appeal = new Appeal { Id = 1, AppealType = AppealType.Score, Status = AppealStatus.Pending };
            var dto = new AppealDTO.AdminScoreAppealDecisionDTO { IsApproved = false, AdminNote = "" }; 

            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(appeal);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _appealService.DecideScoreAppealAsync(1, "adminId", dto));
        }


        [Fact]
        public async Task DecideScoreAppealAsync_RejectValid_Success()
        {
            var appeal = new Appeal { Id = 1, UserId = "userId", AppealType = AppealType.Score, Status = AppealStatus.Pending };
            var dto = new AppealDTO.AdminScoreAppealDecisionDTO { IsApproved = false, AdminNote = "Not enough evidence" };

            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(appeal);

            var result = await _appealService.DecideScoreAppealAsync(1, "adminId", dto);

            Assert.Equal(AppealStatus.Rejected, appeal.Status);
            Assert.Equal("Not enough evidence", appeal.AdminNote);
            _mockUserRepo.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never); 
            _mockNotification.Verify(x => x.SendAsync(It.IsAny<string>(), NotificationType.AppealRejected, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<NotificationReferenceType>()), Times.Once);
        }


        [Fact]
        public async Task DecideScoreAppealAsync_ApproveValid_UpdatesUserScoreAndHistory()
        {
            var userId = "user123";
            var appeal = new Appeal { Id = 1, UserId = userId, AppealType = AppealType.Score, Status = AppealStatus.Pending };
            var user = new ApplicationUser { Id = userId, Score = 50 };
            var dto = new AppealDTO.AdminScoreAppealDecisionDTO { IsApproved = true, NewScore = 80 };

            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(appeal);
            _mockUserRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

            await _appealService.DecideScoreAppealAsync(1, "adminId", dto);

            Assert.Equal(AppealStatus.Approved, appeal.Status);
            Assert.Equal(80, user.Score); 

            _mockUserRepo.Verify(x => x.AddScoreHistoryAsync(It.Is<ScoreHistory>(sh =>
                sh.PointsChanged == 30 && 
                sh.ScoreAfterChange == 80 &&
                sh.UserId == userId)), Times.Once);

            _mockUserRepo.Verify(x => x.UpdateAsync(user), Times.Once);
            _mockNotification.Verify(x => x.SendAsync(userId, NotificationType.AppealApproved, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<NotificationReferenceType>()), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public async Task DecideScoreAppealAsync_InvalidScoreRange_ThrowsArgumentException(int badScore)
        {
            var appeal = new Appeal { Id = 1, AppealType = AppealType.Score, Status = AppealStatus.Pending };
            var dto = new AppealDTO.AdminScoreAppealDecisionDTO { IsApproved = true, NewScore = badScore };

            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(appeal);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _appealService.DecideScoreAppealAsync(1, "adminId", dto));
        }


        [Fact]
        public async Task DecideFineAppealAsync_AppealNotFound_ThrowsKeyNotFoundException()
        {
            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync((Appeal)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _appealService.DecideFineAppealAsync(1, "adminId", new AppealDTO.AdminFineAppealDecisionDTO()));
        }

        [Fact]
        public async Task DecideFineAppealAsync_WrongType_ThrowsInvalidOperationException()
        {
            var appeal = new Appeal { Id = 1, AppealType = AppealType.Score }; //Wrong type
            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(appeal);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _appealService.DecideFineAppealAsync(1, "adminId", new AppealDTO.AdminFineAppealDecisionDTO()));
        }

        [Fact]
        public async Task DecideFineAppealAsync_ApproveWithoutResolution_ThrowsArgumentException()
        {
            var appeal = new Appeal { Id = 1, AppealType = AppealType.Fine, Status = AppealStatus.Pending };
            var dto = new AppealDTO.AdminFineAppealDecisionDTO { IsApproved = true, Resolution = null };

            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(appeal);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _appealService.DecideFineAppealAsync(1, "adminId", dto));
        }

        [Fact]
        public async Task DecideFineAppealAsync_Reject_UpdatesStatusOnly()
        {
            var appeal = new Appeal { Id = 1, UserId = "u1", AppealType = AppealType.Fine, Status = AppealStatus.Pending };
            var dto = new AppealDTO.AdminFineAppealDecisionDTO { IsApproved = false, AdminNote = "No proof" };

            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(appeal);

            await _appealService.DecideFineAppealAsync(1, "adminId", dto);

            Assert.Equal(AppealStatus.Rejected, appeal.Status);
            _mockFineRepo.Verify(x => x.Update(It.IsAny<Fine>()), Times.Never); 
        }

        [Fact]
        public async Task DecideFineAppealAsync_ApproveWaive_SetsFineToZero()
        {
            var userId = "user123";
            var appeal = new Appeal { Id = 1, UserId = userId, AppealType = AppealType.Fine, Status = AppealStatus.Pending, FineId = 50 };
            var fine = new Fine { Id = 50, UserId = userId, Amount = 100.00m, Status = FineStatus.Unpaid };
            var user = new ApplicationUser { Id = userId, UnpaidFinesTotal = 150.00m };
            var dto = new AppealDTO.AdminFineAppealDecisionDTO { IsApproved = true, Resolution = FineAppealResolution.Waive };

            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(appeal);
            _mockFineRepo.Setup(x => x.GetByIdWithDetailsAsync(50)).ReturnsAsync(fine);
            _mockUserRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

            await _appealService.DecideFineAppealAsync(1, "adminId", dto);

            Assert.Equal(0m, fine.Amount);
            Assert.Equal(FineStatus.Waived, fine.Status);
            Assert.Equal(50.00m, user.UnpaidFinesTotal); 
            _mockUserRepo.Verify(x => x.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task DecideFineAppealAsync_ApproveCustomAmount_UpdatesBalanceCorrectly()
        {
            var userId = "user123";
            var appeal = new Appeal { Id = 1, UserId = userId, AppealType = AppealType.Fine, Status = AppealStatus.Pending, FineId = 50 };
            var fine = new Fine { Id = 50, UserId = userId, Amount = 100.00m };
            var user = new ApplicationUser { Id = userId, UnpaidFinesTotal = 100.00m };

            var dto = new AppealDTO.AdminFineAppealDecisionDTO
            {
                IsApproved = true,
                Resolution = FineAppealResolution.Custom,
                CustomFineAmount = 30.00m
            };

            _mockAppealRepo.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(appeal);
            _mockFineRepo.Setup(x => x.GetByIdWithDetailsAsync(50)).ReturnsAsync(fine);
            _mockUserRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

            await _appealService.DecideFineAppealAsync(1, "adminId", dto);

            Assert.Equal(30.00m, fine.Amount);
            Assert.Equal(30.00m, user.UnpaidFinesTotal); 
            _mockFineRepo.Verify(x => x.Update(fine), Times.Once);
        }





    }
}
