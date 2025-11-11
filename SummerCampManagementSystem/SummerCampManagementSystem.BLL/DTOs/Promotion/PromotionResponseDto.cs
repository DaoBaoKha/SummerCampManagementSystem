using SummerCampManagementSystem.BLL.DTOs.PromotionType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Promotion
{
    public class PromotionResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly? startDate { get; set; }
        public DateOnly? endDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; }
        public decimal? Percent { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int? createBy { get; set; }
        public DateTime? createAt { get; set; }
        public PromotionTypeNameResponseDto? PromotionType { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    public class PromotionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; 
    }

    public class PromotionSummaryDto
    {
        public int PromotionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? Percent { get; set; }
    }

    public class PromotionSummaryForCampDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? Percent { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int? MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; }
    }
}
