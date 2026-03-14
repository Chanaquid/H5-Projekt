using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace backend.Tests.Services
{
    public class VerificationServiceTests
    {
        private readonly Mock<IVerificationRepository> _verificationRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<INotificationService> _notificationMock;
        private readonly VerificationService _service;

        public VerificationServiceTests()
        {
            _verificationRepoMock = new Mock<IVerificationRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _notificationMock = new Mock<INotificationService>();
            _service = new VerificationService(
                _verificationRepoMock.Object,
                _userRepoMock.Object,
                _notificationMock.Object);
        }

        [Fact]
        public async Task SubmitRequestAsync_ValidRequest_ReturnsDTO()
        {
            var user = MakeUser("user-1");
            var dto = new VerificationDTO.CreateVerificationRequestDTO
            {
                DocumentUrl = "https://example.com/doc.jpg",
                DocumentType = "Passport"
            };
            var created = MakeRequest(1, "user-1");

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _verificationRepoMock.Setup(r => r.GetPendingByUserIdAsync("user-1"))
                .ReturnsAsync((VerificationRequest?)null);
            _verificationRepoMock.Setup(r => r.AddAsync(It.IsAny<VerificationRequest>()))
                .Returns(Task.CompletedTask);
            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync(created);

            var result = await _service.SubmitRequestAsync("user-1", dto);

            result.Should().NotBeNull();
            result.UserId.Should().Be("user-1");
            _verificationRepoMock.Verify(r => r.AddAsync(It.IsAny<VerificationRequest>()), Times.Once);
            _verificationRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SubmitRequestAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            _userRepoMock.Setup(r => r.GetByIdAsync("ghost")).ReturnsAsync((ApplicationUser?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.SubmitRequestAsync("ghost", new VerificationDTO.CreateVerificationRequestDTO
                {
                    DocumentUrl = "https://example.com/doc.jpg",
                    DocumentType = "Passport"
                }));
        }

        [Fact]
        public async Task SubmitRequestAsync_AlreadyVerified_ThrowsInvalidOperationException()
        {
            var user = MakeUser("user-1", isVerified: true);
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SubmitRequestAsync("user-1", new VerificationDTO.CreateVerificationRequestDTO
                {
                    DocumentUrl = "https://example.com/doc.jpg",
                    DocumentType = "Passport"
                }));
        }

        [Fact]
        public async Task SubmitRequestAsync_PendingRequestExists_ThrowsInvalidOperationException()
        {
            var user = MakeUser("user-1");
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _verificationRepoMock.Setup(r => r.GetPendingByUserIdAsync("user-1"))
                .ReturnsAsync(MakeRequest(1, "user-1"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SubmitRequestAsync("user-1", new VerificationDTO.CreateVerificationRequestDTO
                {
                    DocumentUrl = "https://example.com/doc.jpg",
                    DocumentType = "Passport"
                }));
        }

        [Fact]
        public async Task SubmitRequestAsync_EmptyDocumentUrl_ThrowsArgumentException()
        {
            var user = MakeUser("user-1");
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _verificationRepoMock.Setup(r => r.GetPendingByUserIdAsync("user-1"))
                .ReturnsAsync((VerificationRequest?)null);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.SubmitRequestAsync("user-1", new VerificationDTO.CreateVerificationRequestDTO
                {
                    DocumentUrl = "   ",
                    DocumentType = "Passport"
                }));
        }

        [Theory]
        [InlineData("InvalidType")]
        [InlineData("")]
        [InlineData("passport")] //wrong case — depends on Enum.TryParse behavior
        public async Task SubmitRequestAsync_InvalidDocumentType_ThrowsArgumentException(string documentType)
        {
            var user = MakeUser("user-1");
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _verificationRepoMock.Setup(r => r.GetPendingByUserIdAsync("user-1"))
                .ReturnsAsync((VerificationRequest?)null);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.SubmitRequestAsync("user-1", new VerificationDTO.CreateVerificationRequestDTO
                {
                    DocumentUrl = "https://example.com/doc.jpg",
                    DocumentType = documentType
                }));
        }

        [Theory]
        [InlineData("Passport")]
        [InlineData("NationalId")]
        [InlineData("DrivingLicense")]
        public async Task SubmitRequestAsync_ValidDocumentTypes_Succeeds(string documentType)
        {
            var user = MakeUser("user-1");
            var created = MakeRequest(1, "user-1");

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _verificationRepoMock.Setup(r => r.GetPendingByUserIdAsync("user-1"))
                .ReturnsAsync((VerificationRequest?)null);
            _verificationRepoMock.Setup(r => r.AddAsync(It.IsAny<VerificationRequest>()))
                .Returns(Task.CompletedTask);
            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync(created);

            var result = await _service.SubmitRequestAsync("user-1", new VerificationDTO.CreateVerificationRequestDTO
            {
                DocumentUrl = "https://example.com/doc.jpg",
                DocumentType = documentType
            });

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task SubmitRequestAsync_DocumentUrlIsTrimmed()
        {
            var user = MakeUser("user-1");
            var created = MakeRequest(1, "user-1");

            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);
            _verificationRepoMock.Setup(r => r.GetPendingByUserIdAsync("user-1"))
                .ReturnsAsync((VerificationRequest?)null);
            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync(created);

            VerificationRequest? captured = null;
            _verificationRepoMock.Setup(r => r.AddAsync(It.IsAny<VerificationRequest>()))
                .Callback<VerificationRequest>(r => captured = r)
                .Returns(Task.CompletedTask);

            await _service.SubmitRequestAsync("user-1", new VerificationDTO.CreateVerificationRequestDTO
            {
                DocumentUrl = "  https://example.com/doc.jpg  ",
                DocumentType = "Passport"
            });

            captured!.DocumentUrl.Should().Be("https://example.com/doc.jpg");
        }

        [Fact]
        public async Task SubmitRequestAsync_AlreadyVerified_DoesNotCallAdd()
        {
            var user = MakeUser("user-1", isVerified: true);
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SubmitRequestAsync("user-1", new VerificationDTO.CreateVerificationRequestDTO
                {
                    DocumentUrl = "https://example.com/doc.jpg",
                    DocumentType = "Passport"
                }));

            _verificationRepoMock.Verify(r => r.AddAsync(It.IsAny<VerificationRequest>()), Times.Never);
            _verificationRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }


        [Fact]
        public async Task GetUserRequestAsync_ReturnsLatestRequest()
        {
            var request = MakeRequest(1, "user-1");
            _verificationRepoMock.Setup(r => r.GetLatestByUserIdAsync("user-1")).ReturnsAsync(request);
            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(request);

            var result = await _service.GetUserRequestAsync("user-1");

            result.Should().NotBeNull();
            result.UserId.Should().Be("user-1");
        }

        [Fact]
        public async Task GetUserRequestAsync_NoRequest_ThrowsKeyNotFoundException()
        {
            _verificationRepoMock.Setup(r => r.GetLatestByUserIdAsync("user-1"))
                .ReturnsAsync((VerificationRequest?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetUserRequestAsync("user-1"));
        }

        [Fact]
        public async Task GetUserRequestAsync_ReturnsRejectedRequest_SoUserCanSeeReason()
        {
            var rejected = MakeRequest(1, "user-1", VerificationStatus.Rejected);
            rejected.AdminNote = "Document unclear";

            _verificationRepoMock.Setup(r => r.GetLatestByUserIdAsync("user-1")).ReturnsAsync(rejected);
            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(rejected);

            var result = await _service.GetUserRequestAsync("user-1");

            result.Status.Should().Be("Rejected");
            result.AdminNote.Should().Be("Document unclear");
        }


        [Fact]
        public async Task GetAllPendingAsync_ReturnsMappedList()
        {
            var requests = new List<VerificationRequest>
            {
                MakeRequest(1, "user-1"),
                MakeRequest(2, "user-2")
            };
            _verificationRepoMock.Setup(r => r.GetAllPendingAsync()).ReturnsAsync(requests);

            var result = await _service.GetAllPendingAsync();

            result.Should().HaveCount(2);
            result.All(r => r.Status == "Pending").Should().BeTrue();
        }

        [Fact]
        public async Task GetAllPendingAsync_NoPending_ReturnsEmptyList()
        {
            _verificationRepoMock.Setup(r => r.GetAllPendingAsync())
                .ReturnsAsync(new List<VerificationRequest>());

            var result = await _service.GetAllPendingAsync();

            result.Should().BeEmpty();
        }


        [Fact]
        public async Task DecideAsync_Approve_SetsStatusAndMarksUserVerified()
        {
            var user = MakeUser("user-1");
            var request = MakeRequest(1, "user-1");
            var dto = new VerificationDTO.AdminVerificationDecisionDTO { IsApproved = true };

            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(request);
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(user);

            var result = await _service.DecideAsync(1, "admin-1", dto);

            result.Status.Should().Be("Approved");
            user.IsVerified.Should().BeTrue();
            _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
            _verificationRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DecideAsync_Reject_SetsStatusAndDoesNotMarkUserVerified()
        {
            var user = MakeUser("user-1");
            var request = MakeRequest(1, "user-1");
            var dto = new VerificationDTO.AdminVerificationDecisionDTO
            {
                IsApproved = false,
                AdminNote = "Document expired"
            };

            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(request);

            var result = await _service.DecideAsync(1, "admin-1", dto);

            result.Status.Should().Be("Rejected");
            user.IsVerified.Should().BeFalse();
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task DecideAsync_Approve_SetsAdminFields()
        {
            var request = MakeRequest(1, "user-1");
            var dto = new VerificationDTO.AdminVerificationDecisionDTO { IsApproved = true };

            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(request);
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(MakeUser("user-1"));

            await _service.DecideAsync(1, "admin-1", dto);

            request.ReviewedByAdminId.Should().Be("admin-1");
            request.ReviewedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task DecideAsync_RequestNotFound_ThrowsKeyNotFoundException()
        {
            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(99))
                .ReturnsAsync((VerificationRequest?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.DecideAsync(99, "admin-1", new VerificationDTO.AdminVerificationDecisionDTO
                {
                    IsApproved = true
                }));
        }

        [Theory]
        [InlineData(VerificationStatus.Approved)]
        [InlineData(VerificationStatus.Rejected)]
        public async Task DecideAsync_AlreadyReviewed_ThrowsInvalidOperationException(VerificationStatus status)
        {
            var request = MakeRequest(1, "user-1", status);
            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(request);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.DecideAsync(1, "admin-1", new VerificationDTO.AdminVerificationDecisionDTO
                {
                    IsApproved = true
                }));
        }

        [Fact]
        public async Task DecideAsync_RejectWithoutNote_ThrowsArgumentException()
        {
            var request = MakeRequest(1, "user-1");
            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(request);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.DecideAsync(1, "admin-1", new VerificationDTO.AdminVerificationDecisionDTO
                {
                    IsApproved = false,
                    AdminNote = null
                }));
        }

        [Fact]
        public async Task DecideAsync_RejectWithWhitespaceNote_ThrowsArgumentException()
        {
            var request = MakeRequest(1, "user-1");
            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(request);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.DecideAsync(1, "admin-1", new VerificationDTO.AdminVerificationDecisionDTO
                {
                    IsApproved = false,
                    AdminNote = "   "
                }));
        }

        [Fact]
        public async Task DecideAsync_Approve_SendsApprovedNotification()
        {
            var request = MakeRequest(1, "user-1");
            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(request);
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync(MakeUser("user-1"));

            await _service.DecideAsync(1, "admin-1", new VerificationDTO.AdminVerificationDecisionDTO
            {
                IsApproved = true
            });

            _notificationMock.Verify(n => n.SendAsync(
                "user-1",
                NotificationType.VerificationApproved,
                It.IsAny<string>(),
                1,
                NotificationReferenceType.Verification), Times.Once);
        }

        [Fact]
        public async Task DecideAsync_Reject_SendsRejectedNotification()
        {
            var request = MakeRequest(1, "user-1");
            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(request);

            await _service.DecideAsync(1, "admin-1", new VerificationDTO.AdminVerificationDecisionDTO
            {
                IsApproved = false,
                AdminNote = "Blurry image"
            });

            _notificationMock.Verify(n => n.SendAsync(
                "user-1",
                NotificationType.VerificationRejected,
                It.Is<string>(s => s.Contains("Blurry image")),
                1,
                NotificationReferenceType.Verification), Times.Once);
        }

        [Fact]
        public async Task DecideAsync_AdminNoteIsTrimmed()
        {
            var request = MakeRequest(1, "user-1");
            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(request);

            await _service.DecideAsync(1, "admin-1", new VerificationDTO.AdminVerificationDecisionDTO
            {
                IsApproved = false,
                AdminNote = "  Blurry image  "
            });

            request.AdminNote.Should().Be("Blurry image");
        }

        [Fact]
        public async Task DecideAsync_ApproveWithUserNotFound_StillApprovesRequest()
        {
            // User lookup returning null should not block the approval —
            // the request status should still be set even if the user
            // record is missing (e.g. deleted between submission and review)
            var request = MakeRequest(1, "user-1");
            _verificationRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(request);
            _userRepoMock.Setup(r => r.GetByIdAsync("user-1")).ReturnsAsync((ApplicationUser?)null);

            var result = await _service.DecideAsync(1, "admin-1", new VerificationDTO.AdminVerificationDecisionDTO
            {
                IsApproved = true
            });

            result.Status.Should().Be("Approved");
            _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }



        //Helpers
        private static ApplicationUser MakeUser(string id, bool isVerified = false) => new()
        {
            Id = id,
            FullName = "Test User",
            Email = "test@example.com",
            IsVerified = isVerified
        };

        private static VerificationRequest MakeRequest(
            int id,
            string userId,
            VerificationStatus status = VerificationStatus.Pending) => new()
            {
                Id = id,
                UserId = userId,
                DocumentUrl = "https://example.com/doc.jpg",
                DocumentType = VerificationDocumentType.Passport,
                Status = status,
                SubmittedAt = DateTime.UtcNow,
                User = MakeUser(userId)
            };












    }
}
