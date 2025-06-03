using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduSync.Models
{
    public enum CourseStatus
    {
        Draft,
        Published,
        Archived
    }

    public class Course
    {
        [Key]
        public Guid CourseId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public Guid InstructorId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(255)]
        public string MediaFileName { get; set; }

        [MaxLength(100)]
        public string MediaContentType { get; set; }

        public long? MediaFileSize { get; set; }

        [MaxLength(500)]
        public string MediaFileUrl { get; set; }

        // These properties might not exist in the database schema
        // Making them nullable to prevent errors
        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(50)]
        public string? Level { get; set; }

        [MaxLength(500)]
        public string? Prerequisites { get; set; }

        // Status property commented out as it was removed from DTOs
        // public CourseStatus Status { get; set; } = CourseStatus.Draft;

        // Navigation properties
        [ForeignKey("InstructorId")]
        public virtual User Instructor { get; set; }

        // Standard navigation property for enrollments
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new HashSet<Enrollment>();

        public virtual ICollection<Assessment> Assessments { get; set; } = new HashSet<Assessment>();
        // Ratings removed as requested
    }
}
