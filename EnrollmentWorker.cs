using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TmsApi;

public class EnrollmentWorker(ILogger<EnrollmentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EnrollmentWorker starting up...");

        // Simulate background verification or processing if needed
        await Task.CompletedTask;
    }
}