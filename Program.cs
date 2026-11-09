using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;
using System.Threading.RateLimiting;

using TmsApi.Api.Hubs;
using TmsApi.Application.Transcripts;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.Infrastructure.Workers;
using TmsApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// --------------------------------------------------------------------
// Core Framework Services
// --------------------------------------------------------------------

builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger is intentionally omitted because the Swashbuckle package is not referenced in this project.

// --------------------------------------------------------------------
// Authentication / Authorization
// --------------------------------------------------------------------

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// --------------------------------------------------------------------
// CORS
// --------------------------------------------------------------------

var allowedOrigins =
    builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("TmsClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// --------------------------------------------------------------------
// Database
// --------------------------------------------------------------------

var connectionString =
    builder.Configuration.GetConnectionString("TmsDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'TmsDatabase' not found.");

builder.Services.AddDbContext<TmsDbContext>(options =>
{
    options.UseNpgsql(connectionString);

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.LogTo(Console.WriteLine, LogLevel.Information);
    }
});

// --------------------------------------------------------------------
// Application Services
// --------------------------------------------------------------------

builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

builder.Services.AddMemoryCache();

builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();

// --------------------------------------------------------------------
// Strongly Typed Options
// --------------------------------------------------------------------

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .Validate(
        opt => opt.GatewayUrl != null &&
               opt.GatewayUrl.StartsWith("https://"),
        "GatewayUrl must start with https")
    .Validate(
        opt => opt.MaxDepositBirr % 100 == 0,
        "MaxDepositBirr must be divisible by 100")
    .ValidateOnStart();

// --------------------------------------------------------------------
// Rate Limiting
// --------------------------------------------------------------------

builder.Services.AddRateLimiter(options =>
{
    options.AddTokenBucketLimiter("search", limiterOptions =>
    {
        limiterOptions.TokenLimit = 10;
        limiterOptions.TokensPerPeriod = 5;
        limiterOptions.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        limiterOptions.QueueProcessingOrder =
            QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });

    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "10";

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Title = "Rate limit exceeded",
                Detail = "Too many requests. Please retry after 10 seconds.",
                Status = StatusCodes.Status429TooManyRequests
            }, token);
    };
});

// --------------------------------------------------------------------
// Background Services & Channels
// --------------------------------------------------------------------

builder.Services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
    new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait
    }));

builder.Services.AddHostedService<TranscriptWorker>();
builder.Services.AddSignalR();

// --------------------------------------------------------------------
// Middleware Pipeline & App Building
// --------------------------------------------------------------------

var app = builder.Build();

// Seed test data at startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
            new Student { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true },
            new Student { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
            new Student { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new Student { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", GPA = 3.9m, IsActive = true },
            new Student { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", GPA = 2.5m, IsActive = true }
        };
        context.Students.AddRange(students);

        var courses = new List<Course>
        {
            new Course { Code = "CS-101", Title = "Introduction to Computer Science", MaxCapacity = 30 },
            new Course { Code = "CS-201", Title = "Data Structures and Algorithms", MaxCapacity = 25 },
            new Course { Code = "MAT-101", Title = "Calculus I", MaxCapacity = 40 }
        };
        context.Courses.AddRange(courses);
        
        await context.SaveChangesAsync();

        var enrollments = new List<Enrollment>
        {
            new Enrollment { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
            new Enrollment { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
            new Enrollment { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
            new Enrollment { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
        };
        context.Enrollments.AddRange(enrollments);
        await context.SaveChangesAsync();
    }
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("TmsClient");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<TmsHub>("/Hubs/tms");

app.MapGet("/api/assessments/results", () => 
{
    return Results.Ok(new { message = "This is the confidential data." });
})
.RequireAuthorization();

app.MapControllers();

app.Run();