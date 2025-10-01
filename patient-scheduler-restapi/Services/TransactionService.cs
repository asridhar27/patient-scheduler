using Microsoft.EntityFrameworkCore.Storage;

namespace PatientScheduler.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly PatientSchedulerContext _context;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(PatientSchedulerContext context, ILogger<TransactionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AppointmentResponseDto> ScheduleAppointmentWithBillingAsync(CreateAppointmentDto appointmentDto, decimal amount, DateTime dueDate)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Starting appointment scheduling with billing transaction");

                // Step 1: Create the appointment
                var appointment = new Appointment
                {
                    PatientId = appointmentDto.PatientId,
                    DoctorId = appointmentDto.DoctorId,
                    AppointmentDateTime = appointmentDto.AppointmentDateTime,
                    Duration = TimeSpan.FromHours(1), // Always enforce 1-hour appointments
                    Notes = appointmentDto.Notes,
                    Status = "Scheduled",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment created with ID: {appointment.Id}");

                // Step 2: Create the invoice
                var invoice = new Invoice
                {
                    PatientId = appointmentDto.PatientId,
                    AppointmentId = appointment.Id,
                    Amount = amount,
                    Status = "Pending",
                    DueDate = dueDate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Invoice created with ID: {invoice.Id}");

                // Simulate payment processing (for demonstration)
                // In a real scenario, this would be an external payment processor call
                await Task.Delay(100); // Simulate network call
                
                // Mark invoice as paid for demo purposes
                invoice.Status = "Paid";
                invoice.PaidDate = DateTime.UtcNow;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                _logger.LogInformation("Transaction completed successfully");

                // Return the appointment with related data
                return await GetAppointmentResponseAsync(appointment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed, rolling back changes");
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ProcessPaymentAsync(int invoiceId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation($"Processing payment for invoice ID: {invoiceId}");

                var invoice = await _context.Invoices.FindAsync(invoiceId);
                if (invoice == null)
                {
                    throw new ArgumentException($"Invoice with ID {invoiceId} not found");
                }

                if (invoice.Status == "Paid")
                {
                    _logger.LogWarning($"Invoice {invoiceId} is already paid");
                    return true;
                }

                // Simulate payment processing
                // In a real scenario, this would integrate with a payment gateway
                await Task.Delay(100); // Simulate network call

                invoice.Status = "Paid";
                invoice.PaidDate = DateTime.UtcNow;
                invoice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Payment processed successfully for invoice {invoiceId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Payment processing failed for invoice {invoiceId}");
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> CancelAppointmentWithRefundAsync(int appointmentId)
        {
            return await CancelAppointmentWithRefundAsync(appointmentId, useTransaction: true);
        }

        public async Task<bool> CancelAppointmentWithRefundAsync(int appointmentId, bool useTransaction)
        {
            IDbContextTransaction? transaction = null;
            
            try
            {
                if (useTransaction)
                {
                    transaction = await _context.Database.BeginTransactionAsync();
                }

                _logger.LogInformation($"Cancelling appointment ID: {appointmentId} with refund");

                var appointment = await _context.Appointments
                    .Include(a => a.Invoices)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    throw new ArgumentException($"Appointment with ID {appointmentId} not found");
                }

                // Cancel the appointment
                appointment.Status = "Cancelled";
                appointment.UpdatedAt = DateTime.UtcNow;

                // Find and free up the associated time slot
                var timeSlot = await _context.TimeSlots
                    .FirstOrDefaultAsync(t => t.AppointmentId == appointmentId);
                
                if (timeSlot != null)
                {
                    timeSlot.Status = "Active";
                    timeSlot.AppointmentId = null;
                    timeSlot.UpdatedAt = DateTime.UtcNow;
                }

                // Process refunds for any paid invoices
                foreach (var invoice in appointment.Invoices.Where(i => i.Status == "Paid"))
                {
                    // Simulate refund processing
                    await Task.Delay(50); // Simulate network call
                    
                    invoice.Status = "Refunded";
                    invoice.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                _logger.LogInformation($"Appointment {appointmentId} cancelled and refunds processed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to cancel appointment {appointmentId}");
                
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                
                return false;
            }
        }

        private async Task<AppointmentResponseDto> GetAppointmentResponseAsync(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                throw new ArgumentException($"Appointment with ID {appointmentId} not found");
            }

            return new AppointmentResponseDto
            {
                Id = appointment.Id,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                AppointmentDateTime = appointment.AppointmentDateTime,
                Duration = appointment.Duration,
                Notes = appointment.Notes,
                Status = appointment.Status,
                CreatedAt = appointment.CreatedAt,
                UpdatedAt = appointment.UpdatedAt,
                PatientName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}",
                DoctorName = $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}",
                DoctorSpecialization = appointment.Doctor.Specialization
            };
        }
    }
}
