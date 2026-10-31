namespace TmsApi.Dtos;

public record CourseDto(
    int Id,
    string Title,
    string Code,
    int MaxCapacity,
    int EnrollmentCount
);