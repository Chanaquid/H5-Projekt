using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Repositories
{
    public class VerificationRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly VerificationRepository _repo;

        public VerificationRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new VerificationRepository(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }


        private async Task<ApplicationUser> SeedUserAsync(string id)
        {
            var user = new ApplicationUser
            {
                Id = id,
                UserName = $"{id}@test.com",
                Email = $"{id}@test.com",
                FullName = $"User {id}",
                Address = "Test Address",
                NormalizedEmail = $"{id}@test.com".ToUpper(),
                NormalizedUserName = $"{id}@test.com".ToUpper(),
                MembershipDate = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private async Task<VerificationRequest> SeedRequestAsync(
            string userId,
            VerificationStatus status = VerificationStatus.Pending,
            VerificationDocumentType documentType = VerificationDocumentType.Passport,
            string? reviewedByAdminId = null,
            DateTime? submittedAt = null)
        {
            var request = new VerificationRequest
            {
                UserId = userId,
                DocumentUrl = "http://test.com/doc.jpg",
                DocumentType = documentType,
                Status = status,
                ReviewedByAdminId = reviewedByAdminId,
                SubmittedAt = submittedAt ?? DateTime.UtcNow
            };
            _context.VerificationRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

 
        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsRequest()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedRequestAsync("user-1");

            var result = await _repo.GetByIdAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_ExistingId_IncludesUser()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedRequestAsync("user-1");

            _context.ChangeTracker.Clear();

            var result = await _repo.GetByIdWithDetailsAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.NotNull(result!.User);
            Assert.Equal("user-1", result.User.Id);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_IncludesReviewedByAdmin()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("admin-1");
            var seeded = await SeedRequestAsync("user-1",
                VerificationStatus.Approved, reviewedByAdminId: "admin-1");

            _context.ChangeTracker.Clear();

            var result = await _repo.GetByIdWithDetailsAsync(seeded.Id);

            Assert.NotNull(result!.ReviewedByAdmin);
            Assert.Equal("admin-1", result.ReviewedByAdmin!.Id);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetByIdWithDetailsAsync(999);

            Assert.Null(result);
        }

 
        [Fact]
        public async Task GetPendingByUserIdAsync_HasPendingRequest_ReturnsIt()
        {
            await SeedUserAsync("user-1");
            var seeded = await SeedRequestAsync("user-1", VerificationStatus.Pending);

            var result = await _repo.GetPendingByUserIdAsync("user-1");

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
            Assert.Equal(VerificationStatus.Pending, result.Status);
        }

        [Fact]
        public async Task GetPendingByUserIdAsync_OnlyApprovedRequest_ReturnsNull()
        {
            await SeedUserAsync("user-1");
            await SeedRequestAsync("user-1", VerificationStatus.Approved);

            var result = await _repo.GetPendingByUserIdAsync("user-1");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetPendingByUserIdAsync_OnlyRejectedRequest_ReturnsNull()
        {
            await SeedUserAsync("user-1");
            await SeedRequestAsync("user-1", VerificationStatus.Rejected);

            var result = await _repo.GetPendingByUserIdAsync("user-1");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetPendingByUserIdAsync_NoRequests_ReturnsNull()
        {
            var result = await _repo.GetPendingByUserIdAsync("user-1");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetPendingByUserIdAsync_DifferentUser_ReturnsNull()
        {
            await SeedUserAsync("user-1");
            await SeedRequestAsync("user-1", VerificationStatus.Pending);

            var result = await _repo.GetPendingByUserIdAsync("user-2");

            Assert.Null(result);
        }


        [Fact]
        public async Task GetLatestByUserIdAsync_ReturnsNewestRequest()
        {
            await SeedUserAsync("user-1");
            var older = await SeedRequestAsync("user-1",
                submittedAt: DateTime.UtcNow.AddMinutes(-30));
            var newer = await SeedRequestAsync("user-1",
                submittedAt: DateTime.UtcNow);

            var result = await _repo.GetLatestByUserIdAsync("user-1");

            Assert.NotNull(result);
            Assert.Equal(newer.Id, result!.Id);
        }

        [Fact]
        public async Task GetLatestByUserIdAsync_ReturnsAcrossAllStatuses()
        {
            await SeedUserAsync("user-1");
            await SeedRequestAsync("user-1", VerificationStatus.Rejected,
                submittedAt: DateTime.UtcNow.AddMinutes(-10));
            var latest = await SeedRequestAsync("user-1", VerificationStatus.Pending,
                submittedAt: DateTime.UtcNow);

            var result = await _repo.GetLatestByUserIdAsync("user-1");

            Assert.Equal(latest.Id, result!.Id);
        }

        [Fact]
        public async Task GetLatestByUserIdAsync_NoRequests_ReturnsNull()
        {
            var result = await _repo.GetLatestByUserIdAsync("user-1");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetLatestByUserIdAsync_IncludesReviewedByAdmin()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("admin-1");
            await SeedRequestAsync("user-1", VerificationStatus.Approved,
                reviewedByAdminId: "admin-1");

            _context.ChangeTracker.Clear();

            var result = await _repo.GetLatestByUserIdAsync("user-1");

            Assert.NotNull(result!.ReviewedByAdmin);
            Assert.Equal("admin-1", result.ReviewedByAdmin!.Id);
        }

        [Fact]
        public async Task GetAllPendingAsync_ReturnsOnlyPending()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedUserAsync("user-3");
            await SeedRequestAsync("user-1", VerificationStatus.Pending);
            await SeedRequestAsync("user-2", VerificationStatus.Approved);
            await SeedRequestAsync("user-3", VerificationStatus.Rejected);

            var result = await _repo.GetAllPendingAsync();

            Assert.Single(result);
            Assert.Equal(VerificationStatus.Pending, result[0].Status);
        }

        [Fact]
        public async Task GetAllPendingAsync_OrderedBySubmittedAtAscending()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            var newer = await SeedRequestAsync("user-1",
                submittedAt: DateTime.UtcNow);
            var older = await SeedRequestAsync("user-2",
                submittedAt: DateTime.UtcNow.AddMinutes(-10));

            var result = await _repo.GetAllPendingAsync();

            Assert.Equal(older.Id, result[0].Id); //oldest first — FIFO
            Assert.Equal(newer.Id, result[1].Id);
        }

        [Fact]
        public async Task GetAllPendingAsync_IncludesUser()
        {
            await SeedUserAsync("user-1");
            await SeedRequestAsync("user-1", VerificationStatus.Pending);

            _context.ChangeTracker.Clear();

            var result = await _repo.GetAllPendingAsync();

            Assert.Single(result);
            Assert.NotNull(result[0].User);
            Assert.Equal("user-1", result[0].User.Id);
        }

        [Fact]
        public async Task GetAllPendingAsync_NoPendingRequests_ReturnsEmpty()
        {
            await SeedUserAsync("user-1");
            await SeedRequestAsync("user-1", VerificationStatus.Approved);

            var result = await _repo.GetAllPendingAsync();

            Assert.Empty(result);
        }

 
        [Fact]
        public async Task GetAllByUserIdAsync_ReturnsAllRequestsForUser()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("user-2");
            await SeedRequestAsync("user-1", VerificationStatus.Rejected);
            await SeedRequestAsync("user-1", VerificationStatus.Pending);
            await SeedRequestAsync("user-2", VerificationStatus.Pending); //different user

            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal("user-1", r.UserId));
        }

        [Fact]
        public async Task GetAllByUserIdAsync_OrderedBySubmittedAtDescending()
        {
            await SeedUserAsync("user-1");
            var older = await SeedRequestAsync("user-1",
                submittedAt: DateTime.UtcNow.AddMinutes(-10));
            var newer = await SeedRequestAsync("user-1",
                submittedAt: DateTime.UtcNow);

            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_NoRequests_ReturnsEmpty()
        {
            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_IncludesReviewedByAdmin()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("admin-1");
            await SeedRequestAsync("user-1", VerificationStatus.Approved,
                reviewedByAdminId: "admin-1");

            _context.ChangeTracker.Clear();

            var result = await _repo.GetAllByUserIdAsync("user-1");

            Assert.Single(result);
            Assert.NotNull(result[0].ReviewedByAdmin);
            Assert.Equal("admin-1", result[0].ReviewedByAdmin!.Id);
        }


        [Fact]
        public async Task AddAsync_SaveChangesAsync_PersistsRequest()
        {
            await SeedUserAsync("user-1");

            var request = new VerificationRequest
            {
                UserId = "user-1",
                DocumentUrl = "http://test.com/passport.jpg",
                DocumentType = VerificationDocumentType.Passport,
                Status = VerificationStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(request);
            await _repo.SaveChangesAsync();

            var saved = await _context.VerificationRequests
                .FirstOrDefaultAsync(v => v.UserId == "user-1");
            Assert.NotNull(saved);
            Assert.Equal("http://test.com/passport.jpg", saved!.DocumentUrl);
            Assert.Equal(VerificationDocumentType.Passport, saved.DocumentType);
            Assert.Equal(VerificationStatus.Pending, saved.Status);
        }

        [Fact]
        public async Task AddAsync_SaveChangesAsync_AllDocumentTypes()
        {
            await SeedUserAsync("user-1");

            foreach (var docType in Enum.GetValues<VerificationDocumentType>())
            {
                var request = new VerificationRequest
                {
                    UserId = "user-1",
                    DocumentUrl = $"http://test.com/{docType}.jpg",
                    DocumentType = docType,
                    Status = VerificationStatus.Pending,
                    SubmittedAt = DateTime.UtcNow
                };

                await _repo.AddAsync(request);
            }

            await _repo.SaveChangesAsync();

            var count = await _context.VerificationRequests.CountAsync();
            Assert.Equal(Enum.GetValues<VerificationDocumentType>().Length, count);
        }

        [Fact]
        public async Task Update_Approve_PersistsChanges()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("admin-1");
            var seeded = await SeedRequestAsync("user-1", VerificationStatus.Pending);

            seeded.Status = VerificationStatus.Approved;
            seeded.ReviewedByAdminId = "admin-1";
            seeded.ReviewedAt = DateTime.UtcNow;
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.VerificationRequests.FindAsync(seeded.Id);
            Assert.Equal(VerificationStatus.Approved, updated!.Status);
            Assert.Equal("admin-1", updated.ReviewedByAdminId);
            Assert.NotNull(updated.ReviewedAt);
        }

        [Fact]
        public async Task Update_Reject_PersistsAdminNote()
        {
            await SeedUserAsync("user-1");
            await SeedUserAsync("admin-1");
            var seeded = await SeedRequestAsync("user-1", VerificationStatus.Pending);

            seeded.Status = VerificationStatus.Rejected;
            seeded.AdminNote = "Document is expired.";
            seeded.ReviewedByAdminId = "admin-1";
            seeded.ReviewedAt = DateTime.UtcNow;
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.VerificationRequests.FindAsync(seeded.Id);
            Assert.Equal(VerificationStatus.Rejected, updated!.Status);
            Assert.Equal("Document is expired.", updated.AdminNote);
            Assert.NotNull(updated.ReviewedAt);
        }
    }
}