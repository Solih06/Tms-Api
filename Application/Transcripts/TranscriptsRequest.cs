namespace TmsApi.Application.Transcripts;

public record TranscriptRequest(int StudentId, string? ReportId = null)
{
    public TranscriptRequest WithReportId(string id) => this with { ReportId = id };
}

public record TranscriptStatus(string ReportId, string Status, string? DownloadUrl = null);