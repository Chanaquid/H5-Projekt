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
    public class FineControllerTests
    {
        private readonly Mock<IFineService> _fineServiceMock;
        private readonly FineController _controller;

        public FineControllerTests()
        {
            _fineServiceMock = new Mock<IFineService>();
            _controller = new FineController(_fineServiceMock.Object);
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

        private static FineDTO.FineResponseDTO MakeFineResponse(
            int id = 1,
            string status = "Unpaid",
            string type = "Late") => new()
            {
                Id = id,
                LoanId = 10,
                ItemTitle = "Drill",
                Type = type,
                Status = status,
                Amount = 100m,
                ItemValueAtTimeOfFine = 500m,
                CreatedAt = DateTime.UtcNow
            };


        [Fact]
        public async Task GetMyFines_ReturnsOk_WithFines()
        {
            var fines = new List<FineDTO.FineResponseDTO>
            {
                MakeFineResponse(1),
                MakeFineResponse(2)
            };
            _fineServiceMock
                .Setup(s => s.GetUserFinesAsync("user-1"))
                .ReturnsAsync(fines);

            var result = await _controller.GetMyFines();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<FineDTO.FineResponseDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetMyFines_ReturnsOk_WithEmptyList()
        {
            _fineServiceMock
                .Setup(s => s.GetUserFinesAsync("user-1"))
                .ReturnsAsync(new List<FineDTO.FineResponseDTO>());

            var result = await _controller.GetMyFines();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<FineDTO.FineResponseDTO>>(ok.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public async Task GetMyFines_CallsServiceWithCorrectUserId()
        {
            SetUser("specific-user");
            _fineServiceMock
                .Setup(s => s.GetUserFinesAsync("specific-user"))
                .ReturnsAsync(new List<FineDTO.FineResponseDTO>());

            await _controller.GetMyFines();

            _fineServiceMock.Verify(s => s.GetUserFinesAsync("specific-user"), Times.Once);
        }

 
        [Fact]
        public async Task GetPendingVerification_ReturnsOk_WithFines()
        {
            SetUser("admin-1", "Admin");
            var fines = new List<FineDTO.FineResponseDTO>
            {
                MakeFineResponse(1, "PendingVerification"),
                MakeFineResponse(2, "PendingVerification")
            };
            _fineServiceMock
                .Setup(s => s.GetPendingVerificationAsync())
                .ReturnsAsync(fines);

            var result = await _controller.GetPendingVerification();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<FineDTO.FineResponseDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetPendingVerification_ReturnsOk_WithEmptyList()
        {
            SetUser("admin-1", "Admin");
            _fineServiceMock
                .Setup(s => s.GetPendingVerificationAsync())
                .ReturnsAsync(new List<FineDTO.FineResponseDTO>());

            var result = await _controller.GetPendingVerification();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<FineDTO.FineResponseDTO>>(ok.Value);
            Assert.Empty(returned);
        }


        [Fact]
        public async Task MarkAsPaid_ReturnsOk_WithUpdatedFine()
        {
            var dto = new FineDTO.PayFineDTO
            {
                FineId = 1,
                PaymentProofImageUrl = "http://test.com/proof.jpg",
                PaymentDescription = "Paid via MobilePay"
            };
            var response = MakeFineResponse(1, "PendingVerification");
            _fineServiceMock
                .Setup(s => s.MarkAsPaidAsync("user-1", dto))
                .ReturnsAsync(response);

            var result = await _controller.MarkAsPaid(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<FineDTO.FineResponseDTO>(ok.Value);
            Assert.Equal("PendingVerification", returned.Status);
        }

        [Fact]
        public async Task MarkAsPaid_CallsServiceWithCorrectArguments()
        {
            var dto = new FineDTO.PayFineDTO
            {
                FineId = 1,
                PaymentProofImageUrl = "http://test.com/proof.jpg",
                PaymentDescription = "Paid via bank transfer"
            };
            _fineServiceMock
                .Setup(s => s.MarkAsPaidAsync("user-1", dto))
                .ReturnsAsync(MakeFineResponse());

            await _controller.MarkAsPaid(dto);

            _fineServiceMock.Verify(s => s.MarkAsPaidAsync("user-1", dto), Times.Once);
        }

        [Fact]
        public async Task MarkAsPaid_ServiceThrows_ExceptionPropagates()
        {
            var dto = new FineDTO.PayFineDTO { FineId = 999 };
            _fineServiceMock
                .Setup(s => s.MarkAsPaidAsync("user-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Fine 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.MarkAsPaid(dto));
        }


        [Fact]
        public async Task GetAllUnpaid_ReturnsOk_WithUnpaidFines()
        {
            SetUser("admin-1", "Admin");
            var fines = new List<FineDTO.FineResponseDTO>
            {
                MakeFineResponse(1, "Unpaid"),
                MakeFineResponse(2, "Unpaid")
            };
            _fineServiceMock
                .Setup(s => s.GetAllUnpaidAsync())
                .ReturnsAsync(fines);

            var result = await _controller.GetAllUnpaid();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<FineDTO.FineResponseDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetAllUnpaid_ReturnsOk_WithEmptyList()
        {
            SetUser("admin-1", "Admin");
            _fineServiceMock
                .Setup(s => s.GetAllUnpaidAsync())
                .ReturnsAsync(new List<FineDTO.FineResponseDTO>());

            var result = await _controller.GetAllUnpaid();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<FineDTO.FineResponseDTO>>(ok.Value);
            Assert.Empty(returned);
        }

 
        [Fact]
        public async Task GetFinesByUser_ReturnsOk_WithFines()
        {
            SetUser("admin-1", "Admin");
            var fines = new List<FineDTO.FineResponseDTO>
            {
                MakeFineResponse(1),
                MakeFineResponse(2)
            };
            _fineServiceMock
                .Setup(s => s.GetUserFinesAsync("target-user"))
                .ReturnsAsync(fines);

            var result = await _controller.GetFinesByUser("target-user");

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<FineDTO.FineResponseDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetFinesByUser_CallsServiceWithCorrectUserId()
        {
            SetUser("admin-1", "Admin");
            _fineServiceMock
                .Setup(s => s.GetUserFinesAsync("target-user"))
                .ReturnsAsync(new List<FineDTO.FineResponseDTO>());

            await _controller.GetFinesByUser("target-user");

            _fineServiceMock.Verify(s => s.GetUserFinesAsync("target-user"), Times.Once);
        }

        [Fact]
        public async Task AdminIssueFine_ReturnsOk_WithIssuedFine()
        {
            SetUser("admin-1", "Admin");
            var dto = new FineDTO.AdminIssueFineDTO
            {
                UserId = "user-1",
                LoanId = 10,
                Amount = 200m,
                Reason = "Manual fine for misconduct."
            };
            var response = MakeFineResponse(1, "Unpaid", "Custom");
            _fineServiceMock
                .Setup(s => s.AdminIssueFineAsync(dto))
                .ReturnsAsync(response);

            var result = await _controller.AdminIssueFine(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<FineDTO.FineResponseDTO>(ok.Value);
            Assert.Equal("Custom", returned.Type);
        }

        [Fact]
        public async Task AdminIssueFine_CallsServiceWithCorrectDto()
        {
            SetUser("admin-1", "Admin");
            var dto = new FineDTO.AdminIssueFineDTO
            {
                UserId = "user-1",
                Amount = 150m,
                Reason = "Misconduct"
            };
            _fineServiceMock
                .Setup(s => s.AdminIssueFineAsync(dto))
                .ReturnsAsync(MakeFineResponse());

            await _controller.AdminIssueFine(dto);

            _fineServiceMock.Verify(s => s.AdminIssueFineAsync(dto), Times.Once);
        }

        [Fact]
        public async Task AdminIssueFine_ServiceThrows_ExceptionPropagates()
        {
            SetUser("admin-1", "Admin");
            var dto = new FineDTO.AdminIssueFineDTO { UserId = "nonexistent" };
            _fineServiceMock
                .Setup(s => s.AdminIssueFineAsync(dto))
                .ThrowsAsync(new KeyNotFoundException("User not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.AdminIssueFine(dto));
        }

        [Fact]
        public async Task ConfirmPayment_Approve_ReturnsOk_WithPaidFine()
        {
            SetUser("admin-1", "Admin");
            var dto = new FineDTO.AdminFineVerificationDTO
            {
                FineId = 1,
                IsApproved = true
            };
            var response = MakeFineResponse(1, "Paid");
            _fineServiceMock
                .Setup(s => s.AdminConfirmPaymentAsync("admin-1", dto))
                .ReturnsAsync(response);

            var result = await _controller.ConfirmPayment(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<FineDTO.FineResponseDTO>(ok.Value);
            Assert.Equal("Paid", returned.Status);
        }

        [Fact]
        public async Task ConfirmPayment_Reject_ReturnsOk_WithRejectedFine()
        {
            SetUser("admin-1", "Admin");
            var dto = new FineDTO.AdminFineVerificationDTO
            {
                FineId = 1,
                IsApproved = false,
                RejectionReason = "Proof image is unclear."
            };
            var response = MakeFineResponse(1, "Rejected");
            _fineServiceMock
                .Setup(s => s.AdminConfirmPaymentAsync("admin-1", dto))
                .ReturnsAsync(response);

            var result = await _controller.ConfirmPayment(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<FineDTO.FineResponseDTO>(ok.Value);
            Assert.Equal("Rejected", returned.Status);
        }

        [Fact]
        public async Task ConfirmPayment_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            var dto = new FineDTO.AdminFineVerificationDTO { FineId = 5, IsApproved = true };
            _fineServiceMock
                .Setup(s => s.AdminConfirmPaymentAsync("admin-1", dto))
                .ReturnsAsync(MakeFineResponse());

            await _controller.ConfirmPayment(dto);

            _fineServiceMock.Verify(s => s.AdminConfirmPaymentAsync("admin-1", dto), Times.Once);
        }

        [Fact]
        public async Task ConfirmPayment_ServiceThrows_ExceptionPropagates()
        {
            SetUser("admin-1", "Admin");
            var dto = new FineDTO.AdminFineVerificationDTO { FineId = 999, IsApproved = true };
            _fineServiceMock
                .Setup(s => s.AdminConfirmPaymentAsync("admin-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Fine 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.ConfirmPayment(dto));
        }

        [Fact]
        public async Task AdminUpdateFine_ReturnsOk_WithUpdatedFine()
        {
            SetUser("admin-1", "Admin");
            var dto = new FineDTO.AdminUpdateFineDTO
            {
                Amount = 150m,
                Reason = "Adjusted amount",
                Status = FineStatus.Unpaid
            };
            var response = MakeFineResponse(1, "Unpaid");
            _fineServiceMock
                .Setup(s => s.AdminUpdateFineAsync(1, dto))
                .ReturnsAsync(response);

            var result = await _controller.AdminUpdateFine(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<FineDTO.FineResponseDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
        }

        [Fact]
        public async Task AdminUpdateFine_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            var dto = new FineDTO.AdminUpdateFineDTO { Amount = 200m };
            _fineServiceMock
                .Setup(s => s.AdminUpdateFineAsync(3, dto))
                .ReturnsAsync(MakeFineResponse());

            await _controller.AdminUpdateFine(3, dto);

            _fineServiceMock.Verify(s => s.AdminUpdateFineAsync(3, dto), Times.Once);
        }

        [Fact]
        public async Task AdminUpdateFine_ServiceThrows_ExceptionPropagates()
        {
            SetUser("admin-1", "Admin");
            var dto = new FineDTO.AdminUpdateFineDTO { Amount = 100m };
            _fineServiceMock
                .Setup(s => s.AdminUpdateFineAsync(999, dto))
                .ThrowsAsync(new KeyNotFoundException("Fine 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.AdminUpdateFine(999, dto));
        }
    }
}