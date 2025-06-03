using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace EduSync.Models
{
    public class Result
    {
        [Key]
        public Guid ResultId { get; set; }

        [Required]
        public Guid AssessmentId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        // Alias for UserId to maintain compatibility
        [NotMapped]
        public Guid StudentId
        {
            get { return UserId; }
            set { UserId = value; }
        }

        [Required]
        public int Score { get; set; }

        [Required]
        public DateTime AttemptDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(max)")]
        public string Answers { get; set; } // JSON string containing student answers

        public DateTime? CompletedDate { get; set; }

        public bool IsCompleted { get; set; } = false;

        // Navigation properties
        [ForeignKey("AssessmentId")]
        public virtual Assessment Assessment { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        // Helper methods to work with JSON answers
        public Dictionary<string, string> GetAnswersDictionary()
        {
            if (string.IsNullOrEmpty(Answers))
                return new Dictionary<string, string>();

            return JsonSerializer.Deserialize<Dictionary<string, string>>(Answers);
        }

        public void SetAnswersFromDictionary(Dictionary<string, string> answersDictionary)
        {
            Answers = JsonSerializer.Serialize(answersDictionary);
        }
    }
}
