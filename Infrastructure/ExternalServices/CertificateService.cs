using System.Net.Http.Json;

namespace TmsApi.Infrastructure.ExternalServices;

public interface ICertificateService
{
    Task<CertificateResult> VerifyCertificateAsync(string certificateNumber, CancellationToken cancellationToken = default);
}

public record CertificateResult(bool IsValid, string Message);

public class CertificateService(
    HttpClient httpClient) : ICertificateService
{
    public async Task<CertificateResult> VerifyCertificateAsync(
        string certificateNumber, 
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/certificates/{certificateNumber}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new CertificateResult(false, "Certificate validation failed from external service.");
        }

        var result = await response.Content.ReadFromJsonAsync<CertificateResult>(
            cancellationToken: cancellationToken);
        return result ?? new CertificateResult(false, "Received empty response body.");
    }
}