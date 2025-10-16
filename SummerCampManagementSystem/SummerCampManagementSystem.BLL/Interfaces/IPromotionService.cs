using SummerCampManagementSystem.BLL.DTOs.Promotion;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IPromotionService
    {
        Task<IEnumerable<PromotionResponseDto>> GetAllPromotionsAsync();
        Task<PromotionResponseDto> GetPromotionByIdAsync(int id);
        Task<PromotionResponseDto> CreatePromotionAsync(PromotionRequestDto promotion);
        Task<PromotionResponseDto> UpdatePromotionAsync(int promotionId, PromotionRequestDto promotion);
        Task<bool> DeletePromotionAsync(int id);
    }
}
