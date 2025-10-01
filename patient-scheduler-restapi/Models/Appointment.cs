namespace PatientScheduler.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        [Required]
        public int DoctorId { get; set; }
        
        [Required]
        public DateTime AppointmentDateTime { get; set; }
        
        [Required]
        public TimeSpan Duration { get; set; } = TimeSpan.FromHours(1);
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled, NoShow
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Doctor Doctor { get; set; } = null!;
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
