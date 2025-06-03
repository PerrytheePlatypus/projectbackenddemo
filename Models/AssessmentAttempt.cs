using System;

namespace EduSync.Models
{
    public class AssessmentAttempt
    {
        public Guid AttemptId { get; set; }
        public Guid AssessmentId { get; set; }
        public Guid StudentId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } // Started, Completed, Abandoned
        public double? Score { get; set; }
        public int? TimeSpent { get; set; } // in minutes

        // Navigation properties
        public Assessment Assessment { get; set; }
        public User Student { get; set; }
    }
} 