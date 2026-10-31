using MediatR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Application.Enrollments.Queries;

public class GetStudentScheduleHandler(TmsDbContext context) : IRequestHandler<GetStudentScheduleQuery, ScheduleDto>
{
    public async Task<ScheduleDto> Handle(GetStudentScheduleQuery query, CancellationToken ct)
    {
        var enrollments = await context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.StudentId == query.StudentId)
            .ToListAsync(ct);

        var items = enrollments.Select(e => new ScheduleItemDto(
            e.Course.Code, 
            e.Course.Title, 
            "TBD")).ToList();

        return new ScheduleDto(query.StudentId, items);
    }
}