using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
// Enable scope validation and build-time validation for the service provider
builder.Host.UseDefaultServiceProvider(options =>
{
options.ValidateScopes = true;
options.ValidateOnBuild = true;
});

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .Validate(opt =>
    {
        // Example custom rule: GatewayUrl must start with https
        return opt.GatewayUrl.StartsWith("https://");
    }, "GatewayUrl must start with https")
    .Validate(opt =>
    {
        // Example custom rule: MaxDepositBirr must be divisible by 100
        return opt.MaxDepositBirr % 100 == 0;
    }, "MaxDepositBirr must be divisible by 100")
    .ValidateOnStart();


var app = builder.Build();


// 1. Logging Middleware (Outer wrapper)
app.UseMiddleware<RequestLoggingMiddleware>();

// 2. Error handling, HTTPS, Routing
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();

// 3. Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// 4. Map GET /api/assessments/results with authorization required
app.MapGet("/api/assessments/results", () => 
{
    return Results.Ok(new { message = "This is the confidential data." });
})
.RequireAuthorization();
// 5. Map POST /api/enrollments for enrollment creation
app.MapPost("/api/enrollments", async (EnrollmentRequest request, IEnrollmentService service) =>
{
    var record = await service.EnrollAsync(request.StudentId, request.CourseCode);

    // If this enrollment already existed, return 200 OK instead of 201 Created
    if (service.GetAllAsync().Result.Any(e => e.StudentId == request.StudentId && e.CourseCode == request.CourseCode && e.Id == record.Id))
    {
        return Results.Ok(new { message = "Duplicate enrollment attempt", record });
    }

    return Results.Created($"/api/enrollments/{record.Id}", record);
});


app.Run();

// DTO for request body
public record EnrollmentRequest(string StudentId, string CourseCode);