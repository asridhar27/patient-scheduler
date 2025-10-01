using Microsoft.EntityFrameworkCore;
using PatientScheduler.Data;
using PatientScheduler.Models;
using System.Text.Json;

namespace PatientScheduler.Services
{
    public class BulkOperationService : IBulkOperationService
    {
        private readonly PatientSchedulerContext _context;
        private readonly ILogger<BulkOperationService> _logger;
        private readonly ITransactionService _transactionService;

        public BulkOperationService(
            PatientSchedulerContext context, 
            ILogger<BulkOperationService> logger,
            ITransactionService transactionService)
        {
            _context = context;
            _logger = logger;
            _transactionService = transactionService;
        }

        public async Task<BulkOperationResult> QueueBulkOperationAsync(BulkOperationRequest request)
        {
            try
            {
                var job = new Job
                {
                    OperationType = request.OperationType,
                    Status = "Queued",
                    Parameters = JsonSerializer.Serialize(request),
                    TotalRecords = request.AppointmentIds.Count,
                    ProcessedRecords = 0,
                    FailedRecords = 0,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System" // In real app, get from current user context
                };

                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Queued bulk operation {request.OperationType} with {request.AppointmentIds.Count} records. Job ID: {job.Id}");

                return new BulkOperationResult
                {
                    JobId = job.Id,
                    Status = job.Status,
                    TotalRecords = job.TotalRecords,
                    ProcessedRecords = 0,
                    FailedRecords = 0,
                    CreatedAt = job.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue bulk operation");
                throw;
            }
        }

        public async Task<JobStatus> GetJobStatusAsync(int jobId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null)
                throw new ArgumentException($"Job with ID {jobId} not found");

            return new JobStatus
            {
                Id = job.Id,
                OperationType = job.OperationType,
                Status = job.Status,
                TotalRecords = job.TotalRecords,
                ProcessedRecords = job.ProcessedRecords,
                FailedRecords = job.FailedRecords,
                Errors = string.IsNullOrEmpty(job.Errors) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(job.Errors) ?? new List<string>(),
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                Result = job.Result
            };
        }

        public async Task<List<JobStatus>> GetAllJobsAsync()
        {
            var jobs = await _context.Jobs
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return jobs.Select(job => new JobStatus
            {
                Id = job.Id,
                OperationType = job.OperationType,
                Status = job.Status,
                TotalRecords = job.TotalRecords,
                ProcessedRecords = job.ProcessedRecords,
                FailedRecords = job.FailedRecords,
                Errors = string.IsNullOrEmpty(job.Errors) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(job.Errors) ?? new List<string>(),
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                Result = job.Result
            }).ToList();
        }

        public async Task<bool> CancelJobAsync(int jobId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null || job.Status == "Completed" || job.Status == "Failed")
                return false;

            job.Status = "Cancelled";
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Cancelled job {jobId}");
            return true;
        }

        public async Task ProcessBulkOperationsAsync()
        {
            var queuedJobs = await _context.Jobs
                .Where(j => j.Status == "Queued")
                .OrderBy(j => j.CreatedAt)
                .ToListAsync();

            foreach (var job in queuedJobs)
            {
                await ProcessJobAsync(job);
            }
        }

        private async Task ProcessJobAsync(Job job)
        {
            try
            {
                job.Status = "Processing";
                job.StartedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Processing job {job.Id} of type {job.OperationType}");

                var request = JsonSerializer.Deserialize<BulkOperationRequest>(job.Parameters ?? "{}");
                if (request == null)
                {
                    throw new InvalidOperationException("Invalid job parameters");
                }

                var result = await ExecuteBulkOperationAsync(request, job);
                
                job.Status = result.FailedRecords == 0 ? "Completed" : "Failed";
                job.ProcessedRecords = result.ProcessedRecords;
                job.FailedRecords = result.FailedRecords;
                job.Errors = JsonSerializer.Serialize(result.Errors);
                job.Result = JsonSerializer.Serialize(result);
                job.CompletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Completed job {job.Id}. Processed: {result.ProcessedRecords}, Failed: {result.FailedRecords}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process job {job.Id}");
                
                job.Status = "Failed";
                job.Errors = JsonSerializer.Serialize(new List<string> { ex.Message });
                job.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private async Task<BulkOperationResult> ExecuteBulkOperationAsync(BulkOperationRequest request, Job job)
        {
            var result = new BulkOperationResult
            {
                JobId = job.Id,
                Status = "Processing",
                TotalRecords = request.AppointmentIds.Count,
                ProcessedRecords = 0,
                FailedRecords = 0,
                Errors = new List<string>(),
                CreatedAt = job.CreatedAt
            };

            switch (request.OperationType.ToLower())
            {
                case "bulkcancel":
                    await ExecuteBulkCancelAsync(request, result);
                    break;
                case "bulkreschedule":
                    await ExecuteBulkRescheduleAsync(request, result);
                    break;
                case "bulkbillingupdate":
                    await ExecuteBulkBillingUpdateAsync(request, result);
                    break;
                default:
                    throw new ArgumentException($"Unknown operation type: {request.OperationType}");
            }

            result.Status = result.FailedRecords == 0 ? "Completed" : "Failed";
            result.CompletedAt = DateTime.UtcNow;
            return result;
        }

        private async Task ExecuteBulkCancelAsync(BulkOperationRequest request, BulkOperationResult result)
        {
            try
            {
                _logger.LogInformation($"Starting bulk cancel operation for {request.AppointmentIds.Count} appointments");

                // Convert appointment IDs to comma-separated string
                var appointmentIdsString = string.Join(",", request.AppointmentIds);
                var reason = request.Reason ?? "Bulk cancellation";

                // Execute the stored procedure
                var sql = @"
                    EXEC BulkCancelAppointments 
                        @AppointmentIds = {0}, 
                        @Reason = {1}";

                var dbResult = await _context.Database.ExecuteSqlRawAsync(sql, appointmentIdsString, reason);

                _logger.LogInformation($"Bulk cancel operation completed for {request.AppointmentIds.Count} appointments");
                
                result.ProcessedRecords = request.AppointmentIds.Count;
                result.FailedRecords = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk cancel operation failed");
                
                // If the stored procedure fails, fall back to individual cancellations
                _logger.LogInformation("Falling back to individual appointment cancellations");
                
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var appointmentId in request.AppointmentIds)
                    {
                        try
                        {
                            // Use the overload without transaction to avoid nested transactions
                            var success = await _transactionService.CancelAppointmentWithRefundAsync(appointmentId, useTransaction: false);
                            if (success)
                            {
                                result.ProcessedRecords++;
                            }
                            else
                            {
                                result.FailedRecords++;
                                result.Errors.Add($"Failed to cancel appointment {appointmentId}");
                            }
                        }
                        catch (Exception individualEx)
                        {
                            result.FailedRecords++;
                            result.Errors.Add($"Error cancelling appointment {appointmentId}: {individualEx.Message}");
                            _logger.LogError(individualEx, $"Error cancelling appointment {appointmentId}");
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception fallbackEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(fallbackEx, "Fallback bulk cancel operation failed, transaction rolled back");
                    throw;
                }
            }
        }

        private async Task ExecuteBulkRescheduleAsync(BulkOperationRequest request, BulkOperationResult result)
        {
            if (!request.NewDateTime.HasValue)
            {
                throw new ArgumentException("NewDateTime is required for reschedule operation");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get appointments to update
                var appointments = await _context.Appointments
                    .Where(a => request.AppointmentIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var appointment in appointments)
                {
                    try
                    {
                        appointment.AppointmentDateTime = request.NewDateTime.Value;
                        appointment.UpdatedAt = DateTime.UtcNow;
                        appointment.Notes = string.IsNullOrEmpty(appointment.Notes) 
                            ? request.Reason ?? "Bulk reschedule"
                            : appointment.Notes + " | " + (request.Reason ?? "Bulk reschedule");
                        
                        result.ProcessedRecords++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedRecords++;
                        result.Errors.Add($"Error rescheduling appointment {appointment.Id}: {ex.Message}");
                        _logger.LogError(ex, $"Error rescheduling appointment {appointment.Id}");
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Bulk reschedule operation failed, transaction rolled back");
                result.FailedRecords = request.AppointmentIds.Count;
                result.Errors.Add($"Bulk reschedule failed: {ex.Message}");
            }
        }

        private async Task ExecuteBulkBillingUpdateAsync(BulkOperationRequest request, BulkOperationResult result)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get appointments and their invoices
                var appointments = await _context.Appointments
                    .Include(a => a.Invoices)
                    .Where(a => request.AppointmentIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var appointment in appointments)
                {
                    try
                    {
                        if (request.BillingAdjustment.HasValue && request.BillingAdjustment.Value != 0)
                        {
                            var existingInvoice = appointment.Invoices?.FirstOrDefault();
                            
                            if (existingInvoice != null)
                            {
                                // Update existing invoice
                                existingInvoice.Amount = Math.Max(0, existingInvoice.Amount + request.BillingAdjustment.Value);
                                existingInvoice.UpdatedAt = DateTime.UtcNow;
                                existingInvoice.Notes = string.IsNullOrEmpty(existingInvoice.Notes)
                                    ? request.Notes ?? "Bulk billing update"
                                    : existingInvoice.Notes + " | " + (request.Notes ?? "Bulk billing update");
                            }
                            else if (request.BillingAdjustment.Value > 0)
                            {
                                // Create new invoice for positive adjustment
                                var newInvoice = new Invoice
                                {
                                    PatientId = appointment.PatientId,
                                    AppointmentId = appointment.Id,
                                    Amount = request.BillingAdjustment.Value,
                                    Status = "Pending",
                                    DueDate = DateTime.UtcNow.AddDays(30),
                                    CreatedAt = DateTime.UtcNow,
                                    Notes = request.Notes ?? "Bulk billing update"
                                };
                                _context.Invoices.Add(newInvoice);
                            }
                        }
                        
                        result.ProcessedRecords++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedRecords++;
                        result.Errors.Add($"Error updating billing for appointment {appointment.Id}: {ex.Message}");
                        _logger.LogError(ex, $"Error updating billing for appointment {appointment.Id}");
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Bulk billing update operation failed, transaction rolled back");
                result.FailedRecords = request.AppointmentIds.Count;
                result.Errors.Add($"Bulk billing update failed: {ex.Message}");
            }
        }
    }
}
