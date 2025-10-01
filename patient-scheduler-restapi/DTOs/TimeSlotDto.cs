namespace PatientScheduler.DTOs
{
    public class TimeSlotResponseDto
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty; // "Active", "Booked"
        public int? AppointmentId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorSpecialization { get; set; } = string.Empty;
    }
}