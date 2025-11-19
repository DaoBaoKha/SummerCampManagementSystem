using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SummerCampManagementSystem.BLL.DTOs.User;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SummerCampManagementSystem.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly IUploadSupabaseService _uploadSupabaseService;

        public UserService(IUnitOfWork unitOfWork, IConfiguration config, IMemoryCache memoryCache,
            IEmailService emailService, IMapper mapper, IUploadSupabaseService uploadSupabaseService)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _cache = memoryCache;
            _emailService = emailService;
            _mapper = mapper;
            _uploadSupabaseService = uploadSupabaseService;
        }


        public async Task<(AuthResponseDto? authResponse, string? errorMessage, string? errorCode)> LoginAsync(LoginRequestDto model)
        {
            try
            {
                var user = await _unitOfWork.Users.GetUserByEmail(model.Email);

                if (user == null || string.IsNullOrEmpty(user.password) || !VerifyPassword(model.Password, user.password))
                {
                    return (null, "Email hoặc mật khẩu không chính xác!", "INVALID_CREDENTIALS");
                }
                
                if(user.isActive == false)
                {
                    return (null, "Tài khoản chưa được xác thục. Vui lòng kiểm tra email để kích hoạt tài khoản.", "ACCOUNT_NOT_ACTIVE");
                }

                var authResponse = await CreateAuthResponseAsync(user);
                authResponse.Message = "Đăng nhập thành công!";

                return (authResponse, null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login failed: {ex.Message}");
                return (null, "Đăng nhập thất bại. Vui lòng thử lại sau.", "INTERNAL_ERROR");
            }
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            try
            {
                await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId);
                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout failed: {ex.Message}");
                return false;
            }
        }

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public bool VerifyPassword(string enteredPassword, string hashedPassword)
        {
            string hashedEnteredPassword = HashPassword(enteredPassword);
            return hashedEnteredPassword == hashedPassword;
        }

        private string GenerateJwtToken(UserAccount user)
        {
            var jwtKey = _config["Jwt:Key"];
            var jwtIssuer = _config["Jwt:Issuer"];
            var jwtAudience = _config["Jwt:Audience"];

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            {
                throw new InvalidOperationException("JWT configuration is incomplete.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                //new Claim(JwtRegisteredClaimNames.Sub, user.userId.ToString()),
                new Claim("id", user.userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.email),
                new Claim(JwtRegisteredClaimNames.Name, user.firstName + " " + user.lastName),
                new Claim(ClaimTypes.Role, user.role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private async Task<AuthResponseDto> CreateAuthResponseAsync(UserAccount user)
        {
            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Save refresh token to database
            var refreshTokenEntity = new RefreshToken
            {
                userId = user.userId,
                token = refreshToken,
                createdAt = DateTime.UtcNow,
                expiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _unitOfWork.RefreshTokens.CreateAsync(refreshTokenEntity);
            await _unitOfWork.CommitAsync();

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                Message = "Authentication successful!"
            };
        }

        public async Task<RegisterUserResponseDto?> RegisterAsync(RegisterUserRequestDto model)
        {
            var existingUser = await _unitOfWork.Users.GetUserByEmail(model.Email);
            if (existingUser != null)
            {
                return null;
            }

            var newUser = new UserAccount
            {
                firstName = model.FirstName,
                lastName = model.LastName,
                email = model.Email,
                phoneNumber = model.PhoneNumber,
                password = HashPassword(model.Password),
                avatar = string.Empty,
                dob = model.Dob,
                role = UserRole.User.ToString(), // Default role
                isActive = false,
                createAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.CreateAsync(newUser);
            await _unitOfWork.CommitAsync();


            if (model.Avatar != null)
            {
                var avatarUrl = await _uploadSupabaseService.UploadUserAvatarAsync(newUser.userId, model.Avatar);
                newUser.avatar = avatarUrl;

                await _unitOfWork.Users.UpdateAsync(newUser);
                await _unitOfWork.CommitAsync();
            }

            var otp = new Random().Next(100000, 999999).ToString();

            // store otp in cache for 5 mins
            _cache.Set($"OTP_Activation_{model.Email}", otp, TimeSpan.FromMinutes(5));

            // send otp
            if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains("@"))
            {
                throw new ArgumentException($"Địa chỉ Email không chính xác: {model.Email}");
            }

            await _emailService.SendOtpEmailAsync(model.Email, otp, "Activation");

            return new RegisterUserResponseDto
            {
                UserId = newUser.userId,
                Message = "Đăng ký thành công! OTP đã được gửi tới email của bạn."
            };

        }

        public async Task<RegisterUserResponseDto?> CreateStaffAccountAsync(RegisterStaffRequestDto model)
        {
            if (await _unitOfWork.Users.GetUserByEmail(model.Email) != null)
                return null;

            var newUser = _mapper.Map<UserAccount>(model);
            newUser.password = HashPassword(model.Password);
            newUser.isActive = true; // Admin tạo → active sẵn
            newUser.createAt = DateTime.UtcNow;

            await _unitOfWork.Users.CreateAsync(newUser);
            await _unitOfWork.CommitAsync();

            if (model.Avatar != null)
            {
                var avatarUrl = await _uploadSupabaseService.UploadStaffAvatarAsync(newUser.userId, model.Avatar);
                newUser.avatar = avatarUrl;

                await _unitOfWork.Users.UpdateAsync(newUser);
                await _unitOfWork.CommitAsync();
            }

            await _emailService.SendAccountCreatedEmailAsync(newUser.email, newUser.role.ToString());

            return new RegisterUserResponseDto
            {
                UserId = newUser.userId,
                Message = $"Tài khoản {newUser.role} tạo thành công!"
            };
        }

        public async Task<VerifyOtpResponseDto?> VerifyOtpAsync(VerifyOtpRequestDto model)
        {
            var cacheKey = $"OTP_Activation_{model.Email}";
            
            // try to get OTP from cache
            var hasOtp = _cache.TryGetValue(cacheKey, out string? cachedOtp);
            Console.WriteLine($"Verifying OTP for {model.Email}. OTP found in cache: {hasOtp}");
            
            if (!hasOtp || cachedOtp != model.Otp)
            {
                Console.WriteLine($"OTP verification failed. Cached OTP exists: {hasOtp}, OTP match: {cachedOtp == model.Otp}");
                return new VerifyOtpResponseDto 
                { 
                    IsSuccess = false, 
                    Message = "OTP không hợp lệ hoặc đã hết hạn!" 
                };
            }

            // find user by email
            var user = await _unitOfWork.Users.GetUserByEmail(model.Email);
            if (user == null)
            {
                return new VerifyOtpResponseDto { IsSuccess = false, Message = "Không tìm thấy người dùng!" };
            }

            // activate user
            user.isActive = true;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CommitAsync();

            // Xóa OTP khỏi cache
            _cache.Remove($"OTP_{model.Email}");

            return new VerifyOtpResponseDto
            {
                IsSuccess = true,
                Message = "Tài khoản đã được kích hoạt thành công!"
            };
        }

        public async Task<VerifyOtpResponseDto> ResendActivationOtpAsync(string email)
        {
            var user = await _unitOfWork.Users.GetUserByEmail(email);
            if (user == null)
            {
                return new VerifyOtpResponseDto
                {
                    IsSuccess = false,
                    Message = "Không tìm thấy người dùng!"
                };
            }

            if (user.isActive == true)
            {
                return new VerifyOtpResponseDto
                {
                    IsSuccess = false,
                    Message = "Tài khoản đã được kích hoạt, không cần gửi OTP."
                };
            }

            var otp = new Random().Next(100000, 999999).ToString();

            _cache.Set($"OTP_Activation_{email}", otp, TimeSpan.FromMinutes(5));

            await _emailService.SendOtpEmailAsync(email, otp, "Activation");

            return new VerifyOtpResponseDto
            {
                IsSuccess = true,
                Message = "Mã OTP mới đã được gửi tới email của bạn."
            };
        }


        public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(string email)
        {
            var user = await _unitOfWork.Users.GetUserByEmail(email);
            if (user == null)
            {
                return new ForgotPasswordResponseDto
                {
                    Email = email,
                    Message = "Email không tồn tại trong hệ thống."
                };
            }

            var otp = new Random().Next(100000, 999999).ToString();

            _cache.Set($"OTP_ResetPassword_{email}", otp, TimeSpan.FromMinutes(5));

            await _emailService.SendOtpEmailAsync(email, otp, "ResetPassword");

            return new ForgotPasswordResponseDto
            {
                Email = email,
                Message = "Mã OTP đặt lại mật khẩu đã được gửi tới email của bạn."
            };
        }

        public async Task<ForgotPasswordResponseDto?> ResetPasswordAsync(ResetPasswordRequestDto model)
        {
            // Kiểm tra OTP trong cache
            if (!_cache.TryGetValue($"OTP_ResetPassword_{model.Email}", out string? cachedOtp) || cachedOtp != model.Otp)
            {
                return new ForgotPasswordResponseDto
                {
                    Email = model.Email,
                    Message = "OTP không hợp lệ hoặc đã hết hạn."
                };
            }

            var user = await _unitOfWork.Users.GetUserByEmail(model.Email);
            if (user == null)
            {
                return new ForgotPasswordResponseDto
                {
                    Email = model.Email,
                    Message = "Email không tồn tại trong hệ thống."
                };
            }

            // Cập nhật mật khẩu mới
            user.password = HashPassword(model.NewPassword);
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CommitAsync();

            // Xóa OTP sau khi dùng
            _cache.Remove($"OTP_ResetPassword_{model.Email}");

            return new ForgotPasswordResponseDto
            {
                Email = model.Email,
                Message = "Mật khẩu đã được đặt lại thành công."
            };
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return null;

            return new UserResponseDto
            {
                UserId = user.userId,
                FirstName = user.firstName,
                LastName = user.lastName,
                Email = user.email,
                PhoneNumber = user.phoneNumber,
                DateOfBirth = (DateOnly)user.dob,
                Role = Enum.TryParse<UserRole>(user.role, out var parsedRole) ? parsedRole : UserRole.User,
                IsActive = (bool)user.isActive,

            };
        }

    }
}
