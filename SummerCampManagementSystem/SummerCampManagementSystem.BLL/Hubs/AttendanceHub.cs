using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace SummerCampManagementSystem.BLL.Hubs;

/// <summary>
/// SignalR Hub for real-time attendance updates
/// Clients subscribe to specific activity schedule groups to receive updates
/// </summary>
public class AttendanceHub : Hub
{
    private readonly ILogger<AttendanceHub> _logger;

    public AttendanceHub(ILogger<AttendanceHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribe client to attendance updates for a specific activity schedule
    /// </summary>
    public async Task SubscribeToActivitySchedule(int activityScheduleId)
    {
        var groupName = $"attendance/{activityScheduleId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client {ConnectionId} subscribed to {GroupName}",
            Context.ConnectionId, groupName
        );

        // Send confirmation to client
        await Clients.Caller.SendAsync("SubscriptionConfirmed", new
        {
            groupName,
            activityScheduleId,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Unsubscribe client from activity schedule updates
    /// </summary>
    public async Task UnsubscribeFromActivitySchedule(int activityScheduleId)
    {
        var groupName = $"attendance/{activityScheduleId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from {GroupName}",
            Context.ConnectionId, groupName
        );
    }

    /// <summary>
    /// Client ping to keep connection alive
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
    }

    /// <summary>
    /// Connection established event
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "SignalR client connected: {ConnectionId} (User: {UserId})",
            Context.ConnectionId,
            Context.User?.Identity?.Name ?? "Anonymous"
        );

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Connection closed event
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(
                exception,
                "SignalR client disconnected with error: {ConnectionId}",
                Context.ConnectionId
            );
        }
        else
        {
            _logger.LogInformation(
                "SignalR client disconnected: {ConnectionId}",
                Context.ConnectionId
            );
        }

        await base.OnDisconnectedAsync(exception);
    }
}
