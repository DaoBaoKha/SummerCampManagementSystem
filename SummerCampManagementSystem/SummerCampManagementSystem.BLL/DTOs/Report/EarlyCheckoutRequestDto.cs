namespace SummerCampManagementSystem.BLL.DTOs.Report
{
    public class EarlyCheckoutRequestDto
    {
        public int camperId { get; set; }
        
        public string note { get; set; } = string.Empty;
        
        public string? imageUrl { get; set; }
    }
}
