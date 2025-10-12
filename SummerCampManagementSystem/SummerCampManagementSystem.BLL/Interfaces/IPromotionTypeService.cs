using SummerCampManagementSystem.BLL.DTOs.Requests.PromotionType;
using SummerCampManagementSystem.BLL.DTOs.Responses.PromotionType;
using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IPromotionTypeService
    {
        Task<IEnumerable<PromotionTypeResponseDto>> GetAllPromotionTypesAsync();
        Task<PromotionTypeResponseDto?> GetPromotionTypeByIdAsync(int id);
        Task<PromotionTypeResponseDto> CreatePromotionTypeAsync(PromotionTypeRequestDto promotionType);
        Task<PromotionTypeResponseDto?> UpdatePromotionTypeAsync(int id, PromotionTypeRequestDto promotionType);
        Task<bool> DeletePromotionTypeAsync(int id);
    }
}
