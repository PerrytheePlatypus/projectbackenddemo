using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EduSync.DTOs
{
    // Request DTOs
    public class ResultSubmitDto
    {
        [Required(ErrorMessage = "Assessment ID is required")]
        public Guid AssessmentId { get; set; }

        [Required(ErrorMessage = "Answers are required")]
        public Dictionary<string, string> Answers { get; set; }
        
        // Added to fix controller reference errors
        public string GetAnswersJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(Answers);
        }
    }

    // Response DTOs
    public class ResultDto
    {
        public Guid ResultId { get; set; }
        public Guid AssessmentId { get; set; }
        public string AssessmentTitle { get; set; }
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public DateTime AttemptDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool IsCompleted { get; set; }
        public double ScorePercentage { get; set; }
    }

    public class ResultDetailDto
    {
        public Guid ResultId { get; set; }
        public Guid AssessmentId { get; set; }
        public string AssessmentTitle { get; set; }
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public DateTime AttemptDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool IsCompleted { get; set; }
        public double ScorePercentage { get; set; }
        public List<QuestionResultDto> QuestionResults { get; set; } = new();
    }

    public class QuestionResultDto
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; }
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; }
        public string UserAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int Points { get; set; }
        public int EarnedPoints { get; set; }
    }

    // For instructors to view results by assessment
    public class AssessmentResultSummaryDto
    {
        public Guid AssessmentId { get; set; }
        public string AssessmentTitle { get; set; }
        public int TotalAttempts { get; set; }
        public int CompletedAttempts { get; set; }
        public double AverageScore { get; set; }
        public double HighestScore { get; set; }
        public double LowestScore { get; set; }
        public int MaxScore { get; set; }
        public List<StudentResultDto> StudentResults { get; set; } = new();
    }

    public class StudentResultDto
    {
        public Guid UserId { get; set; }
        public string StudentName { get; set; }
        public int Score { get; set; }
        public double ScorePercentage { get; set; }
        public DateTime AttemptDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool IsCompleted { get; set; }
    }
}
