using Microsoft.EntityFrameworkCore;
using PatientScheduler.Data;

namespace PatientScheduler.Services
{
    public class BackgroundJobService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromSeconds(30); // Process jobs every 30 seconds

        public BackgroundJobService(IServiceProvider serviceProvider, ILogger<BackgroundJobService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Job Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var bulkOperationService = scope.ServiceProvider.GetRequiredService<IBulkOperationService>();
                    
                    await bulkOperationService.ProcessBulkOperationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing background jobs");
                }

                await Task.Delay(_period, stoppingToken);
            }

            _logger.LogInformation("Background Job Service stopped");
        }
    }
}
