namespace SummerCampManagementSystem.BLL.DTOs.FAQ
{
    public class FAQResponseDto
    {
        public int FaqId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}
