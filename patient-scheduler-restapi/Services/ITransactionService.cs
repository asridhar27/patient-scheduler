namespace PatientScheduler.Services
{
    public interface ITransactionService
    {
        Task<AppointmentResponseDto> ScheduleAppointmentWithBillingAsync(CreateAppointmentDto appointmentDto, decimal amount, DateTime dueDate);
        Task<bool> ProcessPaymentAsync(int invoiceId);
        Task<bool> CancelAppointmentWithRefundAsync(int appointmentId);
        Task<bool> CancelAppointmentWithRefundAsync(int appointmentId, bool useTransaction);
    }
}
