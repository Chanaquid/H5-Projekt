using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class ReviewControllerTests
    {
        private readonly Mock<IReviewService> _reviewServiceMock;
        private readonly ReviewController _controller;

        public ReviewControllerTests()
        {
            _reviewServiceMock = new Mock<IReviewService>();
            _controller = new ReviewController(_reviewServiceMock.Object);
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

        private void SetAnonymousUser()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };
        }

        private static ReviewDTO.ItemReviewResponseDTO MakeItemReview(
            int id = 1,
            int rating = 4) => new()
            {
                Id = id,
                LoanId = 10,
                Rating = rating,
                Comment = "Great item!",
                ReviewerName = "User 1",
                IsAdminReview = false,
                IsEdited = false,
                CreatedAt = DateTime.UtcNow
            };

        private static ReviewDTO.UserReviewResponseDTO MakeUserReview(
            int id = 1,
            int rating = 4) => new()
            {
                Id = id,
                LoanId = 10,
                ItemTitle = "Drill",
                Rating = rating,
                Comment = "Great borrower!",
                ReviewerName = "User 1",
                IsAdminReview = false,
                IsEdited = false,
                CreatedAt = DateTime.UtcNow
            };

        [Fact]
        public async Task CreateItemReview_ReturnsOk_WithCreatedReview()
        {
            var dto = new ReviewDTO.CreateItemReviewDTO
            {
                LoanId = 10,
                ItemId = 1,
                Rating = 5,
                Comment = "Excellent item!"
            };
            var response = MakeItemReview(rating: 5);
            _reviewServiceMock
                .Setup(s => s.CreateItemReviewAsync("user-1", dto, false))
                .ReturnsAsync(response);

            var result = await _controller.CreateItemReview(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ReviewDTO.ItemReviewResponseDTO>(ok.Value);
            Assert.Equal(5, returned.Rating);
        }

        [Fact]
        public async Task CreateItemReview_AsAdmin_PassesIsAdminTrue()
        {
            SetUser("admin-1", "Admin");
            var dto = new ReviewDTO.CreateItemReviewDTO { ItemId = 1, Rating = 4 };
            _reviewServiceMock
                .Setup(s => s.CreateItemReviewAsync("admin-1", dto, true))
                .ReturnsAsync(MakeItemReview());

            await _controller.CreateItemReview(dto);

            _reviewServiceMock.Verify(s =>
                s.CreateItemReviewAsync("admin-1", dto, true), Times.Once);
        }

        [Fact]
        public async Task CreateItemReview_AsUser_PassesIsAdminFalse()
        {
            var dto = new ReviewDTO.CreateItemReviewDTO { LoanId = 10, ItemId = 1, Rating = 4 };
            _reviewServiceMock
                .Setup(s => s.CreateItemReviewAsync("user-1", dto, false))
                .ReturnsAsync(MakeItemReview());

            await _controller.CreateItemReview(dto);

            _reviewServiceMock.Verify(s =>
                s.CreateItemReviewAsync("user-1", dto, false), Times.Once);
        }

        [Fact]
        public async Task CreateItemReview_ServiceThrows_KeyNotFound_ExceptionPropagates()
        {
            var dto = new ReviewDTO.CreateItemReviewDTO { LoanId = 999, ItemId = 1, Rating = 4 };
            _reviewServiceMock
                .Setup(s => s.CreateItemReviewAsync("user-1", dto, false))
                .ThrowsAsync(new KeyNotFoundException("Loan 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.CreateItemReview(dto));
        }

        [Fact]
        public async Task CreateItemReview_ServiceThrows_InvalidOperation_ExceptionPropagates()
        {
            var dto = new ReviewDTO.CreateItemReviewDTO { LoanId = 10, ItemId = 1, Rating = 4 };
            _reviewServiceMock
                .Setup(s => s.CreateItemReviewAsync("user-1", dto, false))
                .ThrowsAsync(new InvalidOperationException("You have already reviewed this loan."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.CreateItemReview(dto));
        }


        [Fact]
        public async Task GetItemReviews_ReturnsOk_WithReviews()
        {
            SetAnonymousUser();
            var reviews = new List<ReviewDTO.ItemReviewResponseDTO>
            {
                MakeItemReview(1),
                MakeItemReview(2)
            };
            _reviewServiceMock
                .Setup(s => s.GetItemReviewsAsync(1))
                .ReturnsAsync(reviews);

            var result = await _controller.GetItemReviews(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<ReviewDTO.ItemReviewResponseDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetItemReviews_ReturnsOk_WithEmptyList()
        {
            SetAnonymousUser();
            _reviewServiceMock
                .Setup(s => s.GetItemReviewsAsync(1))
                .ReturnsAsync(new List<ReviewDTO.ItemReviewResponseDTO>());

            var result = await _controller.GetItemReviews(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Empty((List<ReviewDTO.ItemReviewResponseDTO>)ok.Value!);
        }

        [Fact]
        public async Task GetItemReviews_CallsServiceWithCorrectItemId()
        {
            _reviewServiceMock
                .Setup(s => s.GetItemReviewsAsync(5))
                .ReturnsAsync(new List<ReviewDTO.ItemReviewResponseDTO>());

            await _controller.GetItemReviews(5);

            _reviewServiceMock.Verify(s => s.GetItemReviewsAsync(5), Times.Once);
        }

        [Fact]
        public async Task GetItemReviews_ServiceThrows_ExceptionPropagates()
        {
            _reviewServiceMock
                .Setup(s => s.GetItemReviewsAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Item 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.GetItemReviews(999));
        }

 
        [Fact]
        public async Task EditItemReview_ReturnsOk_WithUpdatedReview()
        {
            SetUser("admin-1", "Admin");
            var dto = new ReviewDTO.EditReviewDTO { Rating = 3, Comment = "Updated comment." };
            var response = MakeItemReview(1, rating: 3);
            _reviewServiceMock
                .Setup(s => s.EditItemReviewAsync(1, dto))
                .ReturnsAsync(response);

            var result = await _controller.EditItemReview(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ReviewDTO.ItemReviewResponseDTO>(ok.Value);
            Assert.Equal(3, returned.Rating);
        }

        [Fact]
        public async Task EditItemReview_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            var dto = new ReviewDTO.EditReviewDTO { Rating = 2, Comment = "Changed mind." };
            _reviewServiceMock
                .Setup(s => s.EditItemReviewAsync(3, dto))
                .ReturnsAsync(MakeItemReview());

            await _controller.EditItemReview(3, dto);

            _reviewServiceMock.Verify(s => s.EditItemReviewAsync(3, dto), Times.Once);
        }

        [Fact]
        public async Task EditItemReview_ServiceThrows_ExceptionPropagates()
        {
            SetUser("admin-1", "Admin");
            var dto = new ReviewDTO.EditReviewDTO { Rating = 3 };
            _reviewServiceMock
                .Setup(s => s.EditItemReviewAsync(999, dto))
                .ThrowsAsync(new KeyNotFoundException("Review 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.EditItemReview(999, dto));
        }

  
        [Fact]
        public async Task DeleteItemReview_ReturnsNoContent()
        {
            SetUser("admin-1", "Admin");
            _reviewServiceMock
                .Setup(s => s.DeleteItemReviewAsync(1))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteItemReview(1);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteItemReview_CallsServiceWithCorrectId()
        {
            SetUser("admin-1", "Admin");
            _reviewServiceMock
                .Setup(s => s.DeleteItemReviewAsync(3))
                .Returns(Task.CompletedTask);

            await _controller.DeleteItemReview(3);

            _reviewServiceMock.Verify(s => s.DeleteItemReviewAsync(3), Times.Once);
        }

        [Fact]
        public async Task DeleteItemReview_ServiceThrows_ExceptionPropagates()
        {
            SetUser("admin-1", "Admin");
            _reviewServiceMock
                .Setup(s => s.DeleteItemReviewAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Review 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.DeleteItemReview(999));
        }

 
        [Fact]
        public async Task CreateUserReview_ReturnsOk_WithCreatedReview()
        {
            var dto = new ReviewDTO.CreateUserReviewDTO
            {
                LoanId = 10,
                ReviewedUserId = "owner-1",
                Rating = 5,
                Comment = "Great owner!"
            };
            var response = MakeUserReview(rating: 5);
            _reviewServiceMock
                .Setup(s => s.CreateUserReviewAsync("user-1", dto, false))
                .ReturnsAsync(response);

            var result = await _controller.CreateUserReview(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ReviewDTO.UserReviewResponseDTO>(ok.Value);
            Assert.Equal(5, returned.Rating);
        }

        [Fact]
        public async Task CreateUserReview_AsAdmin_PassesIsAdminTrue()
        {
            SetUser("admin-1", "Admin");
            var dto = new ReviewDTO.CreateUserReviewDTO
            {
                ReviewedUserId = "user-1",
                Rating = 3
            };
            _reviewServiceMock
                .Setup(s => s.CreateUserReviewAsync("admin-1", dto, true))
                .ReturnsAsync(MakeUserReview());

            await _controller.CreateUserReview(dto);

            _reviewServiceMock.Verify(s =>
                s.CreateUserReviewAsync("admin-1", dto, true), Times.Once);
        }

        [Fact]
        public async Task CreateUserReview_AsUser_PassesIsAdminFalse()
        {
            var dto = new ReviewDTO.CreateUserReviewDTO
            {
                LoanId = 10,
                ReviewedUserId = "owner-1",
                Rating = 4
            };
            _reviewServiceMock
                .Setup(s => s.CreateUserReviewAsync("user-1", dto, false))
                .ReturnsAsync(MakeUserReview());

            await _controller.CreateUserReview(dto);

            _reviewServiceMock.Verify(s =>
                s.CreateUserReviewAsync("user-1", dto, false), Times.Once);
        }

        [Fact]
        public async Task CreateUserReview_ServiceThrows_InvalidOperation_ExceptionPropagates()
        {
            var dto = new ReviewDTO.CreateUserReviewDTO
            {
                LoanId = 10,
                ReviewedUserId = "owner-1",
                Rating = 4
            };
            _reviewServiceMock
                .Setup(s => s.CreateUserReviewAsync("user-1", dto, false))
                .ThrowsAsync(new InvalidOperationException("You have already reviewed this user for this loan."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.CreateUserReview(dto));
        }

        [Fact]
        public async Task GetUserReviews_ReturnsOk_WithReviews()
        {
            SetAnonymousUser();
            var reviews = new List<ReviewDTO.UserReviewResponseDTO>
            {
                MakeUserReview(1),
                MakeUserReview(2)
            };
            _reviewServiceMock
                .Setup(s => s.GetUserReviewsAsync("owner-1"))
                .ReturnsAsync(reviews);

            var result = await _controller.GetUserReviews("owner-1");

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<ReviewDTO.UserReviewResponseDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetUserReviews_ReturnsOk_WithEmptyList()
        {
            SetAnonymousUser();
            _reviewServiceMock
                .Setup(s => s.GetUserReviewsAsync("owner-1"))
                .ReturnsAsync(new List<ReviewDTO.UserReviewResponseDTO>());

            var result = await _controller.GetUserReviews("owner-1");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Empty((List<ReviewDTO.UserReviewResponseDTO>)ok.Value!);
        }

        [Fact]
        public async Task GetUserReviews_CallsServiceWithCorrectUserId()
        {
            _reviewServiceMock
                .Setup(s => s.GetUserReviewsAsync("target-user"))
                .ReturnsAsync(new List<ReviewDTO.UserReviewResponseDTO>());

            await _controller.GetUserReviews("target-user");

            _reviewServiceMock.Verify(s => s.GetUserReviewsAsync("target-user"), Times.Once);
        }


        [Fact]
        public async Task EditUserReview_ReturnsOk_WithUpdatedReview()
        {
            SetUser("admin-1", "Admin");
            var dto = new ReviewDTO.EditReviewDTO { Rating = 2, Comment = "Updated." };
            var response = MakeUserReview(1, rating: 2);
            _reviewServiceMock
                .Setup(s => s.EditUserReviewAsync(1, dto))
                .ReturnsAsync(response);

            var result = await _controller.EditUserReview(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<ReviewDTO.UserReviewResponseDTO>(ok.Value);
            Assert.Equal(2, returned.Rating);
        }

        [Fact]
        public async Task EditUserReview_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            var dto = new ReviewDTO.EditReviewDTO { Rating = 5, Comment = "Outstanding." };
            _reviewServiceMock
                .Setup(s => s.EditUserReviewAsync(3, dto))
                .ReturnsAsync(MakeUserReview());

            await _controller.EditUserReview(3, dto);

            _reviewServiceMock.Verify(s => s.EditUserReviewAsync(3, dto), Times.Once);
        }

        [Fact]
        public async Task EditUserReview_ServiceThrows_ExceptionPropagates()
        {
            SetUser("admin-1", "Admin");
            var dto = new ReviewDTO.EditReviewDTO { Rating = 3 };
            _reviewServiceMock
                .Setup(s => s.EditUserReviewAsync(999, dto))
                .ThrowsAsync(new KeyNotFoundException("Review 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.EditUserReview(999, dto));
        }

 
        [Fact]
        public async Task DeleteUserReview_ReturnsNoContent()
        {
            SetUser("admin-1", "Admin");
            _reviewServiceMock
                .Setup(s => s.DeleteUserReviewAsync(1))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteUserReview(1);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteUserReview_CallsServiceWithCorrectId()
        {
            SetUser("admin-1", "Admin");
            _reviewServiceMock
                .Setup(s => s.DeleteUserReviewAsync(4))
                .Returns(Task.CompletedTask);

            await _controller.DeleteUserReview(4);

            _reviewServiceMock.Verify(s => s.DeleteUserReviewAsync(4), Times.Once);
        }

        [Fact]
        public async Task DeleteUserReview_ServiceThrows_ExceptionPropagates()
        {
            SetUser("admin-1", "Admin");
            _reviewServiceMock
                .Setup(s => s.DeleteUserReviewAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Review 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.DeleteUserReview(999));
        }
    }
}