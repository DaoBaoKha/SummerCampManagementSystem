using SummerCampManagementSystem.BLL.DTOs.ChatRoom;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IChatRoomService
    {
        Task<ChatRoomMessageDto> SendMessageAsync(int userId, SendMessageDto request);
    }
}
