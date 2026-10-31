using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

public interface ICourseService
{
    Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<CourseResponseDto?> FindByCodeAsync(string code, CancellationToken ct);
    Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct);
    
    // Add this line for Exercise 4:
    Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct);
}

public class CourseService(TmsDbContext context) : ICourseService
{
    public async Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await context.Courses
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Title,
                c.Code,
                c.MaxCapacity,
                c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CourseResponseDto?> FindByCodeAsync(string code, CancellationToken ct)
    {
        return await context.Courses
            .Where(c => c.Code == code)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Title,
                c.Code,
                c.MaxCapacity,
                c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Title = request.Title,
            Code = request.Code,
            MaxCapacity = request.MaxCapacity
        };

        context.Courses.Add(course);
        await context.SaveChangesAsync(ct);

        return new CourseResponseDto(
            course.Id,
            course.Title,
            course.Code,
            course.MaxCapacity,
            0);
    }

    // Add this implementation method for Exercise 4:
    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
    {
        var query = context.Courses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(c => c.Title.Contains(request.Search));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Title)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Title,
                c.Code,
                c.MaxCapacity,
                c.Enrollments.Count))
            .ToListAsync(ct);

        return new PagedResponse<CourseResponseDto>(items, totalCount, request.Page, request.PageSize);
    }
}