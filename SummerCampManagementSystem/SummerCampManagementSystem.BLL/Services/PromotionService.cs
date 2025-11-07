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

            if (promotion.StartDate.HasValue && promotion.EndDate.HasValue)
            {
                // check if start date is before end date
                if (promotion.EndDate.Value <= promotion.StartDate.Value)
                {
                    throw new InvalidOperationException("Ngày kết thúc phải ít nhất một ngày sau Ngày bắt đầu.");
                }
            }

            // check if StartDate is in the future (for new promotions)
            if (existingId == null && promotion.StartDate.HasValue && promotion.StartDate.Value < today)
            {
                throw new InvalidOperationException("Ngày bắt đầu phải là ngày hôm nay hoặc sau này.");
            }

          
            if (promotion.MaxUsageCount.HasValue && promotion.MaxUsageCount.Value <= 0)
            {
                throw new InvalidOperationException("Số lượt sử dụng tối đa phải là số dương.");
            }


            if (!promotion.Percent.HasValue && !promotion.MaxDiscountAmount.HasValue)
            {
                throw new InvalidOperationException("Khuyến mãi phải có ít nhất một giá trị giảm giá (Phần trăm hoặc Giảm giá tối đa).");
            }

            if (string.IsNullOrWhiteSpace(promotion.Name))
            {
                throw new InvalidOperationException("Tên Khuyến mãi là bắt buộc.");
            }
            if (!promotion.PromotionTypeId.HasValue)
            {
                throw new InvalidOperationException("Loại Khuyến mãi là bắt buộc.");
            }

            if (promotion.PromotionTypeId.HasValue)
            {
                var typeExists = await _unitOfWork.PromotionTypes.GetByIdAsync(promotion.PromotionTypeId.Value);
                if (typeExists == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy Loại Khuyến mãi với ID {promotion.PromotionTypeId.Value}.");
                }
            }

            // check same code
            if (!string.IsNullOrWhiteSpace(promotion.Code))
            {
                var codeExists = await _unitOfWork.Promotions.GetQueryable()
                    .AnyAsync(p => p.code == promotion.Code && p.promotionId != existingId);

                if (codeExists)
                {
                    throw new InvalidOperationException($"Mã Khuyến mãi '{promotion.Code}' đã tồn tại.");
                }
            }
        }

        public async Task<PromotionResponseDto> CreatePromotionAsync(PromotionRequestDto promotion)
        {
            var currentUserId = _userContextService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                throw new UnauthorizedAccessException("Không thể lấy ID người dùng từ token. Vui lòng đăng nhập lại.");
            }

            await ValidatePromotionAsync(promotion);

            var newPromotion = _mapper.Map<Promotion>(promotion);

            newPromotion.createBy = currentUserId.Value;
            newPromotion.createAt = DateTime.UtcNow;
            newPromotion.currentUsageCount = 0; // Initialize usage count

            newPromotion.status = "Active";

            await _unitOfWork.Promotions.CreateAsync(newPromotion);
            await _unitOfWork.CommitAsync();

            int createdPromotionId = newPromotion.promotionId;

            var createdEntity = await GetPromotionsWithIncludes()
                .FirstOrDefaultAsync(p => p.promotionId == createdPromotionId);

            if (createdEntity == null)
            {
                throw new Exception("Không thể truy xuất Khuyến mãi đã tạo để mapping.");
            }

            return _mapper.Map<PromotionResponseDto>(createdEntity);
        }


        public async Task<PromotionResponseDto> UpdatePromotionAsync(int promotionId, PromotionRequestDto promotion)
        {
            var existingPromotion = await GetPromotionsWithIncludes()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.promotionId == promotionId);

            if (existingPromotion == null) throw new KeyNotFoundException($"Không tìm thấy Khuyến mãi với ID {promotionId}.");

            await ValidatePromotionAsync(promotion, promotionId);

            // map dto into entity, keep id and usage count
            var currentUsageCount = existingPromotion.currentUsageCount; // Preserve current usage count

            _mapper.Map(promotion, existingPromotion);

            existingPromotion.promotionId = promotionId;
            existingPromotion.currentUsageCount = currentUsageCount; // Restore usage count

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

                    // valid date: endDate must be today or later
                    (!p.endDate.HasValue || p.endDate.Value >= today) &&

                    // check usage limit: maxUsageCount is null OR currentUsageCount < maxUsageCount
                    (!p.maxUsageCount.HasValue || p.currentUsageCount < p.maxUsageCount.Value) &&

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