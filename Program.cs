using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

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
builder.Services.AddControllers();

// Register TmsDbContext with console SQL logging and sensitive data enabled for development
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
           .LogTo(Console.WriteLine, LogLevel.Information)
           .EnableSensitiveDataLogging());

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


// Seed test data at startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    context.Database.Migrate(); // Applies any pending migrations; keeps migration history intact

    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
            new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", GPA = 3.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", GPA = 2.5m, IsActive = true }
        };
        context.Students.AddRange(students);

        var courses = new List<Course>
        {
            new() { Code = "CS-101", Title = "Introduction to Computer Science", Capacity = 30 },
            new() { Code = "CS-201", Title = "Data Structures and Algorithms", Capacity = 25 },
            new() { Code = "MAT-101", Title = "Calculus I", Capacity = 40 }
        };
        context.Courses.AddRange(courses);
        
        context.SaveChanges();

        var enrollments = new List<Enrollment>
        {
            new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
            new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
            new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
            new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
        };
        context.Enrollments.AddRange(enrollments);
        context.SaveChanges();
    }
}


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
 app.MapControllers();


app.Run();

// DTO for request body
public record EnrollmentRequest(string StudentId, string CourseCode);