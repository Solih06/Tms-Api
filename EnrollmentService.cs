using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

public interface IEnrollmentService
{
    Task<EnrollmentResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct);
    Task<IReadOnlyList<EnrollmentResponseDto>> GetAllAsync(CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}

public class EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public async Task<EnrollmentResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var enrollment = await context.Enrollments
            .Where(e => e.Id == id)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt))
            .FirstOrDefaultAsync(ct);

        if (enrollment is null)
        {
            logger.LogWarning("Enrollment {EnrollmentId} not found", id);
        }

        return enrollment;
    }

    public async Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct)
    {
        // Check for duplicate enrollment by student + course
        var existing = await context.Enrollments
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == request.StudentId, ct);

        if (existing is not null)
        {
            logger.LogWarning(
                "Duplicate enrollment attempt. Student {StudentId} already in Course {CourseId} (record {EnrollmentId})",
                request.StudentId, courseId, existing.Id);
        }

        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Enrolled Student {StudentId} in Course {CourseId}. Record {EnrollmentId}",
            request.StudentId, courseId, enrollment.Id);

        return new EnrollmentResponseDto(
            enrollment.Id,
            enrollment.CourseId,
            enrollment.StudentId,
            enrollment.EnrolledAt);
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetAllAsync(CancellationToken ct)
    {
        var all = await context.Enrollments
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt))
            .ToListAsync(ct);

        logger.LogInformation("Retrieved {EnrollmentCount} enrollment records", all.Count);

        return all;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var enrollment = await context.Enrollments.FindAsync([id], ct);
        if (enrollment is null)
        {
            logger.LogWarning("Delete failed: enrollment {EnrollmentId} not found", id);
            return false;
        }

        context.Enrollments.Remove(enrollment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Deleted enrollment {EnrollmentId}", id);
        return true;
    }
}