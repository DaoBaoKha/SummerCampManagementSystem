namespace SummerCampManagementSystem.BLL.DTOs.User
{
    public class VerifyOtpRequestDto
    {
        public string Email { get; set; } = null!;
        public string Otp { get; set; } = null!;
    }

}
