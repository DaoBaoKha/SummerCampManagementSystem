namespace SummerCampManagementSystem.BLL.DTOs.Promotion
{
    public class PromotionRequestDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateOnly? startDate { get; set; }
        public DateOnly? endDate { get; set; }
        public decimal? Percent { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int? PromotionTypeId { get; set; }
        public string Code { get; set; }
    }
}
