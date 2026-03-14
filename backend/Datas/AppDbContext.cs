using backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }


        //DbSets
        public DbSet<Category> Categories { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<ItemPhoto> ItemPhotos { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanSnapshotPhoto> LoanSnapshotPhotos { get; set; }
        public DbSet<Fine> Fines { get; set; }
        public DbSet<ScoreHistory> ScoreHistories { get; set; }
        public DbSet<Dispute> Disputes { get; set; }
        public DbSet<DisputePhoto> DisputePhotos { get; set; }
        public DbSet<LoanMessage> LoanMessages { get; set; }
        public DbSet<DirectConversation> DirectConversations { get; set; }
        public DbSet<DirectMessage> DirectMessages { get; set; }
        public DbSet<SupportThread> SupportThreads { get; set; }
        public DbSet<SupportMessage> SupportMessages { get; set; }
        public DbSet<UserBlock> UserBlocks { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Appeal> Appeals { get; set; }
        public DbSet<UserFavoriteItem> UserFavoriteItems { get; set; }
        public DbSet<UserRecentlyViewedItem> UserRecentlyViewedItems { get; set; }

        public DbSet<ItemReview> ItemReviews { get; set; }
        public DbSet<UserReview> UserReviews { get; set; }
        public DbSet<VerificationRequest> VerificationRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            //Must call base first — configures all Identity tables
            base.OnModelCreating(builder);

            //ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FullName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(u => u.Gender)
                      .HasMaxLength(20);

                entity.Property(u => u.Address)
                      .HasMaxLength(255);

                entity.Property(u => u.AvatarUrl)
                      .HasMaxLength(1000);

                entity.Property(u => u.Score)
                      .HasDefaultValue(100)
                      .ValueGeneratedNever();

                entity.ToTable(t => t.HasCheckConstraint("CK_User_Score", "[Score] >= 0 AND [Score] <= 100"));

                entity.Property(u => u.UnpaidFinesTotal)
                      .HasColumnType("decimal(10,2)")
                      .HasDefaultValue(0)
                      .ValueGeneratedNever();

                entity.Property(u => u.DeletedByAdminId)
                      .HasMaxLength(450)
                      .IsRequired(false);

                entity.Property(u => u.RefreshToken)
                      .HasMaxLength(500)
                      .IsRequired(false);

                //Global query filter — automatically excludes soft-deleted users
                entity.HasQueryFilter(u => !u.IsDeleted);
            });

            //Category
            builder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(c => c.Icon)
                      .HasMaxLength(10);

                entity.HasIndex(c => c.Name).IsUnique();
            });

            //Item
            builder.Entity<Item>(entity =>
            {
                entity.HasKey(i => i.Id);

                entity.Property(i => i.Title)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(i => i.Description)
                      .IsRequired()
                      .HasMaxLength(2000);

                entity.Property(i => i.CurrentValue)
                      .HasColumnType("decimal(10,2)");

                entity.Property(i => i.Condition)
                      .HasConversion<string>();

                entity.Property(i => i.Status)
                      .HasConversion<string>();

                entity.Property(i => i.PickupAddress)
                      .HasMaxLength(500);

                entity.Property(i => i.AdminNote)
                      .HasMaxLength(1000);

                entity.Property(i => i.QrCode)
                      .IsRequired()
                      .HasMaxLength(12);

                entity.HasIndex(i => i.QrCode).IsUnique();

                entity.Property(i => i.RowVersion).IsRowVersion();

                entity.HasOne(i => i.Owner)
                      .WithMany(u => u.OwnedItems)
                      .HasForeignKey(i => i.OwnerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Category)
                      .WithMany(c => c.Items)
                      .HasForeignKey(i => i.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(i => new { i.Status, i.IsActive });
                entity.HasIndex(i => new { i.PickupLatitude, i.PickupLongitude });
            });

            //ItemPhoto
            builder.Entity<ItemPhoto>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.PhotoUrl)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.HasOne(p => p.Item)
                      .WithMany(i => i.Photos)
                      .HasForeignKey(p => p.ItemId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            //Loan
            builder.Entity<Loan>(entity =>
            {
                entity.HasKey(l => l.Id);

                entity.Property(l => l.Status)
                      .HasConversion<string>();

                entity.Property(l => l.SnapshotCondition)
                      .HasConversion<string>();

                entity.Property(l => l.DecisionNote)
                      .HasMaxLength(1000);

                entity.Property(l => l.ExtensionRequestStatus)
                      .HasConversion<string>()
                      .IsRequired(false);

                entity.HasOne(l => l.Item)
                      .WithMany(i => i.Loans)
                      .HasForeignKey(l => l.ItemId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.Borrower)
                      .WithMany(u => u.BorrowedLoans)
                      .HasForeignKey(l => l.BorrowerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(l => l.Status);
                entity.HasIndex(l => new { l.Status, l.EndDate });
            });

            //LoanSnapshotPhoto
            builder.Entity<LoanSnapshotPhoto>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.PhotoUrl)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.HasOne(p => p.Loan)
                      .WithMany(l => l.SnapshotPhotos)
                      .HasForeignKey(p => p.LoanId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            //Fine
            builder.Entity<Fine>(entity =>
            {
                entity.HasKey(f => f.Id);

                entity.Property(f => f.Type)
                      .HasConversion<string>();

                entity.Property(f => f.Status)
                      .HasConversion<string>();

                entity.Property(f => f.Amount)
                      .HasColumnType("decimal(10,2)");

                entity.Property(f => f.ItemValueAtTimeOfFine)
                      .HasColumnType("decimal(10,2)");

                entity.Property(f => f.AdminNote)
                      .HasMaxLength(1000);

                entity.Property(f => f.PaymentProofImageUrl)
                      .HasMaxLength(1000)
                      .IsRequired(false);

                entity.Property(f => f.PaymentDescription)
                      .HasMaxLength(1000)
                      .IsRequired(false);

                entity.Property(f => f.RejectionReason)
                      .HasMaxLength(1000)
                      .IsRequired(false);

                entity.HasOne(f => f.Loan)
                      .WithMany(l => l.Fines)
                      .HasForeignKey(f => f.LoanId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.User)
                      .WithMany(u => u.Fines)
                      .HasForeignKey(f => f.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Dispute)
                      .WithMany(d => d.Fines)
                      .HasForeignKey(f => f.DisputeId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(f => new { f.UserId, f.Status });
            });

            //ScoreHistory
            builder.Entity<ScoreHistory>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.Property(s => s.PointsChanged).IsRequired();

                entity.Property(s => s.Reason)
                      .HasConversion<string>();

                entity.Property(s => s.Note)
                      .HasMaxLength(500);

                entity.HasOne(s => s.User)
                      .WithMany(u => u.ScoreHistory)
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Loan)
                      .WithMany()
                      .HasForeignKey(s => s.LoanId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(s => new { s.UserId, s.CreatedAt });
            });

            //Dispute
            builder.Entity<Dispute>(entity =>
            {
                entity.HasKey(d => d.Id);

                entity.Property(d => d.FiledAs)
                      .HasConversion<string>();

                entity.Property(d => d.Status)
                      .HasConversion<string>();

                entity.Property(d => d.AdminVerdict)
                      .HasConversion<string>()
                      .IsRequired(false);

                entity.Property(d => d.Description)
                      .IsRequired()
                      .HasMaxLength(2000);

                entity.Property(d => d.ResponseDescription)
                      .HasMaxLength(2000);

                entity.Property(d => d.AdminNote)
                      .HasMaxLength(2000);

                entity.Property(d => d.CustomFineAmount)
                      .HasColumnType("decimal(10,2)");

                entity.HasOne(d => d.Loan)
                      .WithMany(l => l.Disputes)
                      .HasForeignKey(d => d.LoanId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.FiledBy)
                      .WithMany()
                      .HasForeignKey(d => d.FiledById)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ResolvedByAdmin)
                      .WithMany()
                      .HasForeignKey(d => d.ResolvedByAdminId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            //DisputePhoto
            builder.Entity<DisputePhoto>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.PhotoUrl)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.Property(p => p.Caption)
                      .HasMaxLength(500);

                entity.HasOne(p => p.Dispute)
                      .WithMany(d => d.Photos)
                      .HasForeignKey(p => p.DisputeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.SubmittedBy)
                      .WithMany()
                      .HasForeignKey(p => p.SubmittedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //LoanMessage (renamed from Message)
            builder.Entity<LoanMessage>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Content)
                      .IsRequired()
                      .HasMaxLength(2000);

                //Cascade: deleting a loan removes its chat thread
                entity.HasOne(m => m.Loan)
                      .WithMany(l => l.Messages)
                      .HasForeignKey(m => m.LoanId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                      .WithMany(u => u.SentLoanMessages)
                      .HasForeignKey(m => m.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(m => new { m.LoanId, m.SentAt });
            });

            //DirectConversation
            builder.Entity<DirectConversation>(entity =>
            {
                entity.HasKey(c => c.Id);

                //One thread per user pair
                entity.HasIndex(c => new { c.InitiatedById, c.OtherUserId }).IsUnique();

                entity.HasOne(c => c.InitiatedBy)
                      .WithMany()
                      .HasForeignKey(c => c.InitiatedById)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.OtherUser)
                      .WithMany()
                      .HasForeignKey(c => c.OtherUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //DirectMessage
            builder.Entity<DirectMessage>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Content)
                      .IsRequired()
                      .HasMaxLength(2000);

                //Cascade: deleting a conversation removes all its messages
                entity.HasOne(m => m.Conversation)
                      .WithMany(c => c.Messages)
                      .HasForeignKey(m => m.ConversationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                      .WithMany(u => u.SentDirectMessages)
                      .HasForeignKey(m => m.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(m => new { m.ConversationId, m.SentAt });
            });

            //SupportThread
            builder.Entity<SupportThread>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Status)
                      .HasConversion<string>();

                entity.HasOne(t => t.User)
                      .WithMany()
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                //Nullable — null when thread is open/unclaimed
                entity.HasOne(t => t.ClaimedByAdmin)
                      .WithMany()
                      .HasForeignKey(t => t.ClaimedByAdminId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => new { t.UserId, t.Status });
            });

            //SupportMessage
            builder.Entity<SupportMessage>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Content)
                      .IsRequired()
                      .HasMaxLength(2000);

                entity.HasOne(m => m.SupportThread)
                      .WithMany(t => t.Messages)
                      .HasForeignKey(m => m.SupportThreadId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                      .WithMany(u => u.SentSupportMessages)
                      .HasForeignKey(m => m.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(m => new { m.SupportThreadId, m.SentAt });
            });

            //UserBlock
            builder.Entity<UserBlock>(entity =>
            {
                //Composite PK — one block record per pair
                entity.HasKey(b => new { b.BlockerId, b.BlockedId });

                entity.HasOne(b => b.Blocker)
                      .WithMany(u => u.BlockedUsers)
                      .HasForeignKey(b => b.BlockerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Blocked)
                      .WithMany(u => u.BlockedBy)
                      .HasForeignKey(b => b.BlockedId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //Notification
            builder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);

                entity.Property(n => n.Type)
                      .HasConversion<string>();

                entity.Property(n => n.ReferenceType)
                      .HasConversion<string>()
                      .IsRequired(false);

                entity.Property(n => n.Message)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.HasOne(n => n.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
            });

            //Appeal
            builder.Entity<Appeal>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Status)
                      .HasConversion<string>();

                entity.Property(a => a.AppealType)
                      .HasConversion<string>();

                entity.Property(a => a.FineResolution)
                      .HasConversion<string>()
                      .IsRequired(false);

                entity.Property(a => a.Message)
                      .IsRequired()
                      .HasMaxLength(2000);

                entity.Property(a => a.AdminNote)
                      .HasMaxLength(1000);

                entity.Property(a => a.CustomFineAmount)
                      .HasColumnType("decimal(10,2)")
                      .IsRequired(false);

                entity.HasOne(a => a.User)
                      .WithMany(u => u.Appeals)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.ResolvedByAdmin)
                      .WithMany()
                      .HasForeignKey(a => a.ResolvedByAdminId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.Fine)
                      .WithMany()
                      .HasForeignKey(a => a.FineId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(a => a.Status);
            });

            //UserFavoriteItem
            builder.Entity<UserFavoriteItem>(entity =>
            {
                entity.HasKey(f => new { f.UserId, f.ItemId });

                entity.HasOne(f => f.User)
                      .WithMany(u => u.FavoriteItems)
                      .HasForeignKey(f => f.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Item)
                      .WithMany(i => i.FavoritedBy)
                      .HasForeignKey(f => f.ItemId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            //userRecentlyViewedItem
            builder.Entity<UserRecentlyViewedItem>(entity =>
            {
                entity.HasKey(r => new { r.UserId, r.ItemId });

                entity.HasOne(r => r.User)
                      .WithMany(u => u.RecentlyViewed)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Item)
                      .WithMany()
                      .HasForeignKey(r => r.ItemId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(r => new { r.UserId, r.ViewedAt });
            });

            //ItemReview
            builder.Entity<ItemReview>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.HasIndex(r => r.LoanId).IsUnique();

                entity.ToTable(t => t.HasCheckConstraint("CK_ItemReview_Rating", "[Rating] >= 1 AND [Rating] <= 5"));

                entity.Property(r => r.Comment)
                      .HasMaxLength(1000);

                entity.HasOne(r => r.Item)
                      .WithMany(i => i.Reviews)
                      .HasForeignKey(r => r.ItemId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Loan)
                      .WithMany()
                      .HasForeignKey(r => r.LoanId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Reviewer)
                      .WithMany(u => u.ItemReviews)
                      .HasForeignKey(r => r.ReviewerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //UserReview
            builder.Entity<UserReview>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.HasIndex(r => new { r.LoanId, r.ReviewerId }).IsUnique();

                entity.ToTable(t => t.HasCheckConstraint("CK_UserReview_Rating", "[Rating] >= 1 AND [Rating] <= 5"));

                entity.Property(r => r.Comment)
                      .HasMaxLength(1000);

                entity.HasOne(r => r.Loan)
                      .WithMany()
                      .HasForeignKey(r => r.LoanId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Reviewer)
                      .WithMany(u => u.ReviewsGiven)
                      .HasForeignKey(r => r.ReviewerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.ReviewedUser)
                      .WithMany(u => u.ReviewsReceived)
                      .HasForeignKey(r => r.ReviewedUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //VerificationRequest
            builder.Entity<VerificationRequest>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.Property(v => v.DocumentUrl)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.Property(v => v.DocumentType)
                      .HasConversion<string>();

                entity.Property(v => v.Status)
                      .HasConversion<string>();

                entity.Property(v => v.AdminNote)
                      .HasMaxLength(1000);

                entity.Property(v => v.ReviewedByAdminId)
                      .HasMaxLength(450)
                      .IsRequired(false);

                entity.HasOne(v => v.User)
                      .WithMany(u => u.VerificationRequests)
                      .HasForeignKey(v => v.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.ReviewedByAdmin)
                      .WithMany()
                      .HasForeignKey(v => v.ReviewedByAdminId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(v => v.Status);
                entity.HasIndex(v => new { v.UserId, v.Status });
            });
        }
    }
}