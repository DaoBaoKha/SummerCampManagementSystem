using SummerCampManagementSystem.BLL.DTOs.PromotionType;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class PromotionTypeService : IPromotionTypeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PromotionTypeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<PromotionTypeResponseDto> CreatePromotionTypeAsync(PromotionTypeRequestDto promotionType)
        {
            var newPromotionType = new PromotionType
            {
                name = promotionType.Name,
                description = promotionType.Description,
                createAt = DateTime.UtcNow,
                updateAt = DateTime.UtcNow,
                status = "Active",
            };

            await _unitOfWork.PromotionTypes.CreateAsync(newPromotionType);
            await _unitOfWork.CommitAsync();

            return new PromotionTypeResponseDto
            {
                Id = newPromotionType.promotionTypeId,
                Name = newPromotionType.name,
                Description = newPromotionType.description,
                createAt = DateTime.Now,
                updateAt = DateTime.Now,
                Status = newPromotionType.status,
            };
        }

        public async Task<bool> DeletePromotionTypeAsync(int id)
        {
            var existingPromotionType = await _unitOfWork.PromotionTypes.GetByIdAsync(id);
            if (existingPromotionType == null)
            {
                return false;
            }

            await _unitOfWork.PromotionTypes.RemoveAsync(existingPromotionType);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<PromotionTypeResponseDto>> GetAllPromotionTypesAsync()
        {
            var promotionTypes = await _unitOfWork.PromotionTypes.GetAllAsync();
            return promotionTypes.Select(pt => new PromotionTypeResponseDto
            {
                Id = pt.promotionTypeId,
                Name = pt.name,
                Description = pt.description,
                createAt = pt.createAt,
                updateAt = pt.updateAt,
                Status = pt.status,
            });
        }

        public async Task<PromotionTypeResponseDto?> GetPromotionTypeByIdAsync(int id)
        {
            var promotionType = await _unitOfWork.PromotionTypes.GetByIdAsync(id);
            if (promotionType == null) return null;

            return new PromotionTypeResponseDto
            {
                Id = promotionType.promotionTypeId,
                Name = promotionType.name,
                Description = promotionType.description,
                createAt = promotionType.createAt,
                updateAt = promotionType.updateAt,
                Status = promotionType.status,
            };
        }

        public async Task<PromotionTypeResponseDto?> UpdatePromotionTypeAsync(int id, PromotionTypeRequestDto promotionType)
        {
            var existingPromotionType = await _unitOfWork.PromotionTypes.GetByIdAsync(id);
            if (existingPromotionType == null)
            {
                return null;
            }

            existingPromotionType.name = promotionType.Name;
            existingPromotionType.description = promotionType.Description;
            existingPromotionType.updateAt = DateTime.UtcNow;
            existingPromotionType.status = promotionType.Status;

            await _unitOfWork.PromotionTypes.UpdateAsync(existingPromotionType);
            await _unitOfWork.CommitAsync();

            return new PromotionTypeResponseDto
            {
                Id = existingPromotionType.promotionTypeId,
                Name = existingPromotionType.name,
                Description = existingPromotionType.description,
                createAt = existingPromotionType.createAt,
                updateAt = DateTime.Now,
                Status = existingPromotionType.status,
            };
        }
    }
}
