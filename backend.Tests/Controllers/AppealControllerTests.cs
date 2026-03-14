using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace backend.Tests.Controllers
{
    public class AppealControllerTests
    {
        private readonly Mock<IAppealService> _appealServiceMock;
        private readonly AppealController _appealController;


        public AppealControllerTests()
        {
            _appealServiceMock = new Mock<IAppealService>();
            _appealController = new AppealController(_appealServiceMock.Object );
            SetUser("user-1", "User");
        }


        [Fact]
        public async Task GetMyAppeal_ReturnsOk_WithAppeal()
        {
            SetUser("user-1", "User");

            var appeals = new List<AppealDTO.AppealResponseDTO>
            {
                MakeAppealResponse(1),
                MakeAppealResponse(2)
            };

            _appealServiceMock.Setup(x => x.GetMyAppealsAsync(It.IsAny<string>()))
                .ReturnsAsync(appeals);


            var response = await _appealController.GetMyAppeals();

            var okResponse = Assert.IsType<OkObjectResult>(response);
            var result = Assert.IsType<List<AppealDTO.AppealResponseDTO>>(okResponse.Value);

            Assert.Equal(2, result.Count);





        }

        [Fact]
        public async Task GetMyAppeals_ReturnsOk_WithEmptyList()
        {
            _appealServiceMock
                .Setup(s => s.GetMyAppealsAsync("user-1"))
                .ReturnsAsync(new List<AppealDTO.AppealResponseDTO>());

            var result = await _appealController.GetMyAppeals();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<AppealDTO.AppealResponseDTO>>(ok.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public async Task GetMyAppeals_CallsServiceWithCorrectUserId()
        {
            _appealServiceMock
                .Setup(s => s.GetMyAppealsAsync("specific-user"))
                .ReturnsAsync(new List<AppealDTO.AppealResponseDTO>());

            await _appealController.GetMyAppeals();

            _appealServiceMock.Verify(s => s.GetMyAppealsAsync("specific-user"), Times.Once);
        }

        [Fact]
        public async Task CreateScoreAppeal_ReturnsOk_WithCreatedAppeal()
        {
            var dto = new AppealDTO.CreateScoreAppealDTO { Message = "I deserve a second chance." };
            var response = MakeAppealResponse();
            _appealServiceMock
                .Setup(s => s.CreateScoreAppealAsync("user-1", dto))
                .ReturnsAsync(response);

            var result = await _appealController.CreateScoreAppeal(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<AppealDTO.AppealResponseDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
            Assert.Equal("Pending", returned.Status);
        }

        [Fact]
        public async Task CreateScoreAppeal_CallsServiceWithCorrectArguments()
        {
            var dto = new AppealDTO.CreateScoreAppealDTO { Message = "Please help." };
            _appealServiceMock
                .Setup(s => s.CreateScoreAppealAsync("user-1", dto))
                .ReturnsAsync(MakeAppealResponse());

            await _appealController.CreateScoreAppeal(dto);

            _appealServiceMock.Verify(s =>
                s.CreateScoreAppealAsync("user-1", dto), Times.Once);
        }

        [Fact]
        public async Task CreateScoreAppeal_ServiceThrows_ExceptionPropagates()
        {
            var dto = new AppealDTO.CreateScoreAppealDTO { Message = "Help." };
            _appealServiceMock
                .Setup(s => s.CreateScoreAppealAsync("user-1", dto))
                .ThrowsAsync(new InvalidOperationException(
                    "Your score is 20 or above. You do not need a score appeal."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _appealController.CreateScoreAppeal(dto));
        }

        [Fact]
        public async Task CreateFineAppeal_ReturnsOk_WithCreatedAppeal()
        {
            var dto = new AppealDTO.CreateFineAppealDTO { FineId = 10, Message = "This fine is unfair." };
            var response = MakeAppealResponse(appealType: "Fine");
            _appealServiceMock
                .Setup(s => s.CreateFineAppealAsync("user-1", dto))
                .ReturnsAsync(response);

            var result = await _appealController.CreateFineAppeal(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<AppealDTO.AppealResponseDTO>(ok.Value);
            Assert.Equal("Fine", returned.AppealType);
        }

        [Fact]
        public async Task CreateFineAppeal_CallsServiceWithCorrectArguments()
        {
            var dto = new AppealDTO.CreateFineAppealDTO { FineId = 5, Message = "Dispute." };
            _appealServiceMock
                .Setup(s => s.CreateFineAppealAsync("user-1", dto))
                .ReturnsAsync(MakeAppealResponse());

            await _appealController.CreateFineAppeal(dto);

            _appealServiceMock.Verify(s =>
                s.CreateFineAppealAsync("user-1", dto), Times.Once);
        }

        [Fact]
        public async Task CreateFineAppeal_ServiceThrows_ExceptionPropagates()
        {
            var dto = new AppealDTO.CreateFineAppealDTO { FineId = 99, Message = "Help." };
            _appealServiceMock
                .Setup(s => s.CreateFineAppealAsync("user-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Fine not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _appealController.CreateFineAppeal(dto));
        }

        [Fact]
        public async Task GetPendingAppeals_ReturnsOk_WithPendingAppeals()
        {
            SetUser("admin-1", "Admin");
            var appeals = new List<AppealDTO.AppealResponseDTO>
            {
                MakeAppealResponse(1),
                MakeAppealResponse(2)
            };
            _appealServiceMock
                .Setup(s => s.GetAllPendingAsync())
                .ReturnsAsync(appeals);

            var result = await _appealController.GetPendingAppeals();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<AppealDTO.AppealResponseDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetPendingAppeals_ReturnsOk_WithEmptyList()
        {
            SetUser("admin-1", "Admin");
            _appealServiceMock
                .Setup(s => s.GetAllPendingAsync())
                .ReturnsAsync(new List<AppealDTO.AppealResponseDTO>());

            var result = await _appealController.GetPendingAppeals();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<AppealDTO.AppealResponseDTO>>(ok.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public async Task DecideScoreAppeal_Approve_ReturnsOk()
        {
            SetUser("admin-1", "Admin");
            var dto = new AppealDTO.AdminScoreAppealDecisionDTO
            {
                IsApproved = true,
                NewScore = 20
            };
            var response = MakeAppealResponse(status: "Approved");
            _appealServiceMock
                .Setup(s => s.DecideScoreAppealAsync(1, "admin-1", dto))
                .ReturnsAsync(response);

            var result = await _appealController.DecideScoreAppeal(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<AppealDTO.AppealResponseDTO>(ok.Value);
            Assert.Equal("Approved", returned.Status);
        }

        [Fact]
        public async Task DecideScoreAppeal_Reject_ReturnsOk()
        {
            SetUser("admin-1", "Admin");
            var dto = new AppealDTO.AdminScoreAppealDecisionDTO
            {
                IsApproved = false,
                AdminNote = "Insufficient explanation."
            };
            var response = MakeAppealResponse(status: "Rejected");
            _appealServiceMock
                .Setup(s => s.DecideScoreAppealAsync(1, "admin-1", dto))
                .ReturnsAsync(response);

            var result = await _appealController.DecideScoreAppeal(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<AppealDTO.AppealResponseDTO>(ok.Value);
            Assert.Equal("Rejected", returned.Status);
        }

        [Fact]
        public async Task DecideScoreAppeal_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            var dto = new AppealDTO.AdminScoreAppealDecisionDTO { IsApproved = true };
            _appealServiceMock
                .Setup(s => s.DecideScoreAppealAsync(5, "admin-1", dto))
                .ReturnsAsync(MakeAppealResponse());

            await _appealController.DecideScoreAppeal(5, dto);

            _appealServiceMock.Verify(s =>
                s.DecideScoreAppealAsync(5, "admin-1", dto), Times.Once);
        }

        [Fact]
        public async Task DecideScoreAppeal_ServiceThrows_ExceptionPropagates()
        {
            SetUser("admin-1", "Admin");
            var dto = new AppealDTO.AdminScoreAppealDecisionDTO { IsApproved = true };
            _appealServiceMock
                .Setup(s => s.DecideScoreAppealAsync(999, "admin-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Appeal 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _appealController.DecideScoreAppeal(999, dto));
        }

 
        [Fact]
        public async Task DecideFineAppeal_Approve_Waive_ReturnsOk()
        {
            SetUser("admin-1", "Admin");
            var dto = new AppealDTO.AdminFineAppealDecisionDTO
            {
                IsApproved = true,
                Resolution = FineAppealResolution.Waive
            };
            var response = MakeAppealResponse(status: "Approved", appealType: "Fine");
            _appealServiceMock
                .Setup(s => s.DecideFineAppealAsync(1, "admin-1", dto))
                .ReturnsAsync(response);

            var result = await _appealController.DecideFineAppeal(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<AppealDTO.AppealResponseDTO>(ok.Value);
            Assert.Equal("Approved", returned.Status);
        }

        [Fact]
        public async Task DecideFineAppeal_Approve_Custom_ReturnsOk()
        {
            SetUser("admin-1", "Admin");
            var dto = new AppealDTO.AdminFineAppealDecisionDTO
            {
                IsApproved = true,
                Resolution = FineAppealResolution.Custom,
                CustomFineAmount = 150m
            };
            var response = new AppealDTO.AppealResponseDTO
            {
                Id = 1,
                Status = "Approved",
                AppealType = "Fine",
                FineResolution = "Custom",
                CustomFineAmount = 150m,
                CreatedAt = DateTime.UtcNow
            };
            _appealServiceMock
                .Setup(s => s.DecideFineAppealAsync(1, "admin-1", dto))
                .ReturnsAsync(response);

            var result = await _appealController.DecideFineAppeal(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<AppealDTO.AppealResponseDTO>(ok.Value);
            Assert.Equal(150m, returned.CustomFineAmount);
        }

        [Fact]
        public async Task DecideFineAppeal_Reject_ReturnsOk()
        {
            SetUser("admin-1", "Admin");
            var dto = new AppealDTO.AdminFineAppealDecisionDTO
            {
                IsApproved = false,
                AdminNote = "Fine was correctly issued."
            };
            var response = MakeAppealResponse(status: "Rejected", appealType: "Fine");
            _appealServiceMock
                .Setup(s => s.DecideFineAppealAsync(1, "admin-1", dto))
                .ReturnsAsync(response);

            var result = await _appealController.DecideFineAppeal(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<AppealDTO.AppealResponseDTO>(ok.Value);
            Assert.Equal("Rejected", returned.Status);
        }

        [Fact]
        public async Task DecideFineAppeal_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            var dto = new AppealDTO.AdminFineAppealDecisionDTO
            {
                IsApproved = true,
                Resolution = FineAppealResolution.HalfDamage
            };
            _appealServiceMock
                .Setup(s => s.DecideFineAppealAsync(3, "admin-1", dto))
                .ReturnsAsync(MakeAppealResponse());

            await _appealController.DecideFineAppeal(3, dto);

            _appealServiceMock.Verify(s =>
                s.DecideFineAppealAsync(3, "admin-1", dto), Times.Once);
        }

        [Fact]
        public async Task DecideFineAppeal_ServiceThrows_ExceptionPropagates()
        {
            SetUser("admin-1", "Admin");
            var dto = new AppealDTO.AdminFineAppealDecisionDTO { IsApproved = true };
            _appealServiceMock
                .Setup(s => s.DecideFineAppealAsync(999, "admin-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Appeal 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _appealController.DecideFineAppeal(999, dto));
        }



        private void SetUser(string userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            _appealController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private static AppealDTO.AppealResponseDTO MakeAppealResponse(
            int id = 1,
            string status = "Pending",
            string appealType = "Score") => new()
            {
                Id = id,
                UserId = "user-1",
                UserName = "Test User",
                UserScore = 15,
                AppealType = appealType,
                Message = "Please restore my score.",
                Status = status,
                CreatedAt = DateTime.UtcNow
            };






    }
}
