using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduSync.Models
{
    public class Assessment
    {
        [Key]
        public Guid AssessmentId { get; set; }

        [Required]
        public Guid CourseId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public int MaxScore { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Time limit in minutes (null means no time limit)
        public int? TimeLimit { get; set; }

        // These properties exist in the database but were removed from DTOs
        // They need to be uncommented to prevent SQL errors
        public DateTime? DueDate { get; set; }
        public int? PassingScore { get; set; }
        public string Status { get; set; } = "Draft";
        
        // Live assessment properties
        public bool IsLive { get; set; } = false;
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        [MaxLength(100)]
        public string? SessionId { get; set; } // Unique identifier for the live session (nullable)

        // Navigation properties
        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; }

        public virtual ICollection<Question> Questions { get; set; }

        public virtual ICollection<Result> Results { get; set; }

        public virtual ICollection<AssessmentAttempt> Attempts { get; set; }

        public Assessment()
        {
            Questions = new HashSet<Question>();
            Results = new HashSet<Result>();
            Attempts = new HashSet<AssessmentAttempt>();
        }
    }
}
