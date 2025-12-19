using SummerCampManagementSystem.BLL.DTOs.ChatRoom;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IChatRoomService
    {
        Task<ChatRoomMessageDto> SendMessageAsync(int userId, SendMessageDto request);
        Task<IEnumerable<ChatRoomDetailDto>> GetMyChatRoomsAsync(int userId);
        Task<IEnumerable<ChatRoomMessageDto>> GetMessagesByRoomIdAsync(int userId, int chatRoomId);
        Task<CreateOrGetPrivateRoomResponseDto> CreateOrGetPrivateRoomAsync(int currentUserId, int recipientUserId);
        Task<ChatRoomDetailDto> GetRoomDetailsAsync(int userId, int chatRoomId);
    }
}
