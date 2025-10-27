using SummerCampManagementSystem.BLL.DTOs.User;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;

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
    }
}
