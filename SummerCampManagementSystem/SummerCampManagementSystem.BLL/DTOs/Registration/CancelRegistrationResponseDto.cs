namespace SummerCampManagementSystem.BLL.DTOs.Registration
{
    public class CancelRegistrationResponseDto
    {
        public int RegistrationId { get; set; }
        public string Status { get; set; }
        public decimal? RefundAmount { get; set; }
        public string Message { get; set; }
        public int? RefundPercentage { get; set; }
    }
}
