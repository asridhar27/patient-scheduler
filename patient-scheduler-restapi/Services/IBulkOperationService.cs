using PatientScheduler.Models;

namespace PatientScheduler.Services
{
    public interface IBulkOperationService
    {
        Task<BulkOperationResult> QueueBulkOperationAsync(BulkOperationRequest request);
        Task<JobStatus> GetJobStatusAsync(int jobId);
        Task<List<JobStatus>> GetAllJobsAsync();
        Task<bool> CancelJobAsync(int jobId);
        Task ProcessBulkOperationsAsync();
    }
}
