using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EduSync.DTOs
{
    // Request DTOs
    public class AssessmentCreateDto
    {
        [Required(ErrorMessage = "Course ID is required")]
        public Guid CourseId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; }

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Maximum score is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Maximum score must be greater than 0")]
        public int MaxScore { get; set; }

        [Range(0, 180, ErrorMessage = "Time limit must be between 0 and 180 minutes")]
        public int? TimeLimit { get; set; }

        [Required(ErrorMessage = "Questions are required")]
        public List<QuestionCreateDto> Questions { get; set; }
    }

    public class QuestionCreateDto
    {
        [Required(ErrorMessage = "Question text is required")]
        [MaxLength(500, ErrorMessage = "Question text cannot exceed 500 characters")]
        public string QuestionText { get; set; }

        [Required(ErrorMessage = "Options are required")]
        public List<string> Options { get; set; }

        [Required(ErrorMessage = "Correct answer is required")]
        [MaxLength(100, ErrorMessage = "Correct answer cannot exceed 100 characters")]
        public string CorrectAnswer { get; set; }

        [Range(1, 100, ErrorMessage = "Points must be between 1 and 100")]
        public int Points { get; set; } = 1;

        public int Type { get; set; } = 0; // Default to MultipleChoice
    }

    public class AssessmentUpdateDto
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; }

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Maximum score is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Maximum score must be greater than 0")]
        public int MaxScore { get; set; }

        [Range(0, 180, ErrorMessage = "Time limit must be between 0 and 180 minutes")]
        public int? TimeLimit { get; set; }

        // Optional: questions to update
        public List<QuestionUpdateDto> Questions { get; set; }
    }

    public class QuestionUpdateDto
    {
        public Guid? QuestionId { get; set; } // Existing question ID (null if new)

        [Required(ErrorMessage = "Question text is required")]
        [MaxLength(500, ErrorMessage = "Question text cannot exceed 500 characters")]
        public string QuestionText { get; set; }

        [Required(ErrorMessage = "Options are required")]
        public List<string> Options { get; set; } = new();

        [Required(ErrorMessage = "Correct answer is required")]
        [MaxLength(100, ErrorMessage = "Correct answer cannot exceed 100 characters")]
        public string CorrectAnswer { get; set; }

        [Range(1, 100, ErrorMessage = "Points must be between 1 and 100")]
        public int Points { get; set; } = 1;

        public int Type { get; set; } = 0; // Default to MultipleChoice
    }

    public class AssessmentAttemptDto
    {
        [Required(ErrorMessage = "Assessment ID is required")]
        public Guid AssessmentId { get; set; }
    }

    // Response DTOs
    public class AssessmentDto
    {
        public Guid AssessmentId { get; set; }
        public Guid CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int MaxScore { get; set; }
        public int? TimeLimit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public int QuestionCount { get; set; }
        public string CourseName { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? AttemptStarted { get; set; }
        public int? Score { get; set; }
        public DateTime? CompletedDate { get; set; }
        public double? ScorePercentage { get; set; }
        
        // Live assessment properties
        public string Status { get; set; }
        public bool IsLive { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string SessionId { get; set; }
    }

    public class AssessmentDetailDto
    {
        public Guid AssessmentId { get; set; }
        public Guid CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int MaxScore { get; set; }
        public int? TimeLimit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CourseName { get; set; }
        public List<QuestionDto> Questions { get; set; } = new();
    }

    public class QuestionDto
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; }
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; }
        public int Points { get; set; }
        public int Type { get; set; }
    }

    public class StudentAssessmentDto
    {
        public Guid AssessmentId { get; set; }
        public Guid CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int MaxScore { get; set; }
        public int? TimeLimit { get; set; }
        public string CourseName { get; set; }
        public List<StudentQuestionDto> Questions { get; set; }
        public bool IsCompleted { get; set; }
        
        // Live assessment properties
        public string Status { get; set; }
        public bool IsLive { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string SessionId { get; set; }
    }

    public class StudentQuestionDto
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; }
        public List<string> Options { get; set; }
        public int Points { get; set; }
        public int Type { get; set; }
    }
}
