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
    public class ReviewServiceTests
    {
        private readonly Mock<IReviewRepository> _reviewRepoMock;
        private readonly Mock<ILoanRepository> _loanRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly ReviewService _service;

        public ReviewServiceTests()
        {
            _reviewRepoMock = new Mock<IReviewRepository>();
            _loanRepoMock = new Mock<ILoanRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _service = new ReviewService(
                _reviewRepoMock.Object,
                _loanRepoMock.Object,
                _userRepoMock.Object);
        }

        [Fact]
        public async Task CreateItemReviewAsync_ValidBorrower_ReturnsDTO()
        {
            var loan = MakeCompletedLoan(1, "borrower-1", "owner-1");
            var dto = new ReviewDTO.CreateItemReviewDTO { LoanId = 1, Rating = 4, Comment = "Nice!" };

            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _reviewRepoMock.Setup(r => r.GetItemReviewByLoanIdAsync(1)).ReturnsAsync((ItemReview?)null);
            _reviewRepoMock.Setup(r => r.AddItemReviewAsync(It.IsAny<ItemReview>())).Returns(Task.CompletedTask);
            _reviewRepoMock.Setup(r => r.LoadReviewerAsync(It.IsAny<ItemReview>())).Returns(Task.CompletedTask);

            var result = await _service.CreateItemReviewAsync("borrower-1", dto);

            result.Should().NotBeNull();
            result.Rating.Should().Be(4);
            _reviewRepoMock.Verify(r => r.AddItemReviewAsync(It.IsAny<ItemReview>()), Times.Once);
            _reviewRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateItemReviewAsync_CommentIsTrimmed()
        {
            var loan = MakeCompletedLoan(1, "borrower-1", "owner-1");
            var dto = new ReviewDTO.CreateItemReviewDTO { LoanId = 1, Rating = 3, Comment = "  Great item  " };

            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _reviewRepoMock.Setup(r => r.GetItemReviewByLoanIdAsync(1)).ReturnsAsync((ItemReview?)null);
            _reviewRepoMock.Setup(r => r.AddItemReviewAsync(It.IsAny<ItemReview>())).Returns(Task.CompletedTask);
            _reviewRepoMock.Setup(r => r.LoadReviewerAsync(It.IsAny<ItemReview>())).Returns(Task.CompletedTask);

            ItemReview? captured = null;
            _reviewRepoMock.Setup(r => r.AddItemReviewAsync(It.IsAny<ItemReview>()))
                .Callback<ItemReview>(r => captured = r)
                .Returns(Task.CompletedTask);

            await _service.CreateItemReviewAsync("borrower-1", dto);

            captured!.Comment.Should().Be("Great item");
        }

        [Fact]
        public async Task CreateItemReviewAsync_WithoutLoanId_ThrowsArgumentException()
        {
            var dto = new ReviewDTO.CreateItemReviewDTO { LoanId = null, Rating = 4 };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateItemReviewAsync("borrower-1", dto));
        }

        [Fact]
        public async Task CreateItemReviewAsync_LoanNotFound_ThrowsKeyNotFoundException()
        {
            var dto = new ReviewDTO.CreateItemReviewDTO { LoanId = 99, Rating = 4 };
            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(99)).ReturnsAsync((Loan?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CreateItemReviewAsync("borrower-1", dto));
        }

        [Fact]
        public async Task CreateItemReviewAsync_NonBorrower_ThrowsUnauthorizedAccessException()
        {
            var loan = MakeCompletedLoan(1, "borrower-1", "owner-1");
            var dto = new ReviewDTO.CreateItemReviewDTO { LoanId = 1, Rating = 4 };
            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.CreateItemReviewAsync("someone-else", dto));
        }

        [Theory]
        [InlineData(LoanStatus.Active)]
        [InlineData(LoanStatus.Pending)]
        [InlineData(LoanStatus.Approved)]
        public async Task CreateItemReviewAsync_LoanNotCompleted_ThrowsInvalidOperationException(LoanStatus status)
        {
            var loan = MakeCompletedLoan(1, "borrower-1", "owner-1", status);
            var dto = new ReviewDTO.CreateItemReviewDTO { LoanId = 1, Rating = 4 };
            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateItemReviewAsync("borrower-1", dto));
        }

        [Fact]
        public async Task CreateItemReviewAsync_AlreadyReviewed_ThrowsInvalidOperationException()
        {
            var loan = MakeCompletedLoan(1, "borrower-1", "owner-1");
            var dto = new ReviewDTO.CreateItemReviewDTO { LoanId = 1, Rating = 4 };
            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _reviewRepoMock.Setup(r => r.GetItemReviewByLoanIdAsync(1)).ReturnsAsync(MakeItemReview(1));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateItemReviewAsync("borrower-1", dto));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        public async Task CreateItemReviewAsync_InvalidRating_ThrowsArgumentException(int rating)
        {
            var loan = MakeCompletedLoan(1, "borrower-1", "owner-1");
            var dto = new ReviewDTO.CreateItemReviewDTO { LoanId = 1, Rating = rating };
            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _reviewRepoMock.Setup(r => r.GetItemReviewByLoanIdAsync(1)).ReturnsAsync((ItemReview?)null);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateItemReviewAsync("borrower-1", dto));
        }

        [Fact]
        public async Task CreateItemReviewAsync_AdminWithoutItemId_ThrowsArgumentException()
        {
            var dto = new ReviewDTO.CreateItemReviewDTO { ItemId = 0, Rating = 4 };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateItemReviewAsync("admin", dto, isAdmin: true));
        }

        [Fact]
        public async Task CreateItemReviewAsync_AdminWithValidItemId_CreatesReview()
        {
            var dto = new ReviewDTO.CreateItemReviewDTO { ItemId = 10, Rating = 5, Comment = "Admin verified" };
            _reviewRepoMock.Setup(r => r.AddItemReviewAsync(It.IsAny<ItemReview>())).Returns(Task.CompletedTask);
            _reviewRepoMock.Setup(r => r.LoadReviewerAsync(It.IsAny<ItemReview>())).Returns(Task.CompletedTask);

            var result = await _service.CreateItemReviewAsync("admin", dto, isAdmin: true);

            result.IsAdminReview.Should().BeTrue();
            _reviewRepoMock.Verify(r => r.AddItemReviewAsync(It.IsAny<ItemReview>()), Times.Once);
        }


        [Fact]
        public async Task GetItemReviewsAsync_AdminReviewComesFirst()
        {
            var reviews = new List<ItemReview>
            {
                MakeItemReview(1, isAdmin: false),
                MakeItemReview(2, isAdmin: true),
                MakeItemReview(3, isAdmin: false)
            };
            _reviewRepoMock.Setup(r => r.GetItemReviewsByItemIdAsync(10)).ReturnsAsync(reviews);

            var result = await _service.GetItemReviewsAsync(10);

            result.First().IsAdminReview.Should().BeTrue();
        }

        [Fact]
        public async Task GetItemReviewsAsync_NoReviews_ReturnsEmptyList()
        {
            _reviewRepoMock.Setup(r => r.GetItemReviewsByItemIdAsync(10)).ReturnsAsync(new List<ItemReview>());

            var result = await _service.GetItemReviewsAsync(10);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserReviewsAsync_AdminReviewComesFirst()
        {
            var reviews = new List<UserReview>
            {
                MakeUserReview(1, isAdmin: false),
                MakeUserReview(2, isAdmin: true),
                MakeUserReview(3, isAdmin: false)
            };
            _reviewRepoMock.Setup(r => r.GetUserReviewsByReviewedUserIdAsync("owner-1")).ReturnsAsync(reviews);

            var result = await _service.GetUserReviewsAsync("owner-1");

            result.First().IsAdminReview.Should().BeTrue();
        }

        [Fact]
        public async Task CreateUserReviewAsync_BorrowerReviewsOwner_ReturnsDTO()
        {
            var loan = MakeCompletedLoan(1, "borrower-1", "owner-1");
            var dto = new ReviewDTO.CreateUserReviewDTO { LoanId = 1, ReviewedUserId = "owner-1", Rating = 5 };

            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _reviewRepoMock.Setup(r => r.GetUserReviewByLoanAndReviewerAsync(1, "borrower-1")).ReturnsAsync((UserReview?)null);
            _userRepoMock.Setup(r => r.GetByIdAsync("owner-1")).ReturnsAsync(new ApplicationUser { Id = "owner-1" });
            _reviewRepoMock.Setup(r => r.AddUserReviewAsync(It.IsAny<UserReview>())).Returns(Task.CompletedTask);
            _reviewRepoMock.Setup(r => r.LoadUserReviewDetailsAsync(It.IsAny<UserReview>())).Returns(Task.CompletedTask);

            var result = await _service.CreateUserReviewAsync("borrower-1", dto);

            result.Should().NotBeNull();
            _reviewRepoMock.Verify(r => r.AddUserReviewAsync(It.IsAny<UserReview>()), Times.Once);
        }

        [Fact]
        public async Task CreateUserReviewAsync_OwnerReviewsBorrower_ReturnsDTO()
        {
            var loan = MakeCompletedLoan(1, "borrower-1", "owner-1");
            var dto = new ReviewDTO.CreateUserReviewDTO { LoanId = 1, ReviewedUserId = "borrower-1", Rating = 4 };

            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _reviewRepoMock.Setup(r => r.GetUserReviewByLoanAndReviewerAsync(1, "owner-1")).ReturnsAsync((UserReview?)null);
            _userRepoMock.Setup(r => r.GetByIdAsync("borrower-1")).ReturnsAsync(new ApplicationUser { Id = "borrower-1" });
            _reviewRepoMock.Setup(r => r.AddUserReviewAsync(It.IsAny<UserReview>())).Returns(Task.CompletedTask);
            _reviewRepoMock.Setup(r => r.LoadUserReviewDetailsAsync(It.IsAny<UserReview>())).Returns(Task.CompletedTask);

            var result = await _service.CreateUserReviewAsync("owner-1", dto);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateUserReviewAsync_SelfReview_ThrowsArgumentException()
        {
            var dto = new ReviewDTO.CreateUserReviewDTO { LoanId = 1, ReviewedUserId = "borrower-1", Rating = 5 };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateUserReviewAsync("borrower-1", dto));
        }

        [Fact]
        public async Task CreateUserReviewAsync_WithoutLoanId_ThrowsArgumentException()
        {
            var dto = new ReviewDTO.CreateUserReviewDTO { LoanId = null, ReviewedUserId = "owner-1", Rating = 5 };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateUserReviewAsync("borrower-1", dto));
        }

        [Fact]
        public async Task CreateUserReviewAsync_ThirdPartyReviewer_ThrowsUnauthorizedAccessException()
        {
            var loan = MakeCompletedLoan(1, "borrower-1", "owner-1");
            var dto = new ReviewDTO.CreateUserReviewDTO { LoanId = 1, ReviewedUserId = "owner-1", Rating = 5 };
            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.CreateUserReviewAsync("stranger", dto));
        }

        [Fact]
        public async Task CreateUserReviewAsync_ReviewedUserNotOnLoan_ThrowsArgumentException()
        {
            var loan = MakeCompletedLoan(1, "borrower-1", "owner-1");
            var dto = new ReviewDTO.CreateUserReviewDTO { LoanId = 1, ReviewedUserId = "stranger", Rating = 5 };
            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateUserReviewAsync("borrower-1", dto));
        }

        [Fact]
        public async Task CreateUserReviewAsync_AlreadyReviewed_ThrowsInvalidOperationException()
        {
            var loan = MakeCompletedLoan(1, "borrower-1", "owner-1");
            var dto = new ReviewDTO.CreateUserReviewDTO { LoanId = 1, ReviewedUserId = "owner-1", Rating = 5 };
            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _reviewRepoMock.Setup(r => r.GetUserReviewByLoanAndReviewerAsync(1, "borrower-1")).ReturnsAsync(MakeUserReview(1));
            _userRepoMock.Setup(r => r.GetByIdAsync("owner-1")).ReturnsAsync(new ApplicationUser { Id = "owner-1" });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateUserReviewAsync("borrower-1", dto));
        }

        [Fact]
        public async Task CreateUserReviewAsync_ReviewedUserNotFound_ThrowsKeyNotFoundException()
        {
            var loan = MakeCompletedLoan(1, "borrower-1", "owner-1");
            var dto = new ReviewDTO.CreateUserReviewDTO { LoanId = 1, ReviewedUserId = "owner-1", Rating = 5 };
            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _reviewRepoMock.Setup(r => r.GetUserReviewByLoanAndReviewerAsync(1, "borrower-1")).ReturnsAsync((UserReview?)null);
            _userRepoMock.Setup(r => r.GetByIdAsync("owner-1")).ReturnsAsync((ApplicationUser?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CreateUserReviewAsync("borrower-1", dto));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        public async Task CreateUserReviewAsync_InvalidRating_ThrowsArgumentException(int rating)
        {
            var loan = MakeCompletedLoan(1, "borrower-1", "owner-1");
            var dto = new ReviewDTO.CreateUserReviewDTO { LoanId = 1, ReviewedUserId = "owner-1", Rating = rating };
            _loanRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1)).ReturnsAsync(loan);
            _reviewRepoMock.Setup(r => r.GetUserReviewByLoanAndReviewerAsync(1, "borrower-1")).ReturnsAsync((UserReview?)null);
            _userRepoMock.Setup(r => r.GetByIdAsync("owner-1")).ReturnsAsync(new ApplicationUser { Id = "owner-1" });

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateUserReviewAsync("borrower-1", dto));
        }

        [Fact]
        public async Task EditItemReviewAsync_ValidAdminReview_UpdatesFields()
        {
            var review = MakeItemReview(1, isAdmin: true);
            var dto = new ReviewDTO.EditReviewDTO { Rating = 2, Comment = "Updated comment" };
            _reviewRepoMock.Setup(r => r.GetItemReviewByIdAsync(1)).ReturnsAsync(review);

            var result = await _service.EditItemReviewAsync(1, dto);

            result.Rating.Should().Be(2);
            result.IsEdited.Should().BeTrue();
            _reviewRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task EditItemReviewAsync_NonAdminReview_ThrowsUnauthorizedAccessException()
        {
            var review = MakeItemReview(1, isAdmin: false);
            var dto = new ReviewDTO.EditReviewDTO { Rating = 3 };
            _reviewRepoMock.Setup(r => r.GetItemReviewByIdAsync(1)).ReturnsAsync(review);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.EditItemReviewAsync(1, dto));
        }

        [Fact]
        public async Task EditItemReviewAsync_ReviewNotFound_ThrowsKeyNotFoundException()
        {
            _reviewRepoMock.Setup(r => r.GetItemReviewByIdAsync(99)).ReturnsAsync((ItemReview?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.EditItemReviewAsync(99, new ReviewDTO.EditReviewDTO { Rating = 3 }));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public async Task EditItemReviewAsync_InvalidRating_ThrowsArgumentException(int rating)
        {
            var review = MakeItemReview(1, isAdmin: true);
            _reviewRepoMock.Setup(r => r.GetItemReviewByIdAsync(1)).ReturnsAsync(review);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.EditItemReviewAsync(1, new ReviewDTO.EditReviewDTO { Rating = rating }));
        }


        [Fact]
        public async Task EditUserReviewAsync_ValidAdminReview_UpdatesFields()
        {
            var review = MakeUserReview(1, isAdmin: true);
            var dto = new ReviewDTO.EditReviewDTO { Rating = 1, Comment = "Revised" };
            _reviewRepoMock.Setup(r => r.GetUserReviewByIdAsync(1)).ReturnsAsync(review);

            var result = await _service.EditUserReviewAsync(1, dto);

            result.Rating.Should().Be(1);
            result.IsEdited.Should().BeTrue();
            _reviewRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task EditUserReviewAsync_NonAdminReview_ThrowsUnauthorizedAccessException()
        {
            var review = MakeUserReview(1, isAdmin: false);
            _reviewRepoMock.Setup(r => r.GetUserReviewByIdAsync(1)).ReturnsAsync(review);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.EditUserReviewAsync(1, new ReviewDTO.EditReviewDTO { Rating = 3 }));
        }


        [Fact]
        public async Task DeleteItemReviewAsync_ValidReview_DeletesAndSaves()
        {
            var review = MakeItemReview(1);
            _reviewRepoMock.Setup(r => r.GetItemReviewByIdAsync(1)).ReturnsAsync(review);

            await _service.DeleteItemReviewAsync(1);

            _reviewRepoMock.Verify(r => r.DeleteItemReview(review), Times.Once);
            _reviewRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteItemReviewAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _reviewRepoMock.Setup(r => r.GetItemReviewByIdAsync(99)).ReturnsAsync((ItemReview?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.DeleteItemReviewAsync(99));
        }


        [Fact]
        public async Task DeleteUserReviewAsync_ValidReview_DeletesAndSaves()
        {
            var review = MakeUserReview(1);
            _reviewRepoMock.Setup(r => r.GetUserReviewByIdAsync(1)).ReturnsAsync(review);

            await _service.DeleteUserReviewAsync(1);

            _reviewRepoMock.Verify(r => r.DeleteUserReview(review), Times.Once);
            _reviewRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteUserReviewAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _reviewRepoMock.Setup(r => r.GetUserReviewByIdAsync(99)).ReturnsAsync((UserReview?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.DeleteUserReviewAsync(99));
        }




        //Helpers
        private static Loan MakeCompletedLoan(int id, string borrowerId, string ownerId, LoanStatus status = LoanStatus.Returned) => new()
        {
            Id = id,
            BorrowerId = borrowerId,
            Item = new Item { Id = 10, OwnerId = ownerId, Title = "Test Item" },
            Status = status
        };

        private static ItemReview MakeItemReview(int id, bool isAdmin = false) => new()
        {
            Id = id,
            ItemId = 10,
            LoanId = 1,
            ReviewerId = "borrower-1",
            Rating = 4,
            Comment = "Great item",
            IsAdminReview = isAdmin,
            CreatedAt = DateTime.UtcNow,
            Reviewer = new ApplicationUser { FullName = "Borrower One" }
        };

        private static UserReview MakeUserReview(int id, bool isAdmin = false) => new()
        {
            Id = id,
            LoanId = 1,
            ReviewerId = "borrower-1",
            ReviewedUserId = "owner-1",
            Rating = 5,
            Comment = "Great owner",
            IsAdminReview = isAdmin,
            CreatedAt = DateTime.UtcNow,
            Reviewer = new ApplicationUser { FullName = "Borrower One" }
        };









    }
}
