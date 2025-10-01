namespace PatientScheduler.Controllers
{
    [ApiController]
    [Route("api/timeslots")]
    public class TimeSlotsController : ControllerBase
    {
        private readonly PatientSchedulerContext _context;
        private readonly ILogger<TimeSlotsController> _logger;
        private static readonly TimeSpan SlotLength = TimeSpan.FromHours(1);
        private static readonly TimeSpan OfficeOpenTime = TimeSpan.FromHours(9);
        private static readonly TimeSpan OfficeCloseTime = TimeSpan.FromHours(17);

        public TimeSlotsController(PatientSchedulerContext context, ILogger<TimeSlotsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get available time slots for a specific doctor on a specific date (computed dynamically)
        /// Office hours: 9:00 to 15:00, 1-hour blocks
        /// </summary>
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<TimeSlotResponseDto>>> GetAvailableTimeSlots(
            [FromQuery] int doctorId,
            [FromQuery] DateTime date)
        {
            try
            {
                _logger.LogInformation("[TimeSlots] Request received. doctorId={DoctorId}, date={Date:o}", doctorId, date);

                if (doctorId <= 0)
                {
                    _logger.LogWarning("[TimeSlots] Invalid doctorId supplied: {DoctorId}", doctorId);
                    return BadRequest("Doctor ID must be greater than 0");
                }

                var doctor = await _context.Doctors.FindAsync(doctorId);
                if (doctor == null)
                {
                    _logger.LogWarning("[TimeSlots] Doctor not found. doctorId={DoctorId}", doctorId);
                    return NotFound($"Doctor with ID {doctorId} not found");
                }

                var day = date.Date;
                _logger.LogInformation("[TimeSlots] Calculating availability for doctorId={DoctorId} on {Day:yyyy-MM-dd}", doctorId, day);

                var (startOfDay, endOfDay) = GetOfficeHours(day);
                _logger.LogDebug("[TimeSlots] Office hours resolved to start={Start:o}, end={End:o}", startOfDay, endOfDay);

                // Pull existing non-cancelled appointments for the doctor on that day
                var appointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId && a.Status != "Cancelled" && a.AppointmentDateTime.Date == day)
                    .Select(a => new { a.AppointmentDateTime, a.Duration })
                    .ToListAsync();

                _logger.LogInformation("[TimeSlots] Found {AppointmentCount} existing appointments", appointments.Count);

                // Build all candidate 1-hour slots
                var candidateSlots = new List<(DateTime Start, DateTime End)>();
                for (var slotStart = startOfDay; slotStart < endOfDay; slotStart = slotStart.Add(SlotLength))
                {
                    var slotEnd = slotStart.Add(SlotLength);
                    candidateSlots.Add((slotStart, slotEnd));
                }

                _logger.LogDebug("[TimeSlots] Generated {CandidateCount} candidate slots", candidateSlots.Count);

                // Filter out any slot that overlaps an existing appointment
                bool Overlaps((DateTime Start, DateTime End) slot, DateTime apptStart, TimeSpan duration)
                {
                    var apptEnd = apptStart.Add(duration);
                    return apptStart < slot.End && apptEnd > slot.Start;
                }

                var available = candidateSlots
                    .Where(slot => !appointments.Any(a => Overlaps(slot, a.AppointmentDateTime, a.Duration)))
                    .Select((slot, idx) => new TimeSlotResponseDto
                    {
                        Id = idx + 1,
                        DoctorId = doctorId,
                        StartTime = slot.Start,
                        EndTime = slot.End,
                        Status = "Active",
                        AppointmentId = null,
                        DoctorName = $"{doctor.FirstName} {doctor.LastName}",
                        DoctorSpecialization = doctor.Specialization
                    })
                    .ToList();

                _logger.LogInformation("[TimeSlots] Returning {AvailableCount} available slots", available.Count);

                return Ok(available);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving available time slots for doctor {doctorId} on {date:yyyy-MM-dd}");
                return StatusCode(500, "An error occurred while retrieving available time slots");
            }
        }

        /// <summary>
        /// Admin view: compute all slots (booked and available) for a doctor on a date
        /// </summary>
        [HttpGet("doctor/{doctorId}/day")]
        public async Task<ActionResult<IEnumerable<TimeSlotResponseDto>>> GetDoctorDaySlots(int doctorId, [FromQuery] DateTime date)
        {
            try
            {
                var doctor = await _context.Doctors.FindAsync(doctorId);
                if (doctor == null)
                {
                    return NotFound($"Doctor with ID {doctorId} not found");
                }

                var day = date.Date;
                var (startOfDay, endOfDay) = GetOfficeHours(day);

                var appointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId && a.Status != "Cancelled" && a.AppointmentDateTime.Date == day)
                    .ToListAsync();

                var slots = new List<TimeSlotResponseDto>();
                for (var slotStart = startOfDay; slotStart < endOfDay; slotStart = slotStart.Add(SlotLength))
                {
                    var slotEnd = slotStart.Add(SlotLength);
                    var appt = appointments.FirstOrDefault(a => a.AppointmentDateTime < slotEnd && a.AppointmentDateTime.Add(a.Duration) > slotStart);
                    var status = appt == null ? "Active" : "Booked";

                    slots.Add(new TimeSlotResponseDto
                    {
                        Id = slots.Count + 1,
                        DoctorId = doctorId,
                        StartTime = slotStart,
                        EndTime = slotEnd,
                        Status = status,
                        AppointmentId = appt?.Id,
                        DoctorName = $"{doctor.FirstName} {doctor.LastName}",
                        DoctorSpecialization = doctor.Specialization
                    });
                }

                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving day slots for doctor {doctorId}");
                return StatusCode(500, "An error occurred while retrieving time slots");
            }
        }

        private static (DateTime StartOfDay, DateTime EndOfDay) GetOfficeHours(DateTime day)
        {
            // Ensure we're working with local time, not UTC
            var localDay = DateTime.SpecifyKind(day.Date, DateTimeKind.Local);
            var start = localDay.Add(OfficeOpenTime);
            var end = localDay.Add(OfficeCloseTime);
            return (start, end);
        }
    }
}
