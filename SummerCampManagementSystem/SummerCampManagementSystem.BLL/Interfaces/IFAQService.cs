using SummerCampManagementSystem.BLL.DTOs.FAQ;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IFAQService
    {
        Task<FAQResponseDto> CreateFAQAsync(FAQRequestDto dto);
        Task<FAQResponseDto?> GetFAQByIdAsync(int faqId);
        Task<IEnumerable<FAQResponseDto>> GetAllFAQsAsync();
        Task<FAQResponseDto?> UpdateFAQAsync(int faqId, FAQRequestDto dto);
        Task<bool> DeleteFAQAsync(int faqId);
    }
}
