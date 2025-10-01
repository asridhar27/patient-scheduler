using System.ComponentModel.DataAnnotations;

namespace PatientScheduler.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string OperationType { get; set; } = string.Empty;
        
        [Required]
        public string Status { get; set; } = string.Empty; // Queued, Processing, Completed, Failed, Cancelled
        
        public string? Parameters { get; set; } // JSON string of operation parameters
        
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int FailedRecords { get; set; }
        
        public string? Errors { get; set; } // JSON string of error messages
        public string? Result { get; set; } // JSON string of operation result
        
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public string? CreatedBy { get; set; }
        public string? Notes { get; set; }
    }
}
