namespace PatientScheduler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly PatientSchedulerContext _context;
        private readonly ITransactionService _transactionService;
        private readonly ILogger<AppointmentsController> _logger;

        private static readonly TimeSpan AppointmentSlotLength = TimeSpan.FromHours(1);
        private static readonly TimeSpan OfficeOpenTime = TimeSpan.FromHours(9);
        private static readonly TimeSpan OfficeCloseTime = TimeSpan.FromHours(17);

        public AppointmentsController(
            PatientSchedulerContext context,
            ITransactionService transactionService,
            ILogger<AppointmentsController> logger)
        {
            _context = context;
            _transactionService = transactionService;
            _logger = logger;
        }

        /// <summary>
        /// Get all appointments
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppointmentResponseDto>>> GetAppointments()
        {
            try
            {
                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Select(a => new AppointmentResponseDto
                    {
                        Id = a.Id,
                        PatientId = a.PatientId,
                        DoctorId = a.DoctorId,
                        AppointmentDateTime = a.AppointmentDateTime,
                        Duration = a.Duration,
                        Notes = a.Notes,
                        Status = a.Status,
                        CreatedAt = a.CreatedAt,
                        UpdatedAt = a.UpdatedAt,
                        PatientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                        DoctorName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                        DoctorSpecialization = a.Doctor.Specialization
                    })
                    .ToListAsync();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments");
                return StatusCode(500, "An error occurred while retrieving appointments");
            }
        }

        /// <summary>
        /// Get a specific appointment by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AppointmentResponseDto>> GetAppointment(int id)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Where(a => a.Id == id)
                    .Select(a => new AppointmentResponseDto
                    {
                        Id = a.Id,
                        PatientId = a.PatientId,
                        DoctorId = a.DoctorId,
                        AppointmentDateTime = a.AppointmentDateTime,
                        Duration = a.Duration,
                        Notes = a.Notes,
                        Status = a.Status,
                        CreatedAt = a.CreatedAt,
                        UpdatedAt = a.UpdatedAt,
                        PatientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                        DoctorName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                        DoctorSpecialization = a.Doctor.Specialization
                    })
                    .FirstOrDefaultAsync();

                if (appointment == null)
                {
                    return NotFound($"Appointment with ID {id} not found");
                }

                return Ok(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving appointment {id}");
                return StatusCode(500, "An error occurred while retrieving the appointment");
            }
        }

        /// <summary>
        /// Create a new appointment
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AppointmentResponseDto>> CreateAppointment(CreateAppointmentDto createAppointmentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate patient exists
                var patient = await _context.Patients.FindAsync(createAppointmentDto.PatientId);
                if (patient == null)
                {
                    return NotFound($"Patient with ID {createAppointmentDto.PatientId} not found");
                }

                // Validate doctor exists
                var doctor = await _context.Doctors.FindAsync(createAppointmentDto.DoctorId);
                if (doctor == null)
                {
                    return NotFound($"Doctor with ID {createAppointmentDto.DoctorId} not found");
                }

                // Always enforce 1-hour appointments
                var start = createAppointmentDto.AppointmentDateTime;
                var duration = AppointmentSlotLength;

                if (!IsWithinOfficeHours(start, duration))
                {
                    return BadRequest("Appointment must be within office hours (9:00 AM to 5:00 PM).");
                }

                var conflict = await FindConflictingAppointmentAsync(createAppointmentDto.DoctorId, start, duration);

                if (conflict != null)
                {
                    return Conflict($"Doctor has a conflicting appointment at {conflict.AppointmentDateTime:yyyy-MM-dd HH:mm}");
                }

                var appointment = new Appointment
                {
                    PatientId = createAppointmentDto.PatientId,
                    DoctorId = createAppointmentDto.DoctorId,
                    AppointmentDateTime = start,
                    Duration = duration,
                    Notes = createAppointmentDto.Notes,
                    Status = "Scheduled",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment created with ID: {appointment.Id}");

                var response = new AppointmentResponseDto
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
                    PatientName = $"{patient.FirstName} {patient.LastName}",
                    DoctorName = $"{doctor.FirstName} {doctor.LastName}",
                    DoctorSpecialization = doctor.Specialization
                };

                return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating appointment");
                return StatusCode(500, "An error occurred while creating the appointment");
            }
        }

        private static (DateTime Open, DateTime Close) GetOfficeHours(DateTime start)
        {
            var day = start.Date;
            return (day.Add(OfficeOpenTime), day.Add(OfficeCloseTime));
        }

        private static bool IsWithinOfficeHours(DateTime start, TimeSpan duration)
        {
            var (open, close) = GetOfficeHours(start);
            var end = start.Add(duration);
            return start >= open && end <= close;
        }

        private async Task<Appointment?> FindConflictingAppointmentAsync(int doctorId, DateTime start, TimeSpan duration)
        {
            var end = start.Add(duration);

            var appointmentsOnSameDay = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId &&
                            a.Status != "Cancelled" &&
                            a.AppointmentDateTime.Date == start.Date)
                .ToListAsync();

            return appointmentsOnSameDay.FirstOrDefault(a =>
            {
                var appointmentEnd = a.AppointmentDateTime.Add(a.Duration);
                return a.AppointmentDateTime < end && appointmentEnd > start;
            });
        }

        /// <summary>
        /// Schedule an appointment with automatic billing
        /// </summary>
        [HttpPost("schedule-with-billing")]
        public async Task<ActionResult<AppointmentResponseDto>> ScheduleAppointmentWithBilling(
            CreateAppointmentDto createAppointmentDto,
            [FromQuery] decimal amount,
            [FromQuery] DateTime? dueDate)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (amount <= 0)
                {
                    return BadRequest("Amount must be greater than 0");
                }

                // Validate office hours using shared logic
                var start = createAppointmentDto.AppointmentDateTime;
                var duration = AppointmentSlotLength;

                if (!IsWithinOfficeHours(start, duration))
                {
                    return BadRequest("Appointment must be within office hours (9:00 AM to 5:00 PM).");
                }

                var dueDateValue = dueDate ?? DateTime.UtcNow.AddDays(30);

                var appointment = await _transactionService.ScheduleAppointmentWithBillingAsync(
                    createAppointmentDto, amount, dueDateValue);

                // Get the created invoice for the response
                var invoice = await _context.Invoices
                    .Include(i => i.Patient)
                    .Include(i => i.Appointment)
                    .ThenInclude(a => a.Doctor)
                    .FirstOrDefaultAsync(i => i.AppointmentId == appointment.Id);

                var response = new
                {
                    appointment = appointment,
                    invoice = invoice != null ? new
                    {
                        id = invoice.Id,
                        amount = invoice.Amount,
                        status = invoice.Status,
                        dueDate = invoice.DueDate,
                        paidDate = invoice.PaidDate,
                        patientName = $"{invoice.Patient.FirstName} {invoice.Patient.LastName}",
                        doctorName = $"{invoice.Appointment.Doctor.FirstName} {invoice.Appointment.Doctor.LastName}",
                        appointmentDateTime = invoice.Appointment.AppointmentDateTime
                    } : null
                };

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Billing failure during appointment scheduling");
                return BadRequest(new { message = $"Appointment scheduling failed: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling appointment with billing");
                return StatusCode(500, new { message = "An error occurred while scheduling the appointment" });
            }
        }

        /// <summary>
        /// Update an existing appointment
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<AppointmentResponseDto>> UpdateAppointment(int id, UpdateAppointmentDto updateAppointmentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (appointment == null)
                {
                    return NotFound($"Appointment with ID {id} not found");
                }

                // Update fields if provided
                if (updateAppointmentDto.AppointmentDateTime.HasValue)
                    appointment.AppointmentDateTime = updateAppointmentDto.AppointmentDateTime.Value;
                var durationTimeSpan = updateAppointmentDto.GetDurationAsTimeSpan();
                if (durationTimeSpan.HasValue)
                    appointment.Duration = durationTimeSpan.Value;
                if (updateAppointmentDto.Notes != null)
                    appointment.Notes = updateAppointmentDto.Notes;
                if (!string.IsNullOrEmpty(updateAppointmentDto.Status))
                    appointment.Status = updateAppointmentDto.Status;

                appointment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment {id} updated successfully");

                var response = new AppointmentResponseDto
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

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating appointment {id}");
                return StatusCode(500, "An error occurred while updating the appointment");
            }
        }

        /// <summary>
        /// Cancel an appointment with automatic refund
        /// </summary>
        [HttpPost("{id}/cancel-with-refund")]
        public async Task<ActionResult> CancelAppointmentWithRefund(int id)
        {
            try
            {
                var result = await _transactionService.CancelAppointmentWithRefundAsync(id);

                if (result)
                {
                    _logger.LogInformation($"Appointment {id} cancelled and refunds processed");
                    return Ok(new { message = "Appointment cancelled and refunds processed successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to cancel appointment and process refunds" });
                }
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling appointment {id}");
                return StatusCode(500, new { message = "An error occurred while cancelling the appointment" });
            }
        }

        /// <summary>
        /// Delete an appointment
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAppointment(int id)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null)
                {
                    return NotFound($"Appointment with ID {id} not found");
                }

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment {id} deleted successfully");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting appointment {id}");
                return StatusCode(500, "An error occurred while deleting the appointment");
            }
        }
    }
}
