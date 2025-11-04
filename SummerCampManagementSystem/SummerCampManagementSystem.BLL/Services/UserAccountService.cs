using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using SummerCampManagementSystem.BLL.DTOs.User;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;
using static SummerCampManagementSystem.BLL.DTOs.User.EmailDto;

namespace SummerCampManagementSystem.BLL.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService; 
        private readonly IMemoryCache _cache;

        public UserAccountService(IUnitOfWork unitOfWork, IMapper mapper, IUserContextService userContextService,
            IUserService userService, IEmailService emailService, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userContextService = userContextService;
            _userService = userService;
            _emailService = emailService;
            _cache = cache;
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

        public async Task<string> InitiateEmailUpdateAsync(EmailUpdateRequestDto model)
        {
            var userId = GetRequiredUserId();
            var existingUser = await _unitOfWork.Users.GetByIdAsync(userId);

            if (existingUser == null)
            {
                throw new KeyNotFoundException("Không tìm thấy người dùng.");
            }

            // validation
            if (!_userService.VerifyPassword(model.CurrentPassword, existingUser.password))
            {
                throw new UnauthorizedAccessException("Mật khẩu hiện tại không chính xác. Vui lòng thử lại.");
            }

            if (existingUser.email.Equals(model.NewEmail, StringComparison.OrdinalIgnoreCase))
            {
                return "Email mới trùng với email hiện tại.";
            }

            var userWithNewEmail = await _unitOfWork.Users.GetUserByEmail(model.NewEmail);
            if (userWithNewEmail != null && userWithNewEmail.userId != userId)
            {
                throw new ArgumentException("Địa chỉ email mới đã được sử dụng bởi tài khoản khác.");
            }

            // save otp to cache with new userid and email
            var otp = new Random().Next(100000, 999999).ToString();
            var cacheKey = $"OTP_UpdateEmail_{userId}_{model.NewEmail.ToLower()}"; // use new email for key

            _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(5));

            await _emailService.SendOtpEmailAsync(model.NewEmail, otp, "EmailUpdate");

            return "Mã OTP đã được gửi đến email mới của bạn. Vui lòng kiểm tra hộp thư để xác nhận.";
        }

        public async Task<UserResponseDto> VerifyEmailUpdateAsync(EmailUpdateVerificationDto model)
        {
            var userId = GetRequiredUserId();
            var existingUser = await _unitOfWork.Users.GetByIdAsync(userId);

            if (existingUser == null)
            {
                throw new KeyNotFoundException("Không tìm thấy người dùng.");
            }

            var cacheKey = $"OTP_UpdateEmail_{userId}_{model.NewEmail.ToLower()}";

            if (!_cache.TryGetValue(cacheKey, out string? cachedOtp) || cachedOtp != model.Otp)
            {
                throw new InvalidOperationException("OTP không hợp lệ hoặc đã hết hạn.");
            }

            var oldEmail = existingUser.email;

            existingUser.email = model.NewEmail;


            /*
             * if use email to verify -> reset isActive = false so user verify again (based on bussiness rule)
             * right now only isActive = true and verify using password
             */
            await _unitOfWork.Users.UpdateAsync(existingUser);
            await _unitOfWork.CommitAsync();

            _cache.Remove(cacheKey);

            await _emailService.SendEmailUpdateSuccessAsync(model.NewEmail, oldEmail);

            return _mapper.Map<UserResponseDto>(existingUser);
        }

        public async Task<(bool isSuccess, string? message)> ChangePasswordAsync(int userId, ChangePasswordRequestDto model)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                return (false, "Yêu cầu không hợp lệ.");
            }

            if (!_userService.VerifyPassword(model.CurrentPassword, user.password))
            {
                return (false, "Mật khẩu hiện tại không chính xác.");
            }

            if (model.NewPassword.Equals(model.CurrentPassword))
            {
                return (false, "Mật khẩu mới không được trùng với mật khẩu hiện tại.");
            }

            // hash and update new password
            user.password = _userService.HashPassword(model.NewPassword);

            await _unitOfWork.Users.UpdateAsync(user);

            // revoke token to request relogin from user
            await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId);

            await _unitOfWork.CommitAsync();

            return (true, "Mật khẩu đã được thay đổi thành công. Vui lòng đăng nhập lại với mật khẩu mới.");
        }
    }
}
