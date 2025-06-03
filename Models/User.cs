using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduSync.Models
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } // Student, Instructor

        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }

        // These fields are optional
        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; } = ""; // Default to empty string instead of null
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Course> InstructorCourses { get; set; } = new HashSet<Course>();
        public virtual ICollection<Enrollment> EnrolledCourses { get; set; } = new HashSet<Enrollment>();
        // Ratings removed as requested
        public virtual ICollection<Result> Results { get; set; } = new HashSet<Result>();
        public virtual ICollection<Course> CreatedCourses { get; set; } = new HashSet<Course>();
        public virtual ICollection<AssessmentAttempt> AssessmentAttempts { get; set; } = new HashSet<AssessmentAttempt>();
    }

    public static class UserRoles
    {
        public const string Student = "Student";
        public const string Instructor = "Instructor";
        public const string Admin = "Admin";
    }
}
