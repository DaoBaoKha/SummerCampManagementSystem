namespace SummerCampManagementSystem.BLL.DTOs.PromotionType
{
    public class PromotionTypeRequestDto
    {
        public string? Name { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public DateTime? createAt { get; set; }
        public DateTime? updateAt { get; set; }
        public string Status { get; set; } = null!;
    }
}
