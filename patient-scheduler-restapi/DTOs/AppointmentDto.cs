namespace PatientScheduler.DTOs
{
    public class CreateAppointmentDto
    {
        [Required]
        public int PatientId { get; set; }
        
        [Required]
        public int DoctorId { get; set; }
        
        [Required]
        public DateTime AppointmentDateTime { get; set; }
        
        public string Duration { get; set; } = "01:00:00";
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        /// <summary>
        /// Converts the Duration string to TimeSpan
        /// </summary>
        public TimeSpan GetDurationAsTimeSpan()
        {
            if (TimeSpan.TryParse(Duration, out var timeSpan))
            {
                return timeSpan;
            }
            return TimeSpan.FromHours(1); // Default to 1 hour if parsing fails
        }
    }

    public class UpdateAppointmentDto
    {
        public DateTime? AppointmentDateTime { get; set; }
        
        public string? Duration { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        [StringLength(50)]
        public string? Status { get; set; }
        
        /// <summary>
        /// Converts the Duration string to TimeSpan if provided
        /// </summary>
        public TimeSpan? GetDurationAsTimeSpan()
        {
            if (string.IsNullOrEmpty(Duration))
                return null;
                
            if (TimeSpan.TryParse(Duration, out var timeSpan))
            {
                return timeSpan;
            }
            return null;
        }
    }

    public class AppointmentResponseDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorSpecialization { get; set; } = string.Empty;
    }
}
