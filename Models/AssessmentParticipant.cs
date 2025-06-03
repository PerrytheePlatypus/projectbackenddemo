using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduSync.Models
{
    public class AssessmentParticipant
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid AssessmentId { get; set; }
        
        [Required]
        public Guid StudentId { get; set; }
        
        public DateTime JoinedAt { get; set; }
        
        public DateTime? SubmittedAt { get; set; }
        
        public int? Score { get; set; }
        
        public string ConnectionId { get; set; } // SignalR connection ID
        
        // Navigation properties
        [ForeignKey("AssessmentId")]
        public virtual Assessment Assessment { get; set; }
        
        [ForeignKey("StudentId")]
        public virtual User Student { get; set; }
    }
}
