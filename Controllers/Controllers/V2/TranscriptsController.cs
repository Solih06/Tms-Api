using System.Threading.Channels;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Transcripts;

namespace TmsApi.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/transcripts")]
[ApiVersion("2.0")]
public class TranscriptsController(Channel<TranscriptRequest> channel) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RequestTranscript(
        TranscriptRequest request, CancellationToken ct)
    {
        var reportId = Guid.NewGuid().ToString("N")[..8];
        var queuedRequest = request.WithReportId(reportId);
        await channel.Writer.WriteAsync(queuedRequest, ct);

        return Accepted(
            Url.Action(nameof(GetStatus), new { version = "2.0", id = reportId }),
            new TranscriptStatus(reportId, "Queued"));
    }

    [HttpGet("{id}/status")]
    public IActionResult GetStatus(string id)
    {
        return Ok(new TranscriptStatus(id, "Processing"));
    }
}