using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class LoanMessageControllerTests
    {
        private readonly Mock<ILoanMessageService> _loanMessageServiceMock;
        private readonly LoanMessageController _controller;

        public LoanMessageControllerTests()
        {
            _loanMessageServiceMock = new Mock<ILoanMessageService>();
            _controller = new LoanMessageController(_loanMessageServiceMock.Object);
            SetUser("user-1");
        }

 
        private void SetUser(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private static ChatDTO.LoanMessageDTO.LoanMessageThreadDTO MakeThread(
            int loanId = 1) => new()
            {
                LoanId = loanId,
                ItemTitle = "Drill",
                OtherPartyName = "Other User",
                Messages = new List<ChatDTO.LoanMessageDTO.LoanMessageResponseDTO>
            {
                new()
                {
                    Id = 1,
                    LoanId = loanId,
                    SenderId = "user-1",
                    SenderName = "User 1",
                    Content = "Is the item available?",
                    IsRead = false,
                    SentAt = DateTime.UtcNow
                }
            }
            };

        private static ChatDTO.LoanMessageDTO.LoanMessageResponseDTO MakeMessageResponse(
        int id = 1,
        int loanId = 1) => new()
        {
            Id = id,
            LoanId = loanId,
            SenderId = "user-1",
            SenderName = "User 1",
            Content = "Hello!",
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        [Fact]
        public async Task GetThread_ReturnsOk_WithThread()
        {
            var thread = MakeThread(1);
            _loanMessageServiceMock
                .Setup(s => s.GetThreadAsync(1, "user-1"))
                .ReturnsAsync(thread);

            var result = await _controller.GetThread(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ChatDTO.LoanMessageDTO.LoanMessageThreadDTO>(ok.Value);
            Assert.Equal(1, returned.LoanId);
            Assert.Single(returned.Messages);
        }

        [Fact]
        public async Task GetThread_ReturnsOk_WithEmptyMessages()
        {
            var thread = new ChatDTO.LoanMessageDTO.LoanMessageThreadDTO
            {
                LoanId = 1,
                ItemTitle = "Drill",
                OtherPartyName = "Owner",
                Messages = new List<ChatDTO.LoanMessageDTO.LoanMessageResponseDTO>()
            };
            _loanMessageServiceMock
                .Setup(s => s.GetThreadAsync(1, "user-1"))
                .ReturnsAsync(thread);

            var result = await _controller.GetThread(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ChatDTO.LoanMessageDTO.LoanMessageThreadDTO>(ok.Value);
            Assert.Empty(returned.Messages);
        }

        [Fact]
        public async Task GetThread_CallsServiceWithCorrectArguments()
        {
            SetUser("specific-user");
            _loanMessageServiceMock
                .Setup(s => s.GetThreadAsync(3, "specific-user"))
                .ReturnsAsync(MakeThread(3));

            await _controller.GetThread(3);

            _loanMessageServiceMock.Verify(s =>
                s.GetThreadAsync(3, "specific-user"), Times.Once);
        }

        [Fact]
        public async Task GetThread_ServiceThrows_KeyNotFound_ExceptionPropagates()
        {
            _loanMessageServiceMock
                .Setup(s => s.GetThreadAsync(999, "user-1"))
                .ThrowsAsync(new KeyNotFoundException("Loan 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.GetThread(999));
        }

        [Fact]
        public async Task GetThread_ServiceThrows_Unauthorized_ExceptionPropagates()
        {
            _loanMessageServiceMock
                .Setup(s => s.GetThreadAsync(1, "user-1"))
                .ThrowsAsync(new UnauthorizedAccessException("You are not a participant."));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _controller.GetThread(1));
        }

  
        [Fact]
        public async Task Send_ReturnsOk_WithSentMessage()
        {
            var dto = new ChatDTO.LoanMessageDTO.SendLoanMessageDTO
            {
                LoanId = 1,
                Content = "Is the item ready?"
            };
            var response = MakeMessageResponse();
            _loanMessageServiceMock
                .Setup(s => s.SendAsync("user-1", dto))
                .ReturnsAsync(response);

            var result = await _controller.Send(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ChatDTO.LoanMessageDTO.LoanMessageResponseDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
            Assert.Equal("Hello!", returned.Content);
        }

        [Fact]
        public async Task Send_CallsServiceWithCorrectArguments()
        {
            var dto = new ChatDTO.LoanMessageDTO.SendLoanMessageDTO
            {
                LoanId = 2,
                Content = "When can I pick it up?"
            };
            _loanMessageServiceMock
                .Setup(s => s.SendAsync("user-1", dto))
                .ReturnsAsync(MakeMessageResponse());

            await _controller.Send(dto);

            _loanMessageServiceMock.Verify(s =>
                s.SendAsync("user-1", dto), Times.Once);
        }

        [Fact]
        public async Task Send_ServiceThrows_KeyNotFound_ExceptionPropagates()
        {
            var dto = new ChatDTO.LoanMessageDTO.SendLoanMessageDTO { LoanId = 999, Content = "Hello" };
            _loanMessageServiceMock
                .Setup(s => s.SendAsync("user-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Loan 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.Send(dto));
        }

        [Fact]
        public async Task Send_ServiceThrows_Unauthorized_ExceptionPropagates()
        {
            var dto = new ChatDTO.LoanMessageDTO.SendLoanMessageDTO { LoanId = 1, Content = "Hello" };
            _loanMessageServiceMock
                .Setup(s => s.SendAsync("user-1", dto))
                .ThrowsAsync(new UnauthorizedAccessException("You are not a participant in this loan."));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _controller.Send(dto));
        }

        [Fact]
        public async Task Send_ServiceThrows_InvalidOperation_ExceptionPropagates()
        {
            var dto = new ChatDTO.LoanMessageDTO.SendLoanMessageDTO { LoanId = 1, Content = "Hello" };
            _loanMessageServiceMock
                .Setup(s => s.SendAsync("user-1", dto))
                .ThrowsAsync(new InvalidOperationException("Cannot send messages on a completed loan."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.Send(dto));
        }

 
        [Fact]
        public async Task MarkAsRead_ReturnsNoContent()
        {
            _loanMessageServiceMock
                .Setup(s => s.MarkThreadAsReadAsync(1, "user-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.MarkAsRead(1);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task MarkAsRead_CallsServiceWithCorrectArguments()
        {
            SetUser("specific-user");
            _loanMessageServiceMock
                .Setup(s => s.MarkThreadAsReadAsync(3, "specific-user"))
                .Returns(Task.CompletedTask);

            await _controller.MarkAsRead(3);

            _loanMessageServiceMock.Verify(s =>
                s.MarkThreadAsReadAsync(3, "specific-user"), Times.Once);
        }

        [Fact]
        public async Task MarkAsRead_ServiceThrows_ExceptionPropagates()
        {
            _loanMessageServiceMock
                .Setup(s => s.MarkThreadAsReadAsync(999, "user-1"))
                .ThrowsAsync(new KeyNotFoundException("Loan 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.MarkAsRead(999));
        }
    }
}