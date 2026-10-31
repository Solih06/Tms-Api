namespace TmsApi.Dtos;

public record CourseDetailDto(
    int Id,
    string Title,
    string Code,
    int MaxCapacity,
    int EnrollmentCount,
    List<LinkDto> Links
);