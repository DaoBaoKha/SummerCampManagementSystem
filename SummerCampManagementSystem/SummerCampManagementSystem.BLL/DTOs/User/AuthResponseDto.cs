namespace SummerCampManagementSystem.BLL.DTOs.User
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
