namespace TmsApi.Models;

public record CourseResponseDto(int Id, string Title, string Code, int MaxCapacity, int EnrollmentCount);

public record CreateCourseRequest
{
    public required string Title { get; init; }
    public required string Code { get; init; }
    public int MaxCapacity { get; init; }
}