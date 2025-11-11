namespace SummerCampManagementSystem.BLL.DTOs.User
{
    public class VerifyOtpDto
    {
    }

    public class VerifyOtpRequestDto
    {
        public string Email { get; set; } = null!;
        public string Otp { get; set; } = null!;
    }

    public class VerifyOtpResponseDto
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? Token { get; set; }
    }
}
