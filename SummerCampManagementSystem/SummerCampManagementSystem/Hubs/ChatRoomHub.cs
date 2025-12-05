using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SummerCampManagementSystem.API.Hubs
{
    [Authorize] 
    public class ChatRoomHub : Hub
    {
        // use this to join room
        public async Task JoinRoom(string roomId)
        {
            // get current connectionId for roomId
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }

        // use to leave or change room
        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        }
    }
}