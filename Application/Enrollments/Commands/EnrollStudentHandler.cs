using MediatR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Application.Enrollments.Commands;

public class EnrollStudentHandler(TmsDbContext context) : IRequestHandler<EnrollStudentCommand, EnrollmentResult>
{
    public async Task<EnrollmentResult> Handle(EnrollStudentCommand command, CancellationToken ct)
    {
        var course = await context.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Code == command.CourseCode, ct);

        if (course is null)
            return EnrollmentResult.Failure($"Course '{command.CourseCode}' not found.");

        if (course.Enrollments.Count >= course.MaxCapacity)
            return EnrollmentResult.Failure($"Course '{course.Title}' has reached its capacity of {course.MaxCapacity}.");

        var enrollment = new Enrollment
        {
            StudentId = command.StudentId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        return EnrollmentResult.Success(enrollment.Id);
    }
}