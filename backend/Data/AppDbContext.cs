using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Data
{
    // The bridge between our C# classes and the PostgreSQL tables.
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Every DbSet becomes a table.
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Lecture> Lectures { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<LectureProgress> LectureProgresses { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<EmailCode> EmailCodes { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Two users cannot have the same email.
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // A course belongs to one instructor. Do not allow deleting
            // an instructor that still has courses.
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Instructor)
                .WithMany(u => u.CoursesTaught)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            // If a category is deleted, its courses simply have no category.
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Category)
                .WithMany(cat => cat.Courses)
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Money columns: up to 10 digits with 2 decimals.
            modelBuilder.Entity<Course>().Property(c => c.Price).HasPrecision(10, 2);
            modelBuilder.Entity<SubscriptionPlan>().Property(p => p.Price).HasPrecision(10, 2);
            modelBuilder.Entity<Payment>().Property(p => p.Amount).HasPrecision(10, 2);

            // A student can enroll in the same course only once.
            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new { e.UserId, e.CourseId })
                .IsUnique();

            // One progress row per student per lecture.
            modelBuilder.Entity<LectureProgress>()
                .HasIndex(p => new { p.UserId, p.LectureId })
                .IsUnique();

            // One review per student per course.
            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.UserId, r.CourseId })
                .IsUnique();

            // Keep payment history even if the course is deleted later.
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Course)
                .WithMany()
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Plan)
                .WithMany()
                .HasForeignKey(p => p.PlanId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-time codes are always looked up by email + purpose.
            modelBuilder.Entity<EmailCode>()
                .HasIndex(c => new { c.Email, c.Purpose });

            // ---------- Coupons ----------
            modelBuilder.Entity<Coupon>()
                .HasIndex(c => c.Code)
                .IsUnique();

            // A course coupon is deleted together with its course.
            modelBuilder.Entity<Coupon>()
                .HasOne(c => c.Course)
                .WithMany()
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Coupon>()
                .HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => c.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>().Property(p => p.OriginalAmount).HasPrecision(10, 2);
            modelBuilder.Entity<Payment>().Property(p => p.DiscountAmount).HasPrecision(10, 2);

            // Keep the payment even if its coupon is deleted.
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Coupon)
                .WithMany()
                .HasForeignKey(p => p.CouponId)
                .OnDelete(DeleteBehavior.SetNull);

            // ---------- Q&A ----------
            // If the lecture is deleted, the question stays as a general course question.
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Lecture)
                .WithMany()
                .HasForeignKey(q => q.LectureId)
                .OnDelete(DeleteBehavior.SetNull);

            // ---------- Quizzes: one quiz per section ----------
            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Section)
                .WithOne(s => s.Quiz)
                .HasForeignKey<Quiz>(q => q.SectionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------- Wishlist: a course only once per user ----------
            modelBuilder.Entity<WishlistItem>()
                .HasIndex(w => new { w.UserId, w.CourseId })
                .IsUnique();

            // ---------- Notifications are read per user, newest first ----------
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.CreatedAt });

            // A plan that someone has bought cannot be deleted (deactivate it instead).
            modelBuilder.Entity<UserSubscription>()
                .HasOne(s => s.Plan)
                .WithMany()
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
