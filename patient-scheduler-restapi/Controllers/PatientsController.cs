namespace PatientScheduler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly PatientSchedulerContext _context;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(PatientSchedulerContext context, ILogger<PatientsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all patients
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PatientResponseDto>>> GetPatients()
        {
            try
            {
                var patients = await _context.Patients
                    .Select(p => new PatientResponseDto
                    {
                        Id = p.Id,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        Email = p.Email,
                        PhoneNumber = p.PhoneNumber,
                        DateOfBirth = p.DateOfBirth,
                        Address = p.Address,
                        MedicalHistory = p.MedicalHistory,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patients");
                return StatusCode(500, "An error occurred while retrieving patients");
            }
        }

        /// <summary>
        /// Get a specific patient by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PatientResponseDto>> GetPatient(int id)
        {
            try
            {
                var patient = await _context.Patients
                    .Where(p => p.Id == id)
                    .Select(p => new PatientResponseDto
                    {
                        Id = p.Id,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        Email = p.Email,
                        PhoneNumber = p.PhoneNumber,
                        DateOfBirth = p.DateOfBirth,
                        Address = p.Address,
                        MedicalHistory = p.MedicalHistory,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (patient == null)
                {
                    return NotFound($"Patient with ID {id} not found");
                }

                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving patient {id}");
                return StatusCode(500, "An error occurred while retrieving the patient");
            }
        }

        /// <summary>
        /// Create a new patient
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PatientResponseDto>> CreatePatient(CreatePatientDto createPatientDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if patient with same email already exists
                var existingPatient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Email == createPatientDto.Email);

                if (existingPatient != null)
                {
                    return Conflict($"A patient with email {createPatientDto.Email} already exists");
                }

                var patient = new Patient
                {
                    FirstName = createPatientDto.FirstName,
                    LastName = createPatientDto.LastName,
                    Email = createPatientDto.Email,
                    PhoneNumber = createPatientDto.PhoneNumber,
                    DateOfBirth = createPatientDto.DateOfBirth,
                    Address = createPatientDto.Address,
                    MedicalHistory = createPatientDto.MedicalHistory,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Patient created with ID: {patient.Id}");

                var response = new PatientResponseDto
                {
                    Id = patient.Id,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Email = patient.Email,
                    PhoneNumber = patient.PhoneNumber,
                    DateOfBirth = patient.DateOfBirth,
                    Address = patient.Address,
                    MedicalHistory = patient.MedicalHistory,
                    CreatedAt = patient.CreatedAt,
                    UpdatedAt = patient.UpdatedAt
                };

                return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                return StatusCode(500, "An error occurred while creating the patient");
            }
        }

        /// <summary>
        /// Update an existing patient
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PatientResponseDto>> UpdatePatient(int id, UpdatePatientDto updatePatientDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var patient = await _context.Patients.FindAsync(id);
                if (patient == null)
                {
                    return NotFound($"Patient with ID {id} not found");
                }

                // Check if email is being changed and if new email already exists
                if (!string.IsNullOrEmpty(updatePatientDto.Email) && updatePatientDto.Email != patient.Email)
                {
                    var existingPatient = await _context.Patients
                        .FirstOrDefaultAsync(p => p.Email == updatePatientDto.Email && p.Id != id);

                    if (existingPatient != null)
                    {
                        return Conflict($"A patient with email {updatePatientDto.Email} already exists");
                    }
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(updatePatientDto.FirstName))
                    patient.FirstName = updatePatientDto.FirstName;
                if (!string.IsNullOrEmpty(updatePatientDto.LastName))
                    patient.LastName = updatePatientDto.LastName;
                if (!string.IsNullOrEmpty(updatePatientDto.Email))
                    patient.Email = updatePatientDto.Email;
                if (!string.IsNullOrEmpty(updatePatientDto.PhoneNumber))
                    patient.PhoneNumber = updatePatientDto.PhoneNumber;
                if (updatePatientDto.DateOfBirth.HasValue)
                    patient.DateOfBirth = updatePatientDto.DateOfBirth.Value;
                if (updatePatientDto.Address != null)
                    patient.Address = updatePatientDto.Address;
                if (updatePatientDto.MedicalHistory != null)
                    patient.MedicalHistory = updatePatientDto.MedicalHistory;

                patient.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Patient {id} updated successfully");

                var response = new PatientResponseDto
                {
                    Id = patient.Id,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Email = patient.Email,
                    PhoneNumber = patient.PhoneNumber,
                    DateOfBirth = patient.DateOfBirth,
                    Address = patient.Address,
                    MedicalHistory = patient.MedicalHistory,
                    CreatedAt = patient.CreatedAt,
                    UpdatedAt = patient.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating patient {id}");
                return StatusCode(500, "An error occurred while updating the patient");
            }
        }

        /// <summary>
        /// Delete a patient
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePatient(int id)
        {
            try
            {
                var patient = await _context.Patients.FindAsync(id);
                if (patient == null)
                {
                    return NotFound($"Patient with ID {id} not found");
                }

                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Patient {id} deleted successfully");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting patient {id}");
                return StatusCode(500, "An error occurred while deleting the patient");
            }
        }
    }
}
