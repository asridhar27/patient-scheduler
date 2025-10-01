using Microsoft.AspNetCore.Mvc;
using PatientScheduler.Models;
using PatientScheduler.Services;

namespace PatientScheduler.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BulkOperationsController : ControllerBase
    {
        private readonly IBulkOperationService _bulkOperationService;
        private readonly ILogger<BulkOperationsController> _logger;

        public BulkOperationsController(
            IBulkOperationService bulkOperationService,
            ILogger<BulkOperationsController> logger)
        {
            _bulkOperationService = bulkOperationService;
            _logger = logger;
        }

        /// <summary>
        /// Queue a bulk operation for processing
        /// </summary>
        [HttpPost("queue")]
        public async Task<ActionResult<BulkOperationResult>> QueueBulkOperation([FromBody] BulkOperationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (request.AppointmentIds == null || !request.AppointmentIds.Any())
                {
                    return BadRequest("At least one appointment ID is required");
                }

                var result = await _bulkOperationService.QueueBulkOperationAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing bulk operation");
                return StatusCode(500, new { message = "An error occurred while queuing the bulk operation" });
            }
        }

        /// <summary>
        /// Get the status of a specific job
        /// </summary>
        [HttpGet("job/{jobId}")]
        public async Task<ActionResult<JobStatus>> GetJobStatus(int jobId)
        {
            try
            {
                var status = await _bulkOperationService.GetJobStatusAsync(jobId);
                return Ok(status);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting job status for job {jobId}");
                return StatusCode(500, new { message = "An error occurred while getting job status" });
            }
        }

        /// <summary>
        /// Get all jobs with their statuses
        /// </summary>
        [HttpGet("jobs")]
        public async Task<ActionResult<List<JobStatus>>> GetAllJobs()
        {
            try
            {
                var jobs = await _bulkOperationService.GetAllJobsAsync();
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all jobs");
                return StatusCode(500, new { message = "An error occurred while getting jobs" });
            }
        }

        /// <summary>
        /// Cancel a queued or processing job
        /// </summary>
        [HttpPost("job/{jobId}/cancel")]
        public async Task<ActionResult> CancelJob(int jobId)
        {
            try
            {
                var cancelled = await _bulkOperationService.CancelJobAsync(jobId);
                if (cancelled)
                {
                    return Ok(new { message = "Job cancelled successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Job cannot be cancelled (may already be completed or failed)" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling job {jobId}");
                return StatusCode(500, new { message = "An error occurred while cancelling the job" });
            }
        }

        /// <summary>
        /// Process all queued bulk operations (typically called by background service)
        /// </summary>
        [HttpPost("process")]
        public async Task<ActionResult> ProcessBulkOperations()
        {
            try
            {
                await _bulkOperationService.ProcessBulkOperationsAsync();
                return Ok(new { message = "Bulk operations processing completed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk operations");
                return StatusCode(500, new { message = "An error occurred while processing bulk operations" });
            }
        }

        /// <summary>
        /// Get bulk operation statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult> GetBulkOperationStats()
        {
            try
            {
                var jobs = await _bulkOperationService.GetAllJobsAsync();
                
                var stats = new
                {
                    TotalJobs = jobs.Count,
                    QueuedJobs = jobs.Count(j => j.Status == "Queued"),
                    ProcessingJobs = jobs.Count(j => j.Status == "Processing"),
                    CompletedJobs = jobs.Count(j => j.Status == "Completed"),
                    FailedJobs = jobs.Count(j => j.Status == "Failed"),
                    TotalRecordsProcessed = jobs.Sum(j => j.ProcessedRecords),
                    TotalRecordsFailed = jobs.Sum(j => j.FailedRecords),
                    AverageProcessingTime = jobs
                        .Where(j => j.CompletedAt.HasValue && j.StartedAt.HasValue)
                        .Select(j => (j.CompletedAt.Value - j.StartedAt.Value).TotalMinutes)
                        .DefaultIfEmpty(0)
                        .Average()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bulk operation stats");
                return StatusCode(500, new { message = "An error occurred while getting statistics" });
            }
        }
    }
}
