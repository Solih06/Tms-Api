namespace TmsApi.Dtos;

public record CourseResponseDto(
    int Id,
    string Title,
    string Code,
    int MaxCapacity,
    int EnrollmentCount
);