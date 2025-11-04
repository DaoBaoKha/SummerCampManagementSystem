using SummerCampManagementSystem.BLL.DTOs.User;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;
using static SummerCampManagementSystem.BLL.DTOs.User.EmailDto;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IUserAccountService
    {
        Task<UserResponseDto> GetCurrentUserProfileAsync();
        Task<UserResponseDto> UpdateUserProfileAsync(UserProfileUpdateDto updateDto);
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(int userId);
        Task<UserResponseDto> UpdateUserByAdminAsync(int userId, UserAdminUpdateDto updateDto);
        Task<bool> DeleteUserAsync(int userId);
        Task<string> InitiateEmailUpdateAsync(EmailUpdateRequestDto model);
        Task<UserResponseDto> VerifyEmailUpdateAsync(EmailUpdateVerificationDto model);
        Task<(bool isSuccess, string? message)> ChangePasswordAsync(int userId, ChangePasswordRequestDto model);
    }
}
