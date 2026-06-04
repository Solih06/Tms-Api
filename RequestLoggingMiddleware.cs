using System.Diagnostics;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        _logger.LogInformation("Request Entry: {Method} {Path} | ID: {CorrelationId}", 
            context.Request.Method, context.Request.Path, correlationId);

        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("Request Exit: {StatusCode} | {Elapsed}ms | ID: {CorrelationId}", 
                context.Response.StatusCode, sw.ElapsedMilliseconds, correlationId);
        }
    }
}