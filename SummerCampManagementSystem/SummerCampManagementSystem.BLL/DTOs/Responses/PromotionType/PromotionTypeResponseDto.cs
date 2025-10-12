namespace SummerCampManagementSystem.BLL.DTOs.Responses.PromotionType
{
    public class PromotionTypeResponseDto
    {
        public int Id { get; set; }
        public string? Name { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public DateTime? createAt { get; set; }
        public DateTime? updateAt { get; set; }
        public string Status { get; set; } = null!;
    }
}
