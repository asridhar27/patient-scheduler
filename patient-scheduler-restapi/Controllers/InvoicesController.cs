namespace PatientScheduler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly PatientSchedulerContext _context;
        private readonly ITransactionService _transactionService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(
            PatientSchedulerContext context, 
            ITransactionService transactionService,
            ILogger<InvoicesController> logger)
        {
            _context = context;
            _transactionService = transactionService;
            _logger = logger;
        }

        /// <summary>
        /// Get all invoices
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceResponseDto>>> GetInvoices()
        {
            try
            {
                var invoices = await _context.Invoices
                    .Include(i => i.Patient)
                    .Include(i => i.Appointment)
                    .ThenInclude(a => a.Doctor)
                    .Select(i => new InvoiceResponseDto
                    {
                        Id = i.Id,
                        PatientId = i.PatientId,
                        AppointmentId = i.AppointmentId,
                        Amount = i.Amount,
                        Status = i.Status,
                        DueDate = i.DueDate,
                        PaidDate = i.PaidDate,
                        Notes = i.Notes,
                        CreatedAt = i.CreatedAt,
                        UpdatedAt = i.UpdatedAt,
                        PatientName = $"{i.Patient.FirstName} {i.Patient.LastName}",
                        DoctorName = $"{i.Appointment.Doctor.FirstName} {i.Appointment.Doctor.LastName}",
                        AppointmentDateTime = i.Appointment.AppointmentDateTime
                    })
                    .ToListAsync();

                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices");
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        /// <summary>
        /// Get a specific invoice by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceResponseDto>> GetInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Patient)
                    .Include(i => i.Appointment)
                    .ThenInclude(a => a.Doctor)
                    .Where(i => i.Id == id)
                    .Select(i => new InvoiceResponseDto
                    {
                        Id = i.Id,
                        PatientId = i.PatientId,
                        AppointmentId = i.AppointmentId,
                        Amount = i.Amount,
                        Status = i.Status,
                        DueDate = i.DueDate,
                        PaidDate = i.PaidDate,
                        Notes = i.Notes,
                        CreatedAt = i.CreatedAt,
                        UpdatedAt = i.UpdatedAt,
                        PatientName = $"{i.Patient.FirstName} {i.Patient.LastName}",
                        DoctorName = $"{i.Appointment.Doctor.FirstName} {i.Appointment.Doctor.LastName}",
                        AppointmentDateTime = i.Appointment.AppointmentDateTime
                    })
                    .FirstOrDefaultAsync();

                if (invoice == null)
                {
                    return NotFound($"Invoice with ID {id} not found");
                }

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving invoice {id}");
                return StatusCode(500, "An error occurred while retrieving the invoice");
            }
        }

        /// <summary>
        /// Create a new invoice
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<InvoiceResponseDto>> CreateInvoice(CreateInvoiceDto createInvoiceDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate patient exists
                var patient = await _context.Patients.FindAsync(createInvoiceDto.PatientId);
                if (patient == null)
                {
                    return NotFound($"Patient with ID {createInvoiceDto.PatientId} not found");
                }

                // Validate appointment exists
                var appointment = await _context.Appointments.FindAsync(createInvoiceDto.AppointmentId);
                if (appointment == null)
                {
                    return NotFound($"Appointment with ID {createInvoiceDto.AppointmentId} not found");
                }

                // Check if appointment belongs to the patient
                if (appointment.PatientId != createInvoiceDto.PatientId)
                {
                    return BadRequest("Appointment does not belong to the specified patient");
                }

                var invoice = new Invoice
                {
                    PatientId = createInvoiceDto.PatientId,
                    AppointmentId = createInvoiceDto.AppointmentId,
                    Amount = createInvoiceDto.Amount,
                    Status = "Pending",
                    DueDate = createInvoiceDto.DueDate,
                    Notes = createInvoiceDto.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Invoice created with ID: {invoice.Id}");

                // Get the appointment with doctor info for response
                var appointmentWithDoctor = await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.Id == appointment.Id);

                var response = new InvoiceResponseDto
                {
                    Id = invoice.Id,
                    PatientId = invoice.PatientId,
                    AppointmentId = invoice.AppointmentId,
                    Amount = invoice.Amount,
                    Status = invoice.Status,
                    DueDate = invoice.DueDate,
                    PaidDate = invoice.PaidDate,
                    Notes = invoice.Notes,
                    CreatedAt = invoice.CreatedAt,
                    UpdatedAt = invoice.UpdatedAt,
                    PatientName = $"{patient.FirstName} {patient.LastName}",
                    DoctorName = $"{appointmentWithDoctor!.Doctor.FirstName} {appointmentWithDoctor.Doctor.LastName}",
                    AppointmentDateTime = appointment.AppointmentDateTime
                };

                return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return StatusCode(500, "An error occurred while creating the invoice");
            }
        }

        /// <summary>
        /// Update an existing invoice
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<InvoiceResponseDto>> UpdateInvoice(int id, UpdateInvoiceDto updateInvoiceDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var invoice = await _context.Invoices
                    .Include(i => i.Patient)
                    .Include(i => i.Appointment)
                    .ThenInclude(a => a.Doctor)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                {
                    return NotFound($"Invoice with ID {id} not found");
                }

                // Update fields if provided
                if (updateInvoiceDto.Amount.HasValue)
                    invoice.Amount = updateInvoiceDto.Amount.Value;
                if (updateInvoiceDto.DueDate.HasValue)
                    invoice.DueDate = updateInvoiceDto.DueDate.Value;
                if (!string.IsNullOrEmpty(updateInvoiceDto.Status))
                    invoice.Status = updateInvoiceDto.Status;
                if (updateInvoiceDto.PaidDate.HasValue)
                    invoice.PaidDate = updateInvoiceDto.PaidDate.Value;
                if (updateInvoiceDto.Notes != null)
                    invoice.Notes = updateInvoiceDto.Notes;

                invoice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Invoice {id} updated successfully");

                var response = new InvoiceResponseDto
                {
                    Id = invoice.Id,
                    PatientId = invoice.PatientId,
                    AppointmentId = invoice.AppointmentId,
                    Amount = invoice.Amount,
                    Status = invoice.Status,
                    DueDate = invoice.DueDate,
                    PaidDate = invoice.PaidDate,
                    Notes = invoice.Notes,
                    CreatedAt = invoice.CreatedAt,
                    UpdatedAt = invoice.UpdatedAt,
                    PatientName = $"{invoice.Patient.FirstName} {invoice.Patient.LastName}",
                    DoctorName = $"{invoice.Appointment.Doctor.FirstName} {invoice.Appointment.Doctor.LastName}",
                    AppointmentDateTime = invoice.Appointment.AppointmentDateTime
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating invoice {id}");
                return StatusCode(500, "An error occurred while updating the invoice");
            }
        }

        /// <summary>
        /// Process payment for an invoice
        /// </summary>
        [HttpPost("{id}/process-payment")]
        public async Task<ActionResult> ProcessPayment(int id)
        {
            try
            {
                var result = await _transactionService.ProcessPaymentAsync(id);
                
                if (result)
                {
                    _logger.LogInformation($"Payment processed successfully for invoice {id}");
                    return Ok(new { message = "Payment processed successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to process payment" });
                }
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing payment for invoice {id}");
                return StatusCode(500, new { message = "An error occurred while processing the payment" });
            }
        }

        /// <summary>
        /// Delete an invoice
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null)
                {
                    return NotFound($"Invoice with ID {id} not found");
                }

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Invoice {id} deleted successfully");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting invoice {id}");
                return StatusCode(500, "An error occurred while deleting the invoice");
            }
        }
    }
}
