using Polly;
using Polly.Registry;

namespace TmsApi.Infrastructure.ExternalServices;

public record CertificateResult(string CertificateId, string Status);

public interface ICertificateService
{
    Task<CertificateResult> IssueCertificateAsync(int studentId, string courseCode, CancellationToken ct);
}

public class CertificateService(
    ResiliencePipelineProvider<string> pipelineProvider,
    HttpClient httpClient,
    ILogger<CertificateService> logger)
    : ICertificateService
{
    public async Task<CertificateResult> IssueCertificateAsync(
        int studentId, string courseCode, CancellationToken ct)
    {
        var pipeline = pipelineProvider.GetPipeline("certificate-api");
        return await pipeline.ExecuteAsync(async token =>
        {
            logger.LogInformation("Requesting certificate for student {StudentId}, course {CourseCode}", studentId, courseCode);
            var response = await httpClient.PostAsJsonAsync(
                "/api/certificates",
                new { StudentId = studentId, CourseCode = courseCode },
                token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CertificateResult>(token)
                   ?? throw new InvalidOperationException("Empty certificate response.");
        }, ct);
    }
}