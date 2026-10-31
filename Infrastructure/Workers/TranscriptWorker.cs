using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using TmsApi.Api.Hubs;
using TmsApi.Application.Hubs;
using TmsApi.Application.Transcripts;
using TmsApi.Data;

namespace TmsApi.Infrastructure.Workers;

public class TranscriptWorker(
    Channel<TranscriptRequest> channel,
    IServiceScopeFactory scopeFactory,
    IHubContext<TmsHub, ITmsHubClient> hubContext,
    ILogger<TranscriptWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Transcript worker started.");
        await foreach (var request in channel.Reader.ReadAllAsync(ct))
        {
            try
            {
                logger.LogInformation("Generating transcript for student {StudentId}, report {ReportId}",
                    request.StudentId, request.ReportId);

                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();

                // Simulate 5-second generation delay
                await Task.Delay(TimeSpan.FromSeconds(5), ct);

                var downloadUrl = $"/api/v2/transcripts/{request.ReportId}/download";
                
                // Push real-time notification via SignalR
                await hubContext.Clients.All.ReceiveTranscriptReady(request.ReportId!, downloadUrl);

                logger.LogInformation("Transcript ready and notification sent: {ReportId}", request.ReportId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate transcript {ReportId}", request.ReportId);
            }
        }
    }
}