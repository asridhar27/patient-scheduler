namespace PatientScheduler.DTOs
{
    public class CreateInvoiceDto
    {
        [Required]
        public int PatientId { get; set; }
        
        [Required]
        public int AppointmentId { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        public DateTime DueDate { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateInvoiceDto
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal? Amount { get; set; }
        
        public DateTime? DueDate { get; set; }
        
        [StringLength(50)]
        public string? Status { get; set; }
        
        public DateTime? PaidDate { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class InvoiceResponseDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int AppointmentId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime AppointmentDateTime { get; set; }
    }
}
