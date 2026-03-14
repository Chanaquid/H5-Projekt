using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class LoanControllerTests
    {
        private readonly Mock<ILoanService> _loanServiceMock;
        private readonly LoanController _controller;

        public LoanControllerTests()
        {
            _loanServiceMock = new Mock<ILoanService>();
            _controller = new LoanController(_loanServiceMock.Object);
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

        private static LoanDTO.LoanDetailDTO MakeLoanDetail(
        int id = 1,
        string status = "Pending") => new()
        {
            Id = id,
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(5),
            Status = status,
            SnapshotCondition = "Good",
            CreatedAt = DateTime.UtcNow,
            Item = new ItemDTO.ItemSummaryDTO { Id = 1, Title = "Drill" },
            Owner = new UserDTO.UserSummaryDTO { Id = "owner-1", FullName = "Owner" },
            Borrower = new UserDTO.UserSummaryDTO { Id = "user-1", FullName = "Borrower" }
        };

        private static LoanDTO.LoanSummaryDTO MakeLoanSummary(
            int id = 1,
            string status = "Pending") => new()
            {
                Id = id,
                ItemTitle = "Drill",
                OtherPartyName = "Other User",
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(5),
                Status = status
            };


        [Fact]
        public async Task GetBorrowed_ReturnsOk_WithLoans()
        {
            var loans = new List<LoanDTO.LoanSummaryDTO>
            {
                MakeLoanSummary(1),
                MakeLoanSummary(2)
            };
            _loanServiceMock
                .Setup(s => s.GetBorrowedLoansAsync("user-1"))
                .ReturnsAsync(loans);

            var result = await _controller.GetBorrowed();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<LoanDTO.LoanSummaryDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetBorrowed_ReturnsOk_WithEmptyList()
        {
            _loanServiceMock
                .Setup(s => s.GetBorrowedLoansAsync("user-1"))
                .ReturnsAsync(new List<LoanDTO.LoanSummaryDTO>());

            var result = await _controller.GetBorrowed();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Empty((List<LoanDTO.LoanSummaryDTO>)ok.Value!);
        }

        [Fact]
        public async Task GetBorrowed_CallsServiceWithCorrectUserId()
        {
            SetUser("specific-user");
            _loanServiceMock
                .Setup(s => s.GetBorrowedLoansAsync("specific-user"))
                .ReturnsAsync(new List<LoanDTO.LoanSummaryDTO>());

            await _controller.GetBorrowed();

            _loanServiceMock.Verify(s => s.GetBorrowedLoansAsync("specific-user"), Times.Once);
        }

  
        [Fact]
        public async Task GetOwned_ReturnsOk_WithLoans()
        {
            var loans = new List<LoanDTO.LoanSummaryDTO>
            {
                MakeLoanSummary(1),
                MakeLoanSummary(2)
            };
            _loanServiceMock
                .Setup(s => s.GetOwnedLoansAsync("user-1"))
                .ReturnsAsync(loans);

            var result = await _controller.GetOwned();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<LoanDTO.LoanSummaryDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetOwned_CallsServiceWithCorrectUserId()
        {
            SetUser("owner-1");
            _loanServiceMock
                .Setup(s => s.GetOwnedLoansAsync("owner-1"))
                .ReturnsAsync(new List<LoanDTO.LoanSummaryDTO>());

            await _controller.GetOwned();

            _loanServiceMock.Verify(s => s.GetOwnedLoansAsync("owner-1"), Times.Once);
        }

    
        [Fact]
        public async Task GetById_ReturnsOk_WithLoan()
        {
            var loan = MakeLoanDetail(1);
            _loanServiceMock
                .Setup(s => s.GetByIdAsync(1, "user-1"))
                .ReturnsAsync(loan);

            var result = await _controller.GetById(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<LoanDTO.LoanDetailDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
        }

        [Fact]
        public async Task GetById_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            _loanServiceMock
                .Setup(s => s.GetByIdAsync(999, "user-1"))
                .ThrowsAsync(new KeyNotFoundException("Loan 999 not found."));

            var result = await _controller.GetById(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ServiceThrows_Unauthorized_ReturnsForbid()
        {
            _loanServiceMock
                .Setup(s => s.GetByIdAsync(1, "user-1"))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied."));

            var result = await _controller.GetById(1);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetById_CallsServiceWithCorrectArguments()
        {
            _loanServiceMock
                .Setup(s => s.GetByIdAsync(5, "user-1"))
                .ReturnsAsync(MakeLoanDetail(5));

            await _controller.GetById(5);

            _loanServiceMock.Verify(s => s.GetByIdAsync(5, "user-1"), Times.Once);
        }


        [Fact]
        public async Task Create_ReturnsOk_WithCreatedLoan()
        {
            var dto = new LoanDTO.CreateLoanDTO
            {
                ItemId = 1,
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(5)
            };
            var loan = MakeLoanDetail();
            _loanServiceMock
                .Setup(s => s.CreateAsync("user-1", dto))
                .ReturnsAsync(loan);

            var result = await _controller.Create(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<LoanDTO.LoanDetailDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
        }

        [Fact]
        public async Task Create_CallsServiceWithCorrectArguments()
        {
            var dto = new LoanDTO.CreateLoanDTO { ItemId = 2 };
            _loanServiceMock
                .Setup(s => s.CreateAsync("user-1", dto))
                .ReturnsAsync(MakeLoanDetail());

            await _controller.Create(dto);

            _loanServiceMock.Verify(s => s.CreateAsync("user-1", dto), Times.Once);
        }

        [Fact]
        public async Task Create_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            var dto = new LoanDTO.CreateLoanDTO { ItemId = 999 };
            _loanServiceMock
                .Setup(s => s.CreateAsync("user-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Item 999 not found."));

            var result = await _controller.Create(dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Create_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            var dto = new LoanDTO.CreateLoanDTO { ItemId = 1 };
            _loanServiceMock
                .Setup(s => s.CreateAsync("user-1", dto))
                .ThrowsAsync(new InvalidOperationException("Item already has an active loan."));

            var result = await _controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_ServiceThrows_ArgumentException_ReturnsBadRequest()
        {
            var dto = new LoanDTO.CreateLoanDTO { ItemId = 1 };
            _loanServiceMock
                .Setup(s => s.CreateAsync("user-1", dto))
                .ThrowsAsync(new ArgumentException("Start date cannot be in the past."));

            var result = await _controller.Create(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }


        [Fact]
        public async Task Cancel_ReturnsOk_WithCancelledLoan()
        {
            var dto = new LoanDTO.CancelLoanDTO { Reason = "Changed my mind." };
            var loan = MakeLoanDetail(1, "Cancelled");
            _loanServiceMock
                .Setup(s => s.CancelAsync(1, "user-1", dto))
                .ReturnsAsync(loan);

            var result = await _controller.Cancel(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<LoanDTO.LoanDetailDTO>(ok.Value);
            Assert.Equal("Cancelled", returned.Status);
        }

        [Fact]
        public async Task Cancel_CallsServiceWithCorrectArguments()
        {
            var dto = new LoanDTO.CancelLoanDTO { Reason = "No longer needed." };
            _loanServiceMock
                .Setup(s => s.CancelAsync(3, "user-1", dto))
                .ReturnsAsync(MakeLoanDetail());

            await _controller.Cancel(3, dto);

            _loanServiceMock.Verify(s => s.CancelAsync(3, "user-1", dto), Times.Once);
        }

        [Fact]
        public async Task Cancel_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            var dto = new LoanDTO.CancelLoanDTO();
            _loanServiceMock
                .Setup(s => s.CancelAsync(999, "user-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Loan 999 not found."));

            var result = await _controller.Cancel(999, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Cancel_ServiceThrows_Unauthorized_ReturnsForbid()
        {
            var dto = new LoanDTO.CancelLoanDTO();
            _loanServiceMock
                .Setup(s => s.CancelAsync(1, "user-1", dto))
                .ThrowsAsync(new UnauthorizedAccessException("Not your loan."));

            var result = await _controller.Cancel(1, dto);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Cancel_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            var dto = new LoanDTO.CancelLoanDTO();
            _loanServiceMock
                .Setup(s => s.CancelAsync(1, "user-1", dto))
                .ThrowsAsync(new InvalidOperationException("Loan cannot be cancelled at its current status."));

            var result = await _controller.Cancel(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Decide_Approve_ReturnsOk()
        {
            var dto = new LoanDTO.LoanDecisionDTO { IsApproved = true };
            var loan = MakeLoanDetail(1, "Approved");
            _loanServiceMock
                .Setup(s => s.DecideAsync(1, "user-1", dto))
                .ReturnsAsync(loan);

            var result = await _controller.Decide(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<LoanDTO.LoanDetailDTO>(ok.Value);
            Assert.Equal("Approved", returned.Status);
        }

        [Fact]
        public async Task Decide_Reject_ReturnsOk()
        {
            var dto = new LoanDTO.LoanDecisionDTO { IsApproved = false, DecisionNote = "Not available." };
            var loan = MakeLoanDetail(1, "Rejected");
            _loanServiceMock
                .Setup(s => s.DecideAsync(1, "user-1", dto))
                .ReturnsAsync(loan);

            var result = await _controller.Decide(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<LoanDTO.LoanDetailDTO>(ok.Value);
            Assert.Equal("Rejected", returned.Status);
        }

        [Fact]
        public async Task Decide_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            var dto = new LoanDTO.LoanDecisionDTO { IsApproved = true };
            _loanServiceMock
                .Setup(s => s.DecideAsync(999, "user-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Loan 999 not found."));

            var result = await _controller.Decide(999, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Decide_ServiceThrows_Unauthorized_ReturnsForbid()
        {
            var dto = new LoanDTO.LoanDecisionDTO { IsApproved = true };
            _loanServiceMock
                .Setup(s => s.DecideAsync(1, "user-1", dto))
                .ThrowsAsync(new UnauthorizedAccessException("Not your item."));

            var result = await _controller.Decide(1, dto);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Decide_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            var dto = new LoanDTO.LoanDecisionDTO { IsApproved = true };
            _loanServiceMock
                .Setup(s => s.DecideAsync(1, "user-1", dto))
                .ThrowsAsync(new InvalidOperationException("Only pending loans can be decided."));

            var result = await _controller.Decide(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

  
        [Fact]
        public async Task RequestExtension_ReturnsOk_WithUpdatedLoan()
        {
            var dto = new LoanDTO.RequestExtensionDTO
            {
                RequestedExtensionDate = DateTime.UtcNow.Date.AddDays(10)
            };
            var loan = MakeLoanDetail(1, "Active");
            _loanServiceMock
                .Setup(s => s.RequestExtensionAsync(1, "user-1", dto))
                .ReturnsAsync(loan);

            var result = await _controller.RequestExtension(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task RequestExtension_CallsServiceWithCorrectArguments()
        {
            var dto = new LoanDTO.RequestExtensionDTO
            {
                RequestedExtensionDate = DateTime.UtcNow.Date.AddDays(10)
            };
            _loanServiceMock
                .Setup(s => s.RequestExtensionAsync(2, "user-1", dto))
                .ReturnsAsync(MakeLoanDetail());

            await _controller.RequestExtension(2, dto);

            _loanServiceMock.Verify(s => s.RequestExtensionAsync(2, "user-1", dto), Times.Once);
        }

        [Fact]
        public async Task RequestExtension_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            var dto = new LoanDTO.RequestExtensionDTO { RequestedExtensionDate = DateTime.UtcNow.AddDays(10) };
            _loanServiceMock
                .Setup(s => s.RequestExtensionAsync(999, "user-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Loan 999 not found."));

            var result = await _controller.RequestExtension(999, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task RequestExtension_ServiceThrows_Unauthorized_ReturnsForbid()
        {
            var dto = new LoanDTO.RequestExtensionDTO { RequestedExtensionDate = DateTime.UtcNow.AddDays(10) };
            _loanServiceMock
                .Setup(s => s.RequestExtensionAsync(1, "user-1", dto))
                .ThrowsAsync(new UnauthorizedAccessException("Not your loan."));

            var result = await _controller.RequestExtension(1, dto);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task RequestExtension_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            var dto = new LoanDTO.RequestExtensionDTO { RequestedExtensionDate = DateTime.UtcNow.AddDays(10) };
            _loanServiceMock
                .Setup(s => s.RequestExtensionAsync(1, "user-1", dto))
                .ThrowsAsync(new InvalidOperationException("Extensions can only be requested on active loans."));

            var result = await _controller.RequestExtension(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RequestExtension_ServiceThrows_ArgumentException_ReturnsBadRequest()
        {
            var dto = new LoanDTO.RequestExtensionDTO { RequestedExtensionDate = DateTime.UtcNow.AddDays(-1) };
            _loanServiceMock
                .Setup(s => s.RequestExtensionAsync(1, "user-1", dto))
                .ThrowsAsync(new ArgumentException("Extension date must be after current end date."));

            var result = await _controller.RequestExtension(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

 
        [Fact]
        public async Task DecideExtension_Approve_ReturnsOk()
        {
            var dto = new LoanDTO.ExtensionDecisionDTO { IsApproved = true };
            _loanServiceMock
                .Setup(s => s.DecideExtensionAsync(1, "user-1", dto))
                .ReturnsAsync(MakeLoanDetail(1, "Active"));

            var result = await _controller.DecideExtension(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DecideExtension_Reject_ReturnsOk()
        {
            var dto = new LoanDTO.ExtensionDecisionDTO { IsApproved = false };
            _loanServiceMock
                .Setup(s => s.DecideExtensionAsync(1, "user-1", dto))
                .ReturnsAsync(MakeLoanDetail(1, "Active"));

            var result = await _controller.DecideExtension(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task DecideExtension_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            var dto = new LoanDTO.ExtensionDecisionDTO { IsApproved = true };
            _loanServiceMock
                .Setup(s => s.DecideExtensionAsync(999, "user-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Loan 999 not found."));

            var result = await _controller.DecideExtension(999, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DecideExtension_ServiceThrows_Unauthorized_ReturnsForbid()
        {
            var dto = new LoanDTO.ExtensionDecisionDTO { IsApproved = true };
            _loanServiceMock
                .Setup(s => s.DecideExtensionAsync(1, "user-1", dto))
                .ThrowsAsync(new UnauthorizedAccessException("Not your item."));

            var result = await _controller.DecideExtension(1, dto);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DecideExtension_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            var dto = new LoanDTO.ExtensionDecisionDTO { IsApproved = true };
            _loanServiceMock
                .Setup(s => s.DecideExtensionAsync(1, "user-1", dto))
                .ThrowsAsync(new InvalidOperationException("No pending extension request found."));

            var result = await _controller.DecideExtension(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

  
        [Fact]
        public async Task GetAll_ReturnsOk_WithAllLoans()
        {
            SetUser("admin-1", "Admin");
            var loans = new List<LoanDTO.LoanSummaryDTO>
            {
                MakeLoanSummary(1),
                MakeLoanSummary(2),
                MakeLoanSummary(3)
            };
            _loanServiceMock
                .Setup(s => s.GetAllLoansAsync())
                .ReturnsAsync(loans);

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<LoanDTO.LoanSummaryDTO>>(ok.Value);
            Assert.Equal(3, returned.Count);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithEmptyList()
        {
            SetUser("admin-1", "Admin");
            _loanServiceMock
                .Setup(s => s.GetAllLoansAsync())
                .ReturnsAsync(new List<LoanDTO.LoanSummaryDTO>());

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Empty((List<LoanDTO.LoanSummaryDTO>)ok.Value!);
        }


        [Fact]
        public async Task GetPendingApprovals_ReturnsOk_WithPendingLoans()
        {
            SetUser("admin-1", "Admin");
            var loans = new List<LoanDTO.AdminPendingLoanDTO>
            {
                new() { Id = 1, ItemTitle = "Drill", BorrowerScore = 30 },
                new() { Id = 2, ItemTitle = "Camera", BorrowerScore = 25 }
            };
            _loanServiceMock
                .Setup(s => s.GetPendingApprovalsAsync())
                .ReturnsAsync(loans);

            var result = await _controller.GetPendingApprovals();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<LoanDTO.AdminPendingLoanDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task AdminDecide_Approve_ReturnsOk()
        {
            SetUser("admin-1", "Admin");
            var dto = new LoanDTO.LoanDecisionDTO { IsApproved = true };
            var loan = MakeLoanDetail(1, "Pending");
            _loanServiceMock
                .Setup(s => s.AdminDecideAsync(1, "admin-1", dto))
                .ReturnsAsync(loan);

            var result = await _controller.AdminDecide(1, dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AdminDecide_Reject_ReturnsOk()
        {
            SetUser("admin-1", "Admin");
            var dto = new LoanDTO.LoanDecisionDTO
            {
                IsApproved = false,
                DecisionNote = "Score too risky."
            };
            var loan = MakeLoanDetail(1, "Rejected");
            _loanServiceMock
                .Setup(s => s.AdminDecideAsync(1, "admin-1", dto))
                .ReturnsAsync(loan);

            var result = await _controller.AdminDecide(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<LoanDTO.LoanDetailDTO>(ok.Value);
            Assert.Equal("Rejected", returned.Status);
        }

        [Fact]
        public async Task AdminDecide_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            var dto = new LoanDTO.LoanDecisionDTO { IsApproved = true };
            _loanServiceMock
                .Setup(s => s.AdminDecideAsync(5, "admin-1", dto))
                .ReturnsAsync(MakeLoanDetail());

            await _controller.AdminDecide(5, dto);

            _loanServiceMock.Verify(s => s.AdminDecideAsync(5, "admin-1", dto), Times.Once);
        }

        [Fact]
        public async Task AdminDecide_ServiceThrows_KeyNotFound_ReturnsNotFound()
        {
            SetUser("admin-1", "Admin");
            var dto = new LoanDTO.LoanDecisionDTO { IsApproved = true };
            _loanServiceMock
                .Setup(s => s.AdminDecideAsync(999, "admin-1", dto))
                .ThrowsAsync(new KeyNotFoundException("Loan 999 not found."));

            var result = await _controller.AdminDecide(999, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AdminDecide_ServiceThrows_InvalidOperation_ReturnsBadRequest()
        {
            SetUser("admin-1", "Admin");
            var dto = new LoanDTO.LoanDecisionDTO { IsApproved = true };
            _loanServiceMock
                .Setup(s => s.AdminDecideAsync(1, "admin-1", dto))
                .ThrowsAsync(new InvalidOperationException("Only admin-pending loans can be decided here."));

            var result = await _controller.AdminDecide(1, dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}