using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

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

    // 5. Paged list of students (Page size 20, stable sort required)
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

    // 6. Top 5 courses by enrollment count using GroupBy and aggregates
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

    // Part A: Intentional N+1 Query (For learning/logging inspection)
    [HttpGet("debug/n-plus-one")]
    public async Task<IActionResult> TestNPlusOne(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n>>> STARTING N+1 QUERY TEST...");
        
        var students = await context.Students
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var results = new List<string>();

        foreach (var s in students)
        {
            var count = await context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.StudentId == s.Id, cancellationToken);
                
            results.Add($"{s.Name}: {count} enrollments");
        }

        Console.WriteLine(">>> END N+1 QUERY TEST.\n");
        return Ok(results);
    }

    // Part B: Fixed with Shaped Query (Single round-trip with a SQL subquery)
    [HttpGet("reports/shaped-enrollments")]
    public async Task<IActionResult> GetShapedEnrollmentReport(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n>>> STARTING SHAPED QUERY TEST...");

        var report = await context.Students
            .AsNoTracking()
            .Select(s => new
            {
                s.Name,
                EnrollmentCount = s.Enrollments.Count
            })
            .ToListAsync(cancellationToken);

        Console.WriteLine(">>> END SHAPED QUERY TEST.\n");
        return Ok(report);
    }

    // 7. Update Student with Shadow Property & Concurrency Check
    [HttpPut("students/{id}")]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto dto, CancellationToken cancellationToken)
    {
        var student = await context.Students.FindAsync([id], cancellationToken);
        if (student == null)
        {
            return NotFound();
        }

        student.Name = dto.Name;
        student.GPA = dto.GPA; // Fixed casing and added missing semicolon

        // UPDATE THE SHADOW PROPERTY HERE (Before saving changes)
        context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Concurrency conflict: This student record was modified by another user." });
        }

        return NoContent();
    }

    // DTO record for incoming update data
    public record UpdateStudentDto(string Name, decimal GPA);

    // 8. Bulk archive enrollments in a single set-based database operation
    [HttpPost("enrollments/bulk-archive")]
    public async Task<IActionResult> BulkArchiveEnrollments(CancellationToken cancellationToken)
    {
        int affectedRows = await context.Enrollments
            .Where(e => !e.IsArchived)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsArchived, true), cancellationToken);

        return Ok(new
        {
            Message = "Bulk archive completed successfully.",
            ArchivedEnrollmentsCount = affectedRows
        });
    }

    // 9. Admin endpoint to view ALL students, including soft-deleted/archived ones
    [HttpGet("admin/students/all")]
    public async Task<IActionResult> GetAllStudentsIncludingArchived(CancellationToken cancellationToken)
    {
        var allStudents = await context.Students
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return Ok(allStudents);
    }
}