using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.User;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;

        public UserAccountService(IUnitOfWork unitOfWork, IMapper mapper, IUserContextService userContextService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userContextService = userContextService;
        }

        private int GetRequiredUserId()
        {
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("Authentication failed. User ID not found in token.");
            }
            return userId.Value;
        }

        public async Task<UserResponseDto> GetCurrentUserProfileAsync()
        {
            var userId = GetRequiredUserId();
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                throw new KeyNotFoundException($"User account with ID {userId} not found.");
            }

            return _mapper.Map<UserResponseDto>(user);
        }

        public async Task<UserResponseDto> UpdateUserProfileAsync(UserProfileUpdateDto updateDto)
        {
            var userId = GetRequiredUserId();
            var existingUser = await _unitOfWork.Users.GetByIdAsync(userId);

            if (existingUser == null)
            {
                throw new KeyNotFoundException($"User account with ID {userId} not found.");
            }

            _mapper.Map(updateDto, existingUser);

            await _unitOfWork.Users.UpdateAsync(existingUser);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<UserResponseDto>(existingUser);
        }


        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            return _mapper.Map<IEnumerable<UserResponseDto>>(users);
        }

         public async Task<UserResponseDto?> GetUserByIdAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            return user == null ? null : _mapper.Map<UserResponseDto>(user);
        }


        public async Task<UserResponseDto> UpdateUserByAdminAsync(int userId, UserAdminUpdateDto updateDto)
        {
            var existingUser = await _unitOfWork.Users.GetByIdAsync(userId);

            if (existingUser == null)
            {
                throw new KeyNotFoundException($"User account with ID {userId} not found.");
            }

            // Admin is allowed to update Role and IsActive
            existingUser.role = updateDto.Role;
            existingUser.isActive = updateDto.IsActive;

            await _unitOfWork.Users.UpdateAsync(existingUser);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<UserResponseDto>(existingUser);
        }


        public async Task<bool> DeleteUserAsync(int userId)
        {
            var existingUser = await _unitOfWork.Users.GetByIdAsync(userId);

            if (existingUser == null)
            {
                return false;
            }

            // soft delete
            existingUser.isActive = false;
            await _unitOfWork.Users.UpdateAsync(existingUser);
            await _unitOfWork.CommitAsync();

            return true;
        }
    }
}
