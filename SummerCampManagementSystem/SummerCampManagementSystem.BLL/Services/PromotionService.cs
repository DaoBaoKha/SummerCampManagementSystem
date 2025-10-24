using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Promotion;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;

        public PromotionService(IUnitOfWork unitOfWork, IMapper mapper, IUserContextService userContextService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userContextService = userContextService;
        }

        private async Task ValidatePromotionAsync(PromotionRequestDto promotion, int? existingId = null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var tomorrow = today.AddDays(1);

            if (promotion.startDate.HasValue && promotion.endDate.HasValue)
            {
                if (promotion.endDate.Value <= promotion.startDate.Value)
                {
                    throw new InvalidOperationException("End Date must be at least one day after the Start Date, ensuring a minimum validity of 1 day.");
                }
            }

            if (existingId == null && promotion.startDate.HasValue && promotion.startDate.Value < tomorrow)
            {
                throw new InvalidOperationException("Start Date must be tomorrow or later.");
            }

            if (!promotion.Percent.HasValue && !promotion.MaxDiscountAmount.HasValue)
            {
                throw new InvalidOperationException("Promotion must have at least one discount value (Percent or Max Discount Amount).");
            }

            if (string.IsNullOrWhiteSpace(promotion.Name))
            {
                throw new InvalidOperationException("Promotion Name is required.");
            }
            if (!promotion.PromotionTypeId.HasValue)
            {
                throw new InvalidOperationException("Promotion Type is required.");
            }

            if (promotion.PromotionTypeId.HasValue)
            {
                var typeExists = await _unitOfWork.PromotionTypes.GetByIdAsync(promotion.PromotionTypeId.Value);
                if (typeExists == null)
                {
                    throw new KeyNotFoundException($"Promotion Type with ID {promotion.PromotionTypeId.Value} not found.");
                }
            }

            // check same code
            if (!string.IsNullOrWhiteSpace(promotion.Code))
            {
                var codeExists = await _unitOfWork.Promotions.GetQueryable()
                    .AnyAsync(p => p.code == promotion.Code && p.promotionId != existingId);

                if (codeExists)
                {
                    throw new InvalidOperationException($"Promotion Code '{promotion.Code}' already exists.");
                }
            }
        }
        public async Task<PromotionResponseDto> CreatePromotionAsync(PromotionRequestDto promotion)
        {
            var currentUserId = _userContextService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                throw new UnauthorizedAccessException("Cannot get user ID from token. Please login again.");
            }

            await ValidatePromotionAsync(promotion);

            var newPromotion = _mapper.Map<Promotion>(promotion);

            newPromotion.createBy = currentUserId.Value;
            newPromotion.createAt = DateTime.UtcNow;

            newPromotion.status = "Active";

            await _unitOfWork.Promotions.CreateAsync(newPromotion);
            await _unitOfWork.CommitAsync();

            int createdPromotionId = newPromotion.promotionId;

            var createdEntity = await GetPromotionsWithIncludes()
                .FirstOrDefaultAsync(p => p.promotionId == createdPromotionId);

            if (createdEntity == null)
            {
                throw new Exception("Failed to retrieve the created promotion for mapping.");
            }

            return _mapper.Map<PromotionResponseDto>(createdEntity);
        }


        public async Task<PromotionResponseDto> UpdatePromotionAsync(int promotionId, PromotionRequestDto promotion)
        {
            var existingPromotion = await GetPromotionsWithIncludes()
                .AsNoTracking() 
                .FirstOrDefaultAsync(p => p.promotionId == promotionId);

            if (existingPromotion == null) throw new KeyNotFoundException($"Promotion with ID {promotionId} not found.");

            await ValidatePromotionAsync(promotion, promotionId);

            // map dto into entity, keep id
            _mapper.Map(promotion, existingPromotion);
            existingPromotion.promotionId = promotionId; 

            _unitOfWork.Promotions.Attach(existingPromotion);
            await _unitOfWork.Promotions.UpdateAsync(existingPromotion);
            await _unitOfWork.CommitAsync();

            var updatedEntity = await GetPromotionsWithIncludes()
                .FirstOrDefaultAsync(p => p.promotionId == promotionId);

            return _mapper.Map<PromotionResponseDto>(updatedEntity);
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
            var promotions = await GetPromotionsWithIncludes().ToListAsync();

            return _mapper.Map<IEnumerable<PromotionResponseDto>>(promotions);
        }

        public async Task<PromotionResponseDto> GetPromotionByIdAsync(int id)
        {
            var promotion = await GetPromotionsWithIncludes()
                .FirstOrDefaultAsync(c => c.promotionId == id);

            return promotion == null ? null : _mapper.Map<PromotionResponseDto>(promotion);
        }

        public async Task<IEnumerable<PromotionResponseDto>> GetValidPromotionsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var validPromotions = await GetPromotionsWithIncludes()
                .Where(p =>
                    p.status == "Active" &&

                    // valid date: endDate must be today or tomorrow
                    // if EndDate null, it will always be valid (CONSIDERING THIS!)
                    (!p.endDate.HasValue || p.endDate.Value >= today) &&

                    // must have at least percent and maxDiscountAmount
                    (p.percent.HasValue || p.maxDiscountAmount.HasValue) &&

                    // startDate mustnt be after endDate
                    (!p.startDate.HasValue || !p.endDate.HasValue || p.startDate.Value <= p.endDate.Value)
                )
                .OrderBy(p => p.startDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<PromotionResponseDto>>(validPromotions);
        }

        private IQueryable<Promotion> GetPromotionsWithIncludes()
        {
            //load related entities
            return _unitOfWork.Promotions.GetQueryable()
                .Include(c => c.promotionType);
        }
    }
}