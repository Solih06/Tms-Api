using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("1.0")]
public class CoursesController(TmsDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses(CancellationToken ct)
    {
        var courses = await context.Courses
            .AsNoTracking()
            .Select(c => new { c.Id, c.Title, c.Code })
            .ToListAsync(ct);
        return Ok(courses);
    }
}