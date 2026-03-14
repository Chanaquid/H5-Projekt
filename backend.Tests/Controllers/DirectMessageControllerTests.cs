using backend.Controllers;
using backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using static backend.DTOs.ChatDTO;

namespace backend.Tests.Controllers
{
    public class DirectMessageControllerTests
    {
        private readonly Mock<IDirectMessageService> _directMessageServiceMock;
        private readonly DirectMessageController _controller;

        public DirectMessageControllerTests()
        {
            _directMessageServiceMock = new Mock<IDirectMessageService>();
            _controller = new DirectMessageController(_directMessageServiceMock.Object);
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

        private static DirectMessageDTO.DirectConversationSummaryDTO MakeConversationSummary(
        int id = 1,
        string otherUserName = "Other User") => new()
        {
            Id = id,
            OtherUserId = "user-2",
            OtherUserName = otherUserName,
            LastMessageContent = "Hello!",
            LastMessageAt = DateTime.UtcNow,
            UnreadCount = 0,
            IsHidden = false,
            CreatedAt = DateTime.UtcNow
        };

        private static DirectMessageDTO.DirectMessageThreadDTO MakeThread(
        int conversationId = 1) => new()
        {
            ConversationId = conversationId,
            OtherUserId = "user-2",
            OtherUserName = "Other User",
            Messages = new List<DirectMessageDTO.DirectMessageResponseDTO>
        {
            new()
            {
                Id = 1,
                ConversationId = conversationId,
                SenderId = "user-1",
                SenderName = "User 1",
                Content = "Hello!",
                IsRead = false,
                SentAt = DateTime.UtcNow
            }
        }
        };

        private static DirectMessageDTO.DirectMessageResponseDTO MakeMessageResponse(
        int id = 1,
        int conversationId = 1) => new()
        {
            Id = id,
            ConversationId = conversationId,
            SenderId = "user-1",
            SenderName = "User 1",
            Content = "Hello!",
            IsRead = false,
            SentAt = DateTime.UtcNow
        };


        [Fact]
        public async Task GetInbox_ReturnsOk_WithConversations()
        {
            var inbox = new List<DirectMessageDTO.DirectConversationSummaryDTO>
            {
                MakeConversationSummary(1),
                MakeConversationSummary(2)
            };
            _directMessageServiceMock
                .Setup(s => s.GetInboxAsync("user-1"))
                .ReturnsAsync(inbox);

            var result = await _controller.GetInbox();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<DirectMessageDTO.DirectConversationSummaryDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetInbox_ReturnsOk_WithEmptyList()
        {
            _directMessageServiceMock
                .Setup(s => s.GetInboxAsync("user-1"))
                .ReturnsAsync(new List<DirectMessageDTO.DirectConversationSummaryDTO>());

            var result = await _controller.GetInbox();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<DirectMessageDTO.DirectConversationSummaryDTO>>(ok.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public async Task GetInbox_CallsServiceWithCorrectUserId()
        {
            SetUser("specific-user");
            _directMessageServiceMock
                .Setup(s => s.GetInboxAsync("specific-user"))
                .ReturnsAsync(new List<DirectMessageDTO.DirectConversationSummaryDTO>());

            await _controller.GetInbox();

            _directMessageServiceMock.Verify(s => s.GetInboxAsync("specific-user"), Times.Once);
        }

        [Fact]
        public async Task GetInbox_ServiceThrows_ExceptionPropagates()
        {
            _directMessageServiceMock
                .Setup(s => s.GetInboxAsync("user-1"))
                .ThrowsAsync(new InvalidOperationException("Service error."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.GetInbox());
        }

        [Fact]
        public async Task GetThread_ReturnsOk_WithThread()
        {
            var thread = MakeThread(conversationId: 1);
            _directMessageServiceMock
                .Setup(s => s.GetThreadAsync(1, "user-1"))
                .ReturnsAsync(thread);

            var result = await _controller.GetThread(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<DirectMessageDTO.DirectMessageThreadDTO>(ok.Value);
            Assert.Equal(1, returned.ConversationId);
            Assert.Single(returned.Messages);
        }

        [Fact]
        public async Task GetThread_CallsServiceWithCorrectArguments()
        {
            _directMessageServiceMock
                .Setup(s => s.GetThreadAsync(5, "user-1"))
                .ReturnsAsync(MakeThread(5));

            await _controller.GetThread(5);

            _directMessageServiceMock.Verify(s => s.GetThreadAsync(5, "user-1"), Times.Once);
        }

        [Fact]
        public async Task GetThread_ServiceThrows_KeyNotFound_ExceptionPropagates()
        {
            _directMessageServiceMock
                .Setup(s => s.GetThreadAsync(999, "user-1"))
                .ThrowsAsync(new KeyNotFoundException("Conversation not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.GetThread(999));
        }

        [Fact]
        public async Task GetThread_ServiceThrows_Unauthorized_ExceptionPropagates()
        {
            _directMessageServiceMock
                .Setup(s => s.GetThreadAsync(1, "user-1"))
                .ThrowsAsync(new UnauthorizedAccessException("You are not a participant."));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _controller.GetThread(1));
        }

        [Fact]
        public async Task Send_ReturnsOk_WithSentMessage()
        {
            var dto = new DirectMessageDTO.SendDirectMessageDTO
            {
                RecipientUsernameOrEmail = "other@test.com",
                Content = "Hello!"
            };
            var response = MakeMessageResponse();
            _directMessageServiceMock
                .Setup(s => s.SendAsync("user-1", dto))
                .ReturnsAsync(response);

            var result = await _controller.Send(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<DirectMessageDTO.DirectMessageResponseDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
            Assert.Equal("Hello!", returned.Content);
        }

        [Fact]
        public async Task Send_CallsServiceWithCorrectArguments()
        {
            var dto = new DirectMessageDTO.SendDirectMessageDTO
            {
                RecipientUsernameOrEmail = "other@test.com",
                Content = "Hi there!"
            };
            _directMessageServiceMock
                .Setup(s => s.SendAsync("user-1", dto))
                .ReturnsAsync(MakeMessageResponse());

            await _controller.Send(dto);

            _directMessageServiceMock.Verify(s => s.SendAsync("user-1", dto), Times.Once);
        }

        [Fact]
        public async Task Send_ServiceThrows_KeyNotFound_ExceptionPropagates()
        {
            var dto = new DirectMessageDTO.SendDirectMessageDTO
            {
                RecipientUsernameOrEmail = "nonexistent@test.com",
                Content = "Hello!"
            };
            _directMessageServiceMock
                .Setup(s => s.SendAsync("user-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Recipient not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.Send(dto));
        }

        [Fact]
        public async Task Send_ServiceThrows_Blocked_ExceptionPropagates()
        {
            var dto = new DirectMessageDTO.SendDirectMessageDTO
            {
                RecipientUsernameOrEmail = "blocked@test.com",
                Content = "Hello!"
            };
            _directMessageServiceMock
                .Setup(s => s.SendAsync("user-1", dto))
                .ThrowsAsync(new InvalidOperationException("You are blocked by this user."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.Send(dto));
        }

        [Fact]
        public async Task MarkAsRead_ReturnsNoContent()
        {
            _directMessageServiceMock
                .Setup(s => s.MarkAsReadAsync(1, "user-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.MarkAsRead(1);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task MarkAsRead_CallsServiceWithCorrectArguments()
        {
            _directMessageServiceMock
                .Setup(s => s.MarkAsReadAsync(3, "user-1"))
                .Returns(Task.CompletedTask);

            await _controller.MarkAsRead(3);

            _directMessageServiceMock.Verify(s => s.MarkAsReadAsync(3, "user-1"), Times.Once);
        }

        [Fact]
        public async Task MarkAsRead_ServiceThrows_ExceptionPropagates()
        {
            _directMessageServiceMock
                .Setup(s => s.MarkAsReadAsync(999, "user-1"))
                .ThrowsAsync(new KeyNotFoundException("Conversation not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.MarkAsRead(999));
        }

  
        [Fact]
        public async Task Hide_ReturnsNoContent()
        {
            _directMessageServiceMock
                .Setup(s => s.HideConversationAsync(1, "user-1"))
                .Returns(Task.CompletedTask);

            var result = await _controller.Hide(1);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Hide_CallsServiceWithCorrectArguments()
        {
            _directMessageServiceMock
                .Setup(s => s.HideConversationAsync(2, "user-1"))
                .Returns(Task.CompletedTask);

            await _controller.Hide(2);

            _directMessageServiceMock.Verify(s => s.HideConversationAsync(2, "user-1"), Times.Once);
        }

        [Fact]
        public async Task Hide_ServiceThrows_KeyNotFound_ExceptionPropagates()
        {
            _directMessageServiceMock
                .Setup(s => s.HideConversationAsync(999, "user-1"))
                .ThrowsAsync(new KeyNotFoundException("Conversation not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.Hide(999));
        }

        [Fact]
        public async Task Hide_ServiceThrows_Unauthorized_ExceptionPropagates()
        {
            _directMessageServiceMock
                .Setup(s => s.HideConversationAsync(1, "user-1"))
                .ThrowsAsync(new UnauthorizedAccessException("You are not a participant."));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _controller.Hide(1));
        }
    }
}