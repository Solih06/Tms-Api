using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Application.Utilities;

namespace TmsApi.Controllers.V2;

[ApiController]
[Route("api/v2/courses")]
public class CoursesV2Controller(TmsDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses([FromQuery] string? fields)
    {
        var courses = await context.Courses
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Title,
                c.Code,
                c.MaxCapacity,
                c.Enrollments.Count
            ))
            .ToListAsync(); // Must be ToListAsync() when using await

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
                new LinkDto(Url.Action(nameof(GetCourses), new { fields })!, "self", "GET"),
                new LinkDto("/api/v2/enrollments", "enrollments", "GET")
            }
        });
    }
}