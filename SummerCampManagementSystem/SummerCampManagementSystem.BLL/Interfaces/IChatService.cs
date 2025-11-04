using SummerCampManagementSystem.BLL.DTOs.Chat;
using static SummerCampManagementSystem.BLL.DTOs.Chat.AIChatboxDto;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponseDto> GenerateResponseAsync(ChatRequestDto request);

        Task<IEnumerable<ChatConversationDto>> GetConversationHistoryAsync();

        Task<IEnumerable<ChatMessageDto>> GetMessagesByConversationIdAsync(int conversationId);

        Task DeleteConversationAsync(int conversationId);
    }
}
