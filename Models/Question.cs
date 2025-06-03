using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;

namespace EduSync.Models
{
    public class Question
    {
        [Key]
        public Guid QuestionId { get; set; }

        [Required]
        public Guid AssessmentId { get; set; }

        [Required]
        [MaxLength(500)]
        public string QuestionText { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Options { get; set; } // JSON string containing options

        [Required]
        [MaxLength(100)]
        public string CorrectAnswer { get; set; }

        [Required]
        public int Points { get; set; } = 1;

        public QuestionType Type { get; set; } = QuestionType.MultipleChoice;

        // Navigation property
        [ForeignKey("AssessmentId")]
        public virtual Assessment Assessment { get; set; }

        // Helper methods to work with JSON options
        public List<string> GetOptionsAsList()
        {
            if (string.IsNullOrEmpty(Options))
                return new List<string>();

            try
            {
                // Try to deserialize as JSON array
                return JsonSerializer.Deserialize<List<string>>(Options);
            }
            catch
            {
                // Fallback: treat as comma-separated string
                return Options.Split(',').Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).ToList();
            }
        }

        public void SetOptionsFromList(List<string> optionsList)
        {
            Options = JsonSerializer.Serialize(optionsList);
        }
    }

    public enum QuestionType
    {
        MultipleChoice,
        TrueFalse,
        ShortAnswer,
        Essay
    }
}
