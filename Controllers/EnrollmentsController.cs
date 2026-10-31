using Microsoft.AspNetCore.Mvc;
using TmsApi.Services;
using TmsApi.Dtos;  

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses/{courseId}/enrollments")]
[Tags("Enrollments")]
public class EnrollmentsController(
    ICourseService courseService,
    IEnrollmentService enrollmentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> EnrollStudent(int courseId, [FromBody] EnrollStudentRequest request, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(courseId, ct);
        if (course is null) return NotFound();

        if (course.EnrollmentCount >= course.MaxCapacity)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course is full",
                Detail = $"Course '{course.Title}' has reached its maximum capacity of {course.MaxCapacity}.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var enrollment = await enrollmentService.CreateAsync(courseId, request, ct);
        return CreatedAtAction(nameof(GetEnrollment), new { courseId, id = enrollment.Id }, enrollment);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEnrollment(int courseId, int id, CancellationToken ct)
    {
        var enrollment = await enrollmentService.GetByIdAsync(id, ct);
        if (enrollment is null) return NotFound();

        return Ok(enrollment);
    }
}