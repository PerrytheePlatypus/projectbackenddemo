using EduSync.Models;
using Microsoft.EntityFrameworkCore;

namespace EduSync.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<Assessment> Assessments { get; set; } = null!;
        public DbSet<Result> Results { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<Enrollment> Enrollments { get; set; } = null!;
        // Ratings DbSet removed as requested

        public DbSet<AssessmentAttempt> AssessmentAttempts { get; set; } = null!;
        public DbSet<AssessmentParticipant> AssessmentParticipants { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configurations
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasMaxLength(20)
                .HasConversion<string>();
                
            // Make Bio and ProfilePicture nullable
            modelBuilder.Entity<User>()
                .Property(u => u.Bio)
                .IsRequired(false);
                
            modelBuilder.Entity<User>()
                .Property(u => u.ProfilePicture)
                .IsRequired(false);

            // Course configurations
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Instructor)
                .WithMany(u => u.InstructorCourses)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Enrollment configuration
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(u => u.EnrolledCourses)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add index for Enrollment
            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => e.CourseId);

            // Rating configurations removed as requested

            // Assessment configurations
            modelBuilder.Entity<Assessment>()
                .HasOne(a => a.Course)
                .WithMany(c => c.Assessments)
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add index for Assessment
            modelBuilder.Entity<Assessment>()
                .HasIndex(a => a.CourseId);

            // Result configurations
            modelBuilder.Entity<Result>()
                .HasOne(r => r.Assessment)
                .WithMany(a => a.Results)
                .HasForeignKey(r => r.AssessmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Result>()
                .HasOne(r => r.User)
                .WithMany(u => u.Results)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add index for Result
            modelBuilder.Entity<Result>()
                .HasIndex(r => new { r.AssessmentId, r.UserId });

            // Question configurations
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Assessment)
                .WithMany(a => a.Questions)
                .HasForeignKey(q => q.AssessmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // AssessmentAttempt configuration
            modelBuilder.Entity<AssessmentAttempt>()
                .HasKey(aa => aa.AttemptId);

            modelBuilder.Entity<AssessmentAttempt>()
                .HasOne(aa => aa.Student)
                .WithMany(s => s.AssessmentAttempts)
                .HasForeignKey(aa => aa.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssessmentAttempt>()
                .HasOne(aa => aa.Assessment)
                .WithMany(a => a.Attempts)
                .HasForeignKey(aa => aa.AssessmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add index for AssessmentAttempt
            modelBuilder.Entity<AssessmentAttempt>()
                .HasIndex(aa => new { aa.StudentId, aa.AssessmentId });
        }
    }
}
