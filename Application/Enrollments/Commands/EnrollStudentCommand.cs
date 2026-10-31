using MediatR;

namespace TmsApi.Application.Enrollments.Commands;

public record EnrollStudentCommand(int StudentId, string CourseCode) : IRequest<EnrollmentResult>;

public record EnrollmentResult(bool IsSuccess, int? EnrollmentId = null, string? ErrorMessage = null)
{
    public static EnrollmentResult Success(int id) => new(true, id);
    public static EnrollmentResult Failure(string message) => new(false, ErrorMessage: message);
}