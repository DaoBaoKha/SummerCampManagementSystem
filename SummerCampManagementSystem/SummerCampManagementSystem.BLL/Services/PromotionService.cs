using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.DTOs.Promotion;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PromotionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PromotionResponseDto> CreatePromotionAsync(PromotionRequestDto promotion)
        {
            var newPromotion = _mapper.Map<Promotion>(promotion);

            await _unitOfWork.Promotions.CreateAsync(newPromotion);
            await _unitOfWork.CommitAsync();

            int createdPromotionId = newPromotion.promotionId;

            newPromotion = await GetPromotionsWithIncludes()
                .FirstOrDefaultAsync(p => p.promotionId == createdPromotionId);

            if (newPromotion == null)
            {
                throw new Exception("Failed to retrieve the created promotion for mapping.");
            }

            return _mapper.Map<PromotionResponseDto>(newPromotion);
        }

        public async Task<bool> DeletePromotionAsync(int id)
        {
            var existingPromotion = await _unitOfWork.Promotions.GetByIdAsync(id);

            if (existingPromotion == null) return false;

            await _unitOfWork.Promotions.RemoveAsync(existingPromotion);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<PromotionResponseDto>> GetAllPromotionsAsync()
        {
            var promotions = await _unitOfWork.Promotions.GetAllAsync();

            return _mapper.Map<IEnumerable<PromotionResponseDto>>(promotions);
        }

        public async Task<PromotionResponseDto> GetPromotionByIdAsync(int id)
        {
            var promotion = await GetPromotionsWithIncludes()
                .FirstOrDefaultAsync(c => c.promotionId == id);

            return promotion == null ? null : _mapper.Map<PromotionResponseDto>(promotion);
        }

        public async Task<PromotionResponseDto> UpdatePromotionAsync(int promotionId, PromotionRequestDto promotion)
        {
            var existingPromotion = await GetPromotionsWithIncludes()
                .FirstOrDefaultAsync(p => p.promotionId == promotionId); 

            if (existingPromotion == null) throw new Exception("Promotion not found");

            _mapper.Map(promotion, existingPromotion);
            await _unitOfWork.Promotions.UpdateAsync(existingPromotion);
            await _unitOfWork.CommitAsync();

            existingPromotion = await GetPromotionsWithIncludes()
                .FirstOrDefaultAsync(p => p.promotionId == promotionId); 

            return _mapper.Map<PromotionResponseDto>(existingPromotion);
        }

        //private mapping
        private IQueryable<Promotion> GetPromotionsWithIncludes()
        {
            //load related entities
            return _unitOfWork.Promotions.GetQueryable()
                .Include(c => c.promotionType);
        }
    }
}
