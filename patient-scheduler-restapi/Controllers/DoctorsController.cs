namespace PatientScheduler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorsController : ControllerBase
    {
        private readonly PatientSchedulerContext _context;
        private readonly ILogger<DoctorsController> _logger;

        public DoctorsController(PatientSchedulerContext context, ILogger<DoctorsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all doctors
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Doctor>>> GetDoctors()
        {
            try
            {
                var doctors = await _context.Doctors.ToListAsync();
                return Ok(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctors");
                return StatusCode(500, "An error occurred while retrieving doctors");
            }
        }

        /// <summary>
        /// Get a specific doctor by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Doctor>> GetDoctor(int id)
        {
            try
            {
                var doctor = await _context.Doctors.FindAsync(id);
                if (doctor == null)
                {
                    return NotFound($"Doctor with ID {id} not found");
                }

                return Ok(doctor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving doctor {id}");
                return StatusCode(500, "An error occurred while retrieving the doctor");
            }
        }
    }
}
