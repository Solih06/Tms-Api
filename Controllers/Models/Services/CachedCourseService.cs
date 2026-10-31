using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using TmsApi.Data;
using TmsApi.Dtos;

namespace TmsApi.Services;

public interface ICachedCourseService
{
    Task<CourseDto?> GetCourseAsync(string code, CancellationToken ct);
    Task<List<CourseDto>> GetAllCoursesAsync(CancellationToken ct);
    Task InvalidateCourseCacheAsync(CancellationToken ct);
}

public class CachedCourseService(
    HybridCache cache,
    TmsDbContext context) : ICachedCourseService
{
    public async Task<CourseDto?> GetCourseAsync(string code, CancellationToken ct)
    {
        return await cache.GetOrCreateAsync(
            $"course:{code}",
            code,
            async (courseCode, token) =>
            {
                var course = await context.Courses
                    .AsNoTracking()
                    .Include(c => c.Enrollments)
                    .FirstOrDefaultAsync(c => c.Code == courseCode, token);

                if (course is null) return null;

                return new CourseDto(
                    course.Id,
                    course.Title,
                    course.Code,
                    course.MaxCapacity,
                    course.Enrollments.Count);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(2),
            },
            tags: ["courses"],
            cancellationToken: ct
        );
    }

    public async Task<List<CourseDto>> GetAllCoursesAsync(CancellationToken ct)
    {
        return await cache.GetOrCreateAsync(
            "courses:all",
            async (token) =>
            {
                var courses = await context.Courses
                    .AsNoTracking()
                    .Include(c => c.Enrollments)
                    .ToListAsync(token);

                return courses.Select(c => new CourseDto(
                    c.Id,
                    c.Title,
                    c.Code,
                    c.MaxCapacity,
                    c.Enrollments.Count)).ToList();
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(2),
            },
            tags: ["courses"],
            cancellationToken: ct
        );
    }

    public async Task InvalidateCourseCacheAsync(CancellationToken ct)
    {
        await cache.RemoveByTagAsync("courses", ct);
    }
}