using System.ComponentModel.DataAnnotations;

namespace PatientScheduler.Models
{
    public class BulkOperationRequest
    {
        [Required]
        public string OperationType { get; set; } = string.Empty; // "BulkCancel", "BulkReschedule", "BulkBillingUpdate"
        
        [Required]
        public List<int> AppointmentIds { get; set; } = new List<int>();
        
        public string? Reason { get; set; }
        public DateTime? NewDateTime { get; set; }
        public string? NewStatus { get; set; }
        public decimal? BillingAdjustment { get; set; }
        public string? Notes { get; set; }
    }

    public class BulkOperationResult
    {
        public int JobId { get; set; }
        public string Status { get; set; } = string.Empty; // "Queued", "Processing", "Completed", "Failed"
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int FailedRecords { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class JobStatus
    {
        public int Id { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int FailedRecords { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Result { get; set; }
    }
}
