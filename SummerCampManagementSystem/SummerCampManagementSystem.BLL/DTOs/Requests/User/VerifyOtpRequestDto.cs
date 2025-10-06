namespace SummerCampManagementSystem.BLL.DTOs.Requests.User
{
    public class VerifyOtpRequestDto
    {
        public string Email { get; set; } = null!;

        public string Otp { get; set; } = null!;
    }
}
