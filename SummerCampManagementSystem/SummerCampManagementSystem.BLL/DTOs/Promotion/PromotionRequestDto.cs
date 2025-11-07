using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.Promotion
{
    public class PromotionRequestDto
    {
        [Required(ErrorMessage = "Tên khuyến mãi là bắt buộc.")]
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public decimal? Percent { get; set; }
        public decimal? MaxDiscountAmount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượt sử dụng tối đa phải lớn hơn 0.")]
        public int? MaxUsageCount { get; set; }

        [Required(ErrorMessage = "Mã khuyến mãi là bắt buộc.")]
        public string Code { get; set; }

        public int? PromotionTypeId { get; set; } 
    }
}
