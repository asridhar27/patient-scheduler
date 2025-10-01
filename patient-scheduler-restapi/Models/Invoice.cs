namespace PatientScheduler.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        [Required]
        public int AppointmentId { get; set; }
        
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Paid, Overdue, Cancelled
        
        public DateTime DueDate { get; set; }
        
        public DateTime? PaidDate { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Appointment Appointment { get; set; } = null!;
    }
}
