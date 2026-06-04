using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

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

app.Run();