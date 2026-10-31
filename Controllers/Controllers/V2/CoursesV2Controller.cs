using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Application.Utilities;

namespace TmsApi.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
public class CoursesController(TmsDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] string? fields, 
        CancellationToken ct)
    {
        var courses = await context.Courses
            .AsNoTracking()
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Title,
                c.Code,
                c.MaxCapacity,
                c.Enrollments.Count
            ))
            .ToListAsync(ct);

        // Apply data shaping based on query parameters (e.g., ?fields=id,title)
        var shapedData = courses.ShapeData(fields);

        return Ok(new
        {
            Data = shapedData,
            Meta = new
            {
                TotalCount = courses.Count
            },
            Links = new[]
            {
                new LinkDto(Url.Action(nameof(GetCourses), new { version = "2.0", fields })!, "self", "GET"),
                new LinkDto("/api/v2/enrollments", "enrollments", "GET")
            }
        });
    }
}