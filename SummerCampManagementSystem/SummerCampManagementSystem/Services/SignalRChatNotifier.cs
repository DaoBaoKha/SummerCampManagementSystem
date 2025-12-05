using Microsoft.AspNetCore.SignalR;
using SummerCampManagementSystem.API.Hubs;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Services
{
    public class SignalRChatNotifier : IChatNotifier
    {
        private readonly IHubContext<ChatRoomHub> _hubContext;

        public SignalRChatNotifier(IHubContext<ChatRoomHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendMessageToGroupAsync(string groupName, object message)
        {
            // "ReceiveMessage" is the event that Frontend (React/Mobile) will receive
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", message);
        }
    }
}