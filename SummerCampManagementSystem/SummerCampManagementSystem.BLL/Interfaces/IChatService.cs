using static SummerCampManagementSystem.BLL.DTOs.Chat.AIChatboxDto;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponseDto> GenerateResponseAsync(ChatRequestDto requestDto);
    }
}
