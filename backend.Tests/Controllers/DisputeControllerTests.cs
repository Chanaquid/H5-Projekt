using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class DisputeControllerTests
    {
        private readonly Mock<IDisputeService> _disputeServiceMock;
        private readonly DisputeController _controller;

        public DisputeControllerTests()
        {
            _disputeServiceMock = new Mock<IDisputeService>();
            _controller = new DisputeController(_disputeServiceMock.Object);
            SetUser("user-1", "User");
        }

        private void SetUser(string userId, string role = "User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private static DisputeDTO.DisputeDetailDTO MakeDisputeDetail(int id = 1) => new()
        {
            Id = id,
            LoanId = 10,
            ItemTitle = "Drill",
            FiledByName = "User 1",
            FiledAs = "AsOwner",
            Description = "Item came back damaged.",
            Status = "Open",
            ResponseDeadline = DateTime.UtcNow.AddHours(72),
            CreatedAt = DateTime.UtcNow
        };

        private static DisputeDTO.DisputeSummaryDTO MakeDisputeSummary(int id = 1) => new()
        {
            Id = id,
            LoanId = 10,
            ItemTitle = "Drill",
            FiledByName = "User 1",
            FiledAs = "AsOwner",
            Status = "Open",
            ResponseDeadline = DateTime.UtcNow.AddHours(72),
            CreatedAt = DateTime.UtcNow
        };

 
        [Fact]
        public async Task GetMyDisputes_ReturnsOk_WithDisputes()
        {
            var disputes = new List<DisputeDTO.DisputeSummaryDTO>
            {
                MakeDisputeSummary(1),
                MakeDisputeSummary(2)
            };
            _disputeServiceMock
                .Setup(s => s.GetDisputesByUserIdAsync("user-1"))
                .ReturnsAsync(disputes);

            var result = await _controller.GetMyDisputes();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<DisputeDTO.DisputeSummaryDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetMyDisputes_ReturnsOk_WithEmptyList()
        {
            _disputeServiceMock
                .Setup(s => s.GetDisputesByUserIdAsync("user-1"))
                .ReturnsAsync(new List<DisputeDTO.DisputeSummaryDTO>());

            var result = await _controller.GetMyDisputes();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<DisputeDTO.DisputeSummaryDTO>>(ok.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public async Task GetMyDisputes_CallsServiceWithCorrectUserId()
        {
            SetUser("specific-user");
            _disputeServiceMock
                .Setup(s => s.GetDisputesByUserIdAsync("specific-user"))
                .ReturnsAsync(new List<DisputeDTO.DisputeSummaryDTO>());

            await _controller.GetMyDisputes();

            _disputeServiceMock.Verify(s => s.GetDisputesByUserIdAsync("specific-user"), Times.Once);
        }


        [Fact]
        public async Task GetById_ReturnsOk_WithDispute()
        {
            var detail = MakeDisputeDetail(1);
            _disputeServiceMock
                .Setup(s => s.GetByIdAsync(1, "user-1"))
                .ReturnsAsync(detail);

            var result = await _controller.GetById(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<DisputeDTO.DisputeDetailDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
        }

        [Fact]
        public async Task GetById_CallsServiceWithCorrectArguments()
        {
            _disputeServiceMock
                .Setup(s => s.GetByIdAsync(5, "user-1"))
                .ReturnsAsync(MakeDisputeDetail(5));

            await _controller.GetById(5);

            _disputeServiceMock.Verify(s => s.GetByIdAsync(5, "user-1"), Times.Once);
        }

        [Fact]
        public async Task GetById_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            _disputeServiceMock
                .Setup(s => s.GetByIdAsync(999, "user-1"))
                .ThrowsAsync(new KeyNotFoundException("Dispute 999 not found."));

            var result = await _controller.GetById(999);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFound.Value);
        }

        [Fact]
        public async Task GetById_ServiceThrows_Unauthorized_ReturnsForbid()
        {
            _disputeServiceMock
                .Setup(s => s.GetByIdAsync(1, "user-1"))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied."));

            var result = await _controller.GetById(1);

            Assert.IsType<ForbidResult>(result);
        }

   
        [Fact]
        public async Task Create_ReturnsOk_WithCreatedDispute()
        {
            var dto = new DisputeDTO.CreateDisputeDTO
            {
                LoanId = 10,
                FiledAs = "AsOwner",
                Description = "Item came back damaged."
            };
            var detail = MakeDisputeDetail();
            _disputeServiceMock
                .Setup(s => s.CreateAsync("user-1", dto))
                .ReturnsAsync(detail);

            var result = await _controller.Create(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<DisputeDTO.DisputeDetailDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
        }

        [Fact]
        public async Task Create_CallsServiceWithCorrectArguments()
        {
            var dto = new DisputeDTO.CreateDisputeDTO
            {
                LoanId = 10,
                FiledAs = "AsBorrower",
                Description = "Item was already damaged."
            };
            _disputeServiceMock
                .Setup(s => s.CreateAsync("user-1", dto))
                .ReturnsAsync(MakeDisputeDetail());

            await _controller.Create(dto);

            _disputeServiceMock.Verify(s => s.CreateAsync("user-1", dto), Times.Once);
        }

        [Fact]
        public async Task Create_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            var dto = new DisputeDTO.CreateDisputeDTO { LoanId = 999 };
            _disputeServiceMock
                .Setup(s => s.CreateAsync("user-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Loan 999 not found."));

            var result = await _controller.Create(dto);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFound.Value);
        }

        [Fact]
        public async Task Create_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            var dto = new DisputeDTO.CreateDisputeDTO { LoanId = 1, FiledAs = "AsOwner" };
            _disputeServiceMock
                .Setup(s => s.CreateAsync("user-1", dto))
                .ThrowsAsync(new InvalidOperationException("A dispute already exists for this loan."));

            var result = await _controller.Create(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task Create_ServiceThrows_ArgumentException_ReturnsBadRequest()
        {
            var dto = new DisputeDTO.CreateDisputeDTO { LoanId = 1, FiledAs = "Invalid" };
            _disputeServiceMock
                .Setup(s => s.CreateAsync("user-1", dto))
                .ThrowsAsync(new ArgumentException("Invalid FiledAs value."));

            var result = await _controller.Create(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }


        [Fact]
        public async Task Respond_ReturnsOk_WithUpdatedDispute()
        {
            var dto = new DisputeDTO.DisputeResponseDTO
            {
                ResponseDescription = "The item was already scratched when I got it."
            };
            var detail = MakeDisputeDetail();
            _disputeServiceMock
                .Setup(s => s.SubmitResponseAsync(1, "user-1", dto))
                .ReturnsAsync(detail);

            var result = await _controller.Respond(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<DisputeDTO.DisputeDetailDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
        }

        [Fact]
        public async Task Respond_CallsServiceWithCorrectArguments()
        {
            var dto = new DisputeDTO.DisputeResponseDTO { ResponseDescription = "My response." };
            _disputeServiceMock
                .Setup(s => s.SubmitResponseAsync(3, "user-1", dto))
                .ReturnsAsync(MakeDisputeDetail());

            await _controller.Respond(3, dto);

            _disputeServiceMock.Verify(s => s.SubmitResponseAsync(3, "user-1", dto), Times.Once);
        }

        [Fact]
        public async Task Respond_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            var dto = new DisputeDTO.DisputeResponseDTO { ResponseDescription = "My response." };
            _disputeServiceMock
                .Setup(s => s.SubmitResponseAsync(999, "user-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Dispute 999 not found."));

            var result = await _controller.Respond(999, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Respond_ServiceThrows_Unauthorized_ReturnsForbid()
        {
            var dto = new DisputeDTO.DisputeResponseDTO { ResponseDescription = "My response." };
            _disputeServiceMock
                .Setup(s => s.SubmitResponseAsync(1, "user-1", dto))
                .ThrowsAsync(new UnauthorizedAccessException("You are not the other party."));

            var result = await _controller.Respond(1, dto);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Respond_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            var dto = new DisputeDTO.DisputeResponseDTO { ResponseDescription = "My response." };
            _disputeServiceMock
                .Setup(s => s.SubmitResponseAsync(1, "user-1", dto))
                .ThrowsAsync(new InvalidOperationException("Response deadline has passed."));

            var result = await _controller.Respond(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

         [Fact]
        public async Task AddPhoto_ReturnsOk_WithMessage()
        {
            var dto = new DisputeDTO.AddDisputePhotoDTO
            {
                PhotoUrl = "http://test.com/photo.jpg",
                Caption = "Scratch on front"
            };
            _disputeServiceMock
                .Setup(s => s.AddPhotoAsync(1, "user-1", dto.PhotoUrl, dto.Caption))
                .Returns(Task.CompletedTask);

            var result = await _controller.AddPhoto(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AddPhoto_CallsServiceWithCorrectArguments()
        {
            var dto = new DisputeDTO.AddDisputePhotoDTO
            {
                PhotoUrl = "http://test.com/photo.jpg",
                Caption = "Dent on side"
            };
            _disputeServiceMock
                .Setup(s => s.AddPhotoAsync(2, "user-1", dto.PhotoUrl, dto.Caption))
                .Returns(Task.CompletedTask);

            await _controller.AddPhoto(2, dto);

            _disputeServiceMock.Verify(s =>
                s.AddPhotoAsync(2, "user-1", dto.PhotoUrl, dto.Caption), Times.Once);
        }

        [Fact]
        public async Task AddPhoto_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            var dto = new DisputeDTO.AddDisputePhotoDTO { PhotoUrl = "http://test.com/photo.jpg" };
            _disputeServiceMock
                .Setup(s => s.AddPhotoAsync(999, "user-1", dto.PhotoUrl, dto.Caption))
                .ThrowsAsync(new KeyNotFoundException("Dispute 999 not found."));

            var result = await _controller.AddPhoto(999, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AddPhoto_ServiceThrows_Unauthorized_ReturnsForbid()
        {
            var dto = new DisputeDTO.AddDisputePhotoDTO { PhotoUrl = "http://test.com/photo.jpg" };
            _disputeServiceMock
                .Setup(s => s.AddPhotoAsync(1, "user-1", dto.PhotoUrl, dto.Caption))
                .ThrowsAsync(new UnauthorizedAccessException("You are not a party in this dispute."));

            var result = await _controller.AddPhoto(1, dto);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task AddPhoto_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            var dto = new DisputeDTO.AddDisputePhotoDTO { PhotoUrl = "http://test.com/photo.jpg" };
            _disputeServiceMock
                .Setup(s => s.AddPhotoAsync(1, "user-1", dto.PhotoUrl, dto.Caption))
                .ThrowsAsync(new InvalidOperationException("Dispute is already resolved."));

            var result = await _controller.AddPhoto(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetAllOpen_ReturnsOk_WithOpenDisputes()
        {
            SetUser("admin-1", "Admin");
            var disputes = new List<DisputeDTO.DisputeSummaryDTO>
            {
                MakeDisputeSummary(1),
                MakeDisputeSummary(2)
            };
            _disputeServiceMock
                .Setup(s => s.GetAllOpenAsync())
                .ReturnsAsync(disputes);

            var result = await _controller.GetAllOpen();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<DisputeDTO.DisputeSummaryDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetAllOpen_ReturnsOk_WithEmptyList()
        {
            SetUser("admin-1", "Admin");
            _disputeServiceMock
                .Setup(s => s.GetAllOpenAsync())
                .ReturnsAsync(new List<DisputeDTO.DisputeSummaryDTO>());

            var result = await _controller.GetAllOpen();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<DisputeDTO.DisputeSummaryDTO>>(ok.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public async Task IssueVerdict_ReturnsOk_WithResolvedDispute()
        {
            SetUser("admin-1", "Admin");
            var dto = new DisputeDTO.AdminVerdictDTO
            {
                Verdict = "OwnerFavored",
                AdminNote = "Evidence clearly shows damage."
            };
            var detail = MakeDisputeDetail();
            _disputeServiceMock
                .Setup(s => s.IssueVerdictAsync(1, "admin-1", dto))
                .ReturnsAsync(detail);

            var result = await _controller.IssueVerdict(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<DisputeDTO.DisputeDetailDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
        }

        [Fact]
        public async Task IssueVerdict_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            var dto = new DisputeDTO.AdminVerdictDTO
            {
                Verdict = "BorrowerFavored",
                AdminNote = "Pre-existing damage."
            };
            _disputeServiceMock
                .Setup(s => s.IssueVerdictAsync(3, "admin-1", dto))
                .ReturnsAsync(MakeDisputeDetail());

            await _controller.IssueVerdict(3, dto);

            _disputeServiceMock.Verify(s => s.IssueVerdictAsync(3, "admin-1", dto), Times.Once);
        }

        [Fact]
        public async Task IssueVerdict_PartialDamage_WithCustomAmount_ReturnsOk()
        {
            SetUser("admin-1", "Admin");
            var dto = new DisputeDTO.AdminVerdictDTO
            {
                Verdict = "PartialDamage",
                CustomFineAmount = 250m,
                AdminNote = "Partial damage confirmed."
            };
            _disputeServiceMock
                .Setup(s => s.IssueVerdictAsync(1, "admin-1", dto))
                .ReturnsAsync(MakeDisputeDetail());

            var result = await _controller.IssueVerdict(1, dto);

            Assert.IsType<OkObjectResult>(result);
            _disputeServiceMock.Verify(s =>
                s.IssueVerdictAsync(1, "admin-1", dto), Times.Once);
        }

        [Fact]
        public async Task IssueVerdict_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            SetUser("admin-1", "Admin");
            var dto = new DisputeDTO.AdminVerdictDTO { Verdict = "OwnerFavored" };
            _disputeServiceMock
                .Setup(s => s.IssueVerdictAsync(999, "admin-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Dispute 999 not found."));

            var result = await _controller.IssueVerdict(999, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task IssueVerdict_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            SetUser("admin-1", "Admin");
            var dto = new DisputeDTO.AdminVerdictDTO { Verdict = "OwnerFavored" };
            _disputeServiceMock
                .Setup(s => s.IssueVerdictAsync(1, "admin-1", dto))
                .ThrowsAsync(new InvalidOperationException("Dispute is already resolved."));

            var result = await _controller.IssueVerdict(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task IssueVerdict_ServiceThrows_ArgumentException_ReturnsBadRequest()
        {
            SetUser("admin-1", "Admin");
            var dto = new DisputeDTO.AdminVerdictDTO { Verdict = "InvalidVerdict" };
            _disputeServiceMock
                .Setup(s => s.IssueVerdictAsync(1, "admin-1", dto))
                .ThrowsAsync(new ArgumentException("Invalid verdict value."));

            var result = await _controller.IssueVerdict(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}