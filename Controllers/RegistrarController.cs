using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/registrar")]
public class RegistrarController(TmsDbContext context) : ControllerBase
{
    // 1. How many active students have GPA >= 3.0?
    [HttpGet("active-count")]
    public async Task<IActionResult> GetActiveStudentsCount()
    {
        var count = await context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();

        return Ok(new { ActiveStudentsWithGoodGPA = count });
    }

    // 2. Which courses have the most enrollments, sorted descending?
    [HttpGet("courses-by-enrollment")]
    public async Task<IActionResult> GetCoursesByEnrollment()
    {
        var list = await context.Courses
            .Select(c => new
            {
                c.Title,
                EnrollmentCount = c.Enrollments.Count
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();

        return Ok(list);
    }

    // 3. What is the average GPA per course?
    [HttpGet("average-gpa-per-course")]
    public async Task<IActionResult> GetAverageGpaPerCourse()
    {
        var list = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new
            {
                Course = g.Key,
                AverageGPA = g.Average(e => e.Student.GPA)
            })
            .ToListAsync();

        return Ok(list);
    }

    // 4. Which students have zero enrollments? (Approach A: Subquery / NOT EXISTS)
    [HttpGet("zero-enrollments-subquery")]
    public async Task<IActionResult> GetZeroEnrollmentsSubquery()
    {
        var list = await context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();

        return Ok(list);
    }

    // 4. Which students have zero enrollments? (Approach B: Left Join)
    [HttpGet("zero-enrollments-leftjoin")]
    public async Task<IActionResult> GetZeroEnrollmentsLeftJoin()
    {
        var list = await context.Students
            .LeftJoin(context.Enrollments,
                s => s.Id,
                e => e.StudentId,
                (s, e) => new { s, e })
            .Where(x => x.e == null)
            .Select(x => x.s.Name)
            .ToListAsync();

        return Ok(list);
    }

// 1. Paged list of students (Page size 20, stable sort required)
    [HttpGet("students/paged")]
    public async Task<IActionResult> GetPagedStudents(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var students = await context.Students
            .OrderBy(s => s.Name) // CRITICAL: Always OrderBy before Skip/Take for stable sorting
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            CurrentPage = page,
            PageSize = pageSize,
            Students = students
        });
    }

    // 2. Top 5 courses by enrollment count using GroupBy and aggregates
    [HttpGet("courses/top-enrollments")]
    public async Task<IActionResult> GetTopCoursesByEnrollment(CancellationToken cancellationToken = default)
    {
        var topCourses = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new
            {
                CourseTitle = g.Key,
                EnrollmentCount = g.Count()
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        return Ok(topCourses);
    }
}