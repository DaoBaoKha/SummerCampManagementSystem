namespace SummerCampManagementSystem.BLL.DTOs.PayOS
{
    public class WebCallbackResponseDto
    {
        public bool IsSuccess { get; set; }
        public int OrderCode { get; set; }
        public string Status { get; set; } = "PENDING";
        public string Message { get; set; } = "";
        public string Detail { get; set; } = "";
    }
}
