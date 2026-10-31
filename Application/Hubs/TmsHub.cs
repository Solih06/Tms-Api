using Microsoft.AspNetCore.SignalR;
using TmsApi.Application.Hubs;

namespace TmsApi.Api.Hubs;

public class TmsHub : Hub<ITmsHubClient>
{
    public async Task JoinCourseGroup(string courseCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"course-{courseCode}");
    }

    public async Task LeaveCourseGroup(string courseCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"course-{courseCode}");
    }
}