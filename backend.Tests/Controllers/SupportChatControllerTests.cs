using backend.Controllers;
using backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using static backend.DTOs.ChatDTO;

namespace backend.Tests.Controllers
{
    public class SupportChatControllerTests
    {
        private readonly Mock<ISupportChatService> _supportChatServiceMock;
        private readonly SupportChatController _controller;

        public SupportChatControllerTests()
        {
            _supportChatServiceMock = new Mock<ISupportChatService>();
            _controller = new SupportChatController(_supportChatServiceMock.Object);
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

        private static SupportChatDTO.SupportThreadDetailDTO MakeThreadDetail(
            int id = 1,
            string status = "Open") => new()
            {
                Id = id,
                UserId = "user-1",
                UserName = "User 1",
                Status = status,
                CreatedAt = DateTime.UtcNow,
                Messages = new List<SupportChatDTO.SupportMessageResponseDTO>()
            };

        private static SupportChatDTO.SupportThreadSummaryDTO MakeThreadSummary(
            int id = 1,
            string status = "Open") => new()
            {
                Id = id,
                UserId = "user-1",
                UserName = "User 1",
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

        private static SupportChatDTO.SupportMessageResponseDTO MakeMessageResponse(
            int id = 1,
            int threadId = 1) => new()
            {
                Id = id,
                SupportThreadId = threadId,
                SenderId = "user-1",
                SenderName = "User 1",
                Content = "Hello, I need help.",
                IsRead = false,
                IsAdminSender = false,
                SentAt = DateTime.UtcNow
            };

        [Fact]
        public async Task GetMyThreads_ReturnsOk_WithThreads()
        {
            var threads = new List<SupportChatDTO.SupportThreadSummaryDTO>
            {
                MakeThreadSummary(1),
                MakeThreadSummary(2)
            };
            _supportChatServiceMock
                .Setup(s => s.GetMyThreadsAsync("user-1"))
                .ReturnsAsync(threads);

            var result = await _controller.GetMyThreads();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<SupportChatDTO.SupportThreadSummaryDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetMyThreads_ReturnsOk_WithEmptyList()
        {
            _supportChatServiceMock
                .Setup(s => s.GetMyThreadsAsync("user-1"))
                .ReturnsAsync(new List<SupportChatDTO.SupportThreadSummaryDTO>());

            var result = await _controller.GetMyThreads();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Empty((List<SupportChatDTO.SupportThreadSummaryDTO>)ok.Value!);
        }

        [Fact]
        public async Task GetMyThreads_CallsServiceWithCorrectUserId()
        {
            SetUser("specific-user");
            _supportChatServiceMock
                .Setup(s => s.GetMyThreadsAsync("specific-user"))
                .ReturnsAsync(new List<SupportChatDTO.SupportThreadSummaryDTO>());

            await _controller.GetMyThreads();

            _supportChatServiceMock.Verify(s =>
                s.GetMyThreadsAsync("specific-user"), Times.Once);
        }

        [Fact]
        public async Task GetThread_ReturnsOk_WithThread()
        {
            var thread = MakeThreadDetail(1);
            _supportChatServiceMock
                .Setup(s => s.GetThreadAsync(1, "user-1", false))
                .ReturnsAsync(thread);

            var result = await _controller.GetThread(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<SupportChatDTO.SupportThreadDetailDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
        }

        [Fact]
        public async Task GetThread_AsAdmin_PassesIsAdminTrue()
        {
            SetUser("admin-1", "Admin");
            _supportChatServiceMock
                .Setup(s => s.GetThreadAsync(1, "admin-1", true))
                .ReturnsAsync(MakeThreadDetail());

            await _controller.GetThread(1);

            _supportChatServiceMock.Verify(s =>
                s.GetThreadAsync(1, "admin-1", true), Times.Once);
        }

        [Fact]
        public async Task GetThread_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            _supportChatServiceMock
                .Setup(s => s.GetThreadAsync(999, "user-1", false))
                .ThrowsAsync(new KeyNotFoundException("Thread 999 not found."));

            var result = await _controller.GetThread(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetThread_ServiceThrows_Unauthorized_ReturnsForbid()
        {
            _supportChatServiceMock
                .Setup(s => s.GetThreadAsync(1, "user-1", false))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied."));

            var result = await _controller.GetThread(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CreateThread_ReturnsOk_WithCreatedThread()
        {
            var dto = new SupportChatDTO.CreateSupportThreadDTO
            {
                InitialMessage = "I need help with my account."
            };
            var thread = MakeThreadDetail();
            _supportChatServiceMock
                .Setup(s => s.CreateThreadAsync("user-1", dto))
                .ReturnsAsync(thread);

            var result = await _controller.CreateThread(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<SupportChatDTO.SupportThreadDetailDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
        }

        [Fact]
        public async Task CreateThread_CallsServiceWithCorrectArguments()
        {
            var dto = new SupportChatDTO.CreateSupportThreadDTO
            {
                InitialMessage = "Help!"
            };
            _supportChatServiceMock
                .Setup(s => s.CreateThreadAsync("user-1", dto))
                .ReturnsAsync(MakeThreadDetail());

            await _controller.CreateThread(dto);

            _supportChatServiceMock.Verify(s =>
                s.CreateThreadAsync("user-1", dto), Times.Once);
        }

        [Fact]
        public async Task CreateThread_ServiceThrows_ExceptionPropagates()
        {
            var dto = new SupportChatDTO.CreateSupportThreadDTO { InitialMessage = "Help!" };
            _supportChatServiceMock
                .Setup(s => s.CreateThreadAsync("user-1", dto))
                .ThrowsAsync(new InvalidOperationException("Service error."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.CreateThread(dto));
        }

        [Fact]
        public async Task SendMessage_ReturnsOk_WithSentMessage()
        {
            var dto = new SupportChatDTO.SendSupportMessageDTO
            {
                SupportThreadId = 1,
                Content = "Any updates?"
            };
            var response = MakeMessageResponse();
            _supportChatServiceMock
                .Setup(s => s.SendMessageAsync("user-1", dto))
                .ReturnsAsync(response);

            var result = await _controller.SendMessage(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<SupportChatDTO.SupportMessageResponseDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
        }

        [Fact]
        public async Task SendMessage_CallsServiceWithCorrectArguments()
        {
            var dto = new SupportChatDTO.SendSupportMessageDTO
            {
                SupportThreadId = 2,
                Content = "Hello!"
            };
            _supportChatServiceMock
                .Setup(s => s.SendMessageAsync("user-1", dto))
                .ReturnsAsync(MakeMessageResponse());

            await _controller.SendMessage(dto);

            _supportChatServiceMock.Verify(s =>
                s.SendMessageAsync("user-1", dto), Times.Once);
        }

        [Fact]
        public async Task SendMessage_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            var dto = new SupportChatDTO.SendSupportMessageDTO { SupportThreadId = 999 };
            _supportChatServiceMock
                .Setup(s => s.SendMessageAsync("user-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Thread 999 not found."));

            var result = await _controller.SendMessage(dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task SendMessage_ServiceThrows_Unauthorized_ReturnsForbid()
        {
            var dto = new SupportChatDTO.SendSupportMessageDTO { SupportThreadId = 1 };
            _supportChatServiceMock
                .Setup(s => s.SendMessageAsync("user-1", dto))
                .ThrowsAsync(new UnauthorizedAccessException("Not your thread."));

            var result = await _controller.SendMessage(dto);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task SendMessage_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            var dto = new SupportChatDTO.SendSupportMessageDTO { SupportThreadId = 1 };
            _supportChatServiceMock
                .Setup(s => s.SendMessageAsync("user-1", dto))
                .ThrowsAsync(new InvalidOperationException("Thread is closed."));

            var result = await _controller.SendMessage(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task MarkRead_ReturnsOk()
        {
            _supportChatServiceMock
                .Setup(s => s.MarkReadAsync(1, "user-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.MarkRead(1);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task MarkRead_CallsServiceWithCorrectArguments()
        {
            SetUser("specific-user");
            _supportChatServiceMock
                .Setup(s => s.MarkReadAsync(3, "specific-user"))
                .Returns(Task.CompletedTask);

            await _controller.MarkRead(3);

            _supportChatServiceMock.Verify(s =>
                s.MarkReadAsync(3, "specific-user"), Times.Once);
        }

        [Fact]
        public async Task MarkRead_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            _supportChatServiceMock
                .Setup(s => s.MarkReadAsync(999, "user-1"))
                .ThrowsAsync(new KeyNotFoundException("Thread 999 not found."));

            var result = await _controller.MarkRead(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task MarkRead_ServiceThrows_Unauthorized_ReturnsForbid()
        {
            _supportChatServiceMock
                .Setup(s => s.MarkReadAsync(1, "user-1"))
                .ThrowsAsync(new UnauthorizedAccessException("Not your thread."));

            var result = await _controller.MarkRead(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetAllOpen_ReturnsOk_WithOpenThreads()
        {
            SetUser("admin-1", "Admin");
            var threads = new List<SupportChatDTO.SupportThreadSummaryDTO>
            {
                MakeThreadSummary(1, "Open"),
                MakeThreadSummary(2, "Claimed")
            };
            _supportChatServiceMock
                .Setup(s => s.GetAllOpenThreadsAsync())
                .ReturnsAsync(threads);

            var result = await _controller.GetAllOpen();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<SupportChatDTO.SupportThreadSummaryDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetAllOpen_ReturnsOk_WithEmptyList()
        {
            SetUser("admin-1", "Admin");
            _supportChatServiceMock
                .Setup(s => s.GetAllOpenThreadsAsync())
                .ReturnsAsync(new List<SupportChatDTO.SupportThreadSummaryDTO>());

            var result = await _controller.GetAllOpen();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Empty((List<SupportChatDTO.SupportThreadSummaryDTO>)ok.Value!);
        }

        [Fact]
        public async Task ClaimThread_ReturnsOk_WithClaimedThread()
        {
            SetUser("admin-1", "Admin");
            var thread = MakeThreadDetail(1, "Claimed");
            _supportChatServiceMock
                .Setup(s => s.ClaimThreadAsync(1, "admin-1"))
                .ReturnsAsync(thread);

            var result = await _controller.ClaimThread(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<SupportChatDTO.SupportThreadDetailDTO>(ok.Value);
            Assert.Equal("Claimed", returned.Status);
        }

        [Fact]
        public async Task ClaimThread_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            _supportChatServiceMock
                .Setup(s => s.ClaimThreadAsync(3, "admin-1"))
                .ReturnsAsync(MakeThreadDetail());

            await _controller.ClaimThread(3);

            _supportChatServiceMock.Verify(s =>
                s.ClaimThreadAsync(3, "admin-1"), Times.Once);
        }

        [Fact]
        public async Task ClaimThread_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            SetUser("admin-1", "Admin");
            _supportChatServiceMock
                .Setup(s => s.ClaimThreadAsync(999, "admin-1"))
                .ThrowsAsync(new KeyNotFoundException("Thread 999 not found."));

            var result = await _controller.ClaimThread(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ClaimThread_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            SetUser("admin-1", "Admin");
            _supportChatServiceMock
                .Setup(s => s.ClaimThreadAsync(1, "admin-1"))
                .ThrowsAsync(new InvalidOperationException("Thread is already claimed."));

            var result = await _controller.ClaimThread(1);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CloseThread_ReturnsOk_WithClosedThread()
        {
            SetUser("admin-1", "Admin");
            var thread = MakeThreadDetail(1, "Closed");
            _supportChatServiceMock
                .Setup(s => s.CloseThreadAsync(1, "admin-1"))
                .ReturnsAsync(thread);

            var result = await _controller.CloseThread(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<SupportChatDTO.SupportThreadDetailDTO>(ok.Value);
            Assert.Equal("Closed", returned.Status);
        }

        [Fact]
        public async Task CloseThread_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            _supportChatServiceMock
                .Setup(s => s.CloseThreadAsync(2, "admin-1"))
                .ReturnsAsync(MakeThreadDetail());

            await _controller.CloseThread(2);

            _supportChatServiceMock.Verify(s =>
                s.CloseThreadAsync(2, "admin-1"), Times.Once);
        }

        [Fact]
        public async Task CloseThread_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            SetUser("admin-1", "Admin");
            _supportChatServiceMock
                .Setup(s => s.CloseThreadAsync(999, "admin-1"))
                .ThrowsAsync(new KeyNotFoundException("Thread 999 not found."));

            var result = await _controller.CloseThread(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CloseThread_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            SetUser("admin-1", "Admin");
            _supportChatServiceMock
                .Setup(s => s.CloseThreadAsync(1, "admin-1"))
                .ThrowsAsync(new InvalidOperationException("Thread is already closed."));

            var result = await _controller.CloseThread(1);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ReopenThread_ReturnsOk_WithReopenedThread()
        {
            SetUser("admin-1", "Admin");
            var thread = MakeThreadDetail(1, "Open");
            _supportChatServiceMock
                .Setup(s => s.ReopenThreadAsync(1, "admin-1"))
                .ReturnsAsync(thread);

            var result = await _controller.ReopenThread(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<SupportChatDTO.SupportThreadDetailDTO>(ok.Value);
            Assert.Equal("Open", returned.Status);
        }

        [Fact]
        public async Task ReopenThread_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            _supportChatServiceMock
                .Setup(s => s.ReopenThreadAsync(4, "admin-1"))
                .ReturnsAsync(MakeThreadDetail());

            await _controller.ReopenThread(4);

            _supportChatServiceMock.Verify(s =>
                s.ReopenThreadAsync(4, "admin-1"), Times.Once);
        }

        [Fact]
        public async Task ReopenThread_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            SetUser("admin-1", "Admin");
            _supportChatServiceMock
                .Setup(s => s.ReopenThreadAsync(999, "admin-1"))
                .ThrowsAsync(new KeyNotFoundException("Thread 999 not found."));

            var result = await _controller.ReopenThread(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ReopenThread_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            SetUser("admin-1", "Admin");
            _supportChatServiceMock
                .Setup(s => s.ReopenThreadAsync(1, "admin-1"))
                .ThrowsAsync(new InvalidOperationException("Thread is not closed."));

            var result = await _controller.ReopenThread(1);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}