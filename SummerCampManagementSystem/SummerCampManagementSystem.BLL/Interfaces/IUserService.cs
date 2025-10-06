using SummerCampManagementSystem.BLL.DTOs.Requests.User;
using SummerCampManagementSystem.BLL.DTOs.Responses;
using SummerCampManagementSystem.BLL.DTOs.Responses.User;
using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IUserService
    {
        Task<(AuthResponseDto? authResponse, string? errorMessage)> LoginAsync(LoginRequestDto model);
        Task<bool> LogoutAsync(int userId);

        Task<RegisterUserResponseDto?> RegisterAsync(RegisterUserRequestDto model);

        Task<VerifyOtpResponseDto?> VerifyOtpAsync(VerifyOtpRequestDto model);
    }
}
