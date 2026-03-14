using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace backend.Tests.Controllers
{
    public class VerificationControllerTests
    {
        private readonly Mock<IVerificationService> _verificationServiceMock;
        private readonly VerificationController _controller;

        public VerificationControllerTests()
        {
            _verificationServiceMock = new Mock<IVerificationService>();
            _controller = new VerificationController(_verificationServiceMock.Object);
        }

        private void SetUser(string userId, string? role = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            if (!string.IsNullOrEmpty(role))
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private static VerificationDTO.VerificationRequestResponseDTO MakeRequest(int id, string userId = "user-1")
        {
            return new VerificationDTO.VerificationRequestResponseDTO
            {
                Id = id,
                UserId = userId,
                UserName = "Test User",
                UserEmail = "test@example.com",
                DocumentUrl = "http://example.com/id.jpg",
                DocumentType = "Passport",
                Status = "Pending",
                SubmittedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task GetMyRequest_ReturnsOk_WithRequest()
        {
            SetUser("user-1");
            var request = MakeRequest(1, "user-1");

            _verificationServiceMock
                .Setup(s => s.GetUserRequestAsync("user-1"))
                .ReturnsAsync(request);

            var result = await _controller.GetMyRequest();

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<VerificationDTO.VerificationRequestResponseDTO>(ok.Value);
            Assert.Equal("user-1", data.UserId);
            Assert.Equal(1, data.Id);
        }

        [Fact]
        public async Task Submit_ReturnsOk_WhenUserIsNotAdmin()
        {
            SetUser("user-2"); 
            var dto = new VerificationDTO.CreateVerificationRequestDTO
            {
                DocumentUrl = "http://example.com/doc.jpg",
                DocumentType = "Passport"
            };

            var response = MakeRequest(2, "user-2");
            _verificationServiceMock
                .Setup(s => s.SubmitRequestAsync("user-2", dto))
                .ReturnsAsync(response);

            var result = await _controller.Submit(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<VerificationDTO.VerificationRequestResponseDTO>(ok.Value);
            Assert.Equal("user-2", data.UserId);
            Assert.Equal(2, data.Id);
        }

        [Fact]
        public async Task Submit_ThrowsUnauthorized_WhenUserIsAdmin()
        {
            SetUser("admin-1", "Admin");
            var dto = new VerificationDTO.CreateVerificationRequestDTO
            {
                DocumentUrl = "http://example.com/doc.jpg",
                DocumentType = "Passport"
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.Submit(dto));
        }

        [Fact]
        public async Task GetPendingRequests_ReturnsOk_WithRequests()
        {
            SetUser("admin-1", "Admin");
            var requests = new List<VerificationDTO.VerificationRequestResponseDTO>
            {
                MakeRequest(1),
                MakeRequest(2)
            };

            _verificationServiceMock
                .Setup(s => s.GetAllPendingAsync())
                .ReturnsAsync(requests);

            var result = await _controller.GetPendingRequests();

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<VerificationDTO.VerificationRequestResponseDTO>>(ok.Value);
            Assert.Equal(2, data.Count);
        }

        [Fact]
        public async Task Decide_ReturnsOk_WithRequest()
        {
            SetUser("admin-1", "Admin");
            var dto = new VerificationDTO.AdminVerificationDecisionDTO
            {
                IsApproved = true,
                AdminNote = "Looks good"
            };

            var request = MakeRequest(1);
            _verificationServiceMock
                .Setup(s => s.DecideAsync(1, "admin-1", dto))
                .ReturnsAsync(request);

            var result = await _controller.Decide(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<VerificationDTO.VerificationRequestResponseDTO>(ok.Value);
            Assert.Equal(1, data.Id);
        }
    }
}