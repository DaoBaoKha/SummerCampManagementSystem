namespace SummerCampManagementSystem.BLL.DTOs.User
{
    public class VerifyOtpResponseDto
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? Token { get; set; }
    }
}
