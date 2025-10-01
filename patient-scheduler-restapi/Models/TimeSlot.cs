namespace PatientScheduler.Models
{
    public class TimeSlot
    {
        public int Id { get; set; }
        
        [Required]
        public int DoctorId { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; }
        
        [Required]
        public DateTime EndTime { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Booked
        
        public int? AppointmentId { get; set; } // Links to appointment when booked
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Doctor Doctor { get; set; } = null!;
        public virtual Appointment? Appointment { get; set; }
    }
}
