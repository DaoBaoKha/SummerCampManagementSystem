using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SummerCampManagementSystem.BLL.DTOs.Requests.User;
using SummerCampManagementSystem.BLL.DTOs.Responses;
using SummerCampManagementSystem.BLL.DTOs.Responses.User;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
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

        public UserService(IUnitOfWork unitOfWork, IConfiguration config, IMemoryCache memoryCache, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _cache = memoryCache;
            _emailService = emailService;
        }


        public async Task<(AuthResponseDto? authResponse, string? errorMessage)> LoginAsync(LoginRequestDto model)
        {
            try
            {
                Console.WriteLine($"Login attempt for email: {model.Email}");

                var user = await _unitOfWork.Users.GetUserByEmail(model.Email);

                if (user == null)
                {
                    Console.WriteLine($"User not found: {model.Email}");
                    return (null, "Invalid email or password.");
                }

                if (string.IsNullOrEmpty(user.password))
                {
                    Console.WriteLine($"User has no password set: {model.Email}");
                    return (null, "Invalid email or password.");
                }

                if (!VerifyPassword(model.Password, user.password))
                {
                    Console.WriteLine($"Invalid password for: {model.Email}");
                    return (null, "Invalid email or password.");
                }

                // **FIX: Check if user is active**
                if (user.isActive == false)
                {
                    Console.WriteLine($"User not activated: {model.Email}");
                    return (null, "Tài khoản chưa được kích hoạt. Vui lòng kiểm tra email để kích hoạt tài khoản.");
                }

                Console.WriteLine($"Login successful for: {model.Email}");
                var authResponse = await CreateAuthResponseAsync(user);
                authResponse.Message = "Login successful!";

                return (authResponse, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return (null, "Login failed. Please try again.");
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

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string enteredPassword, string hashedPassword)
        {
            string hashedEnteredPassword = HashPassword(enteredPassword);
            return hashedEnteredPassword == hashedPassword;
        }

        private string GenerateJwtToken(UserAccount user)
        {
            var jwtKey = _config["Jwt:Key"];
            var jwtIssuer = _config["Jwt:Issuer"];
            var jwtAudience = _config["Jwt:Audience"];

            // **FIX: Better error logging**
            if (string.IsNullOrEmpty(jwtKey))
            {
                Console.WriteLine("JWT Key is null or empty!");
                throw new InvalidOperationException("JWT Key configuration is missing.");
            }

            if (string.IsNullOrEmpty(jwtIssuer))
            {
                Console.WriteLine("JWT Issuer is null or empty!");
                throw new InvalidOperationException("JWT Issuer configuration is missing.");
            }

            if (string.IsNullOrEmpty(jwtAudience))
            {
                Console.WriteLine("JWT Audience is null or empty!");
                throw new InvalidOperationException("JWT Audience configuration is missing.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.userId.ToString()),
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
            try
            {
                Console.WriteLine($"Registration attempt for: {model.Email}");

                var existingUser = await _unitOfWork.Users.GetUserByEmail(model.Email);
                if (existingUser != null)
                {
                    Console.WriteLine($"User already exists: {model.Email}");
                    return null;
                }

                var newUser = new UserAccount
                {
                    firstName = model.FirstName,
                    lastName = model.LastName,
                    email = model.Email,
                    phoneNumber = model.PhoneNumber,
                    password = HashPassword(model.Password),
                    dob = model.Dob,
                    role = "User", // Default role
                    isActive = false,
                    createAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.CreateAsync(newUser);
                await _unitOfWork.CommitAsync();

                var otp = new Random().Next(100000, 999999).ToString();

                // store otp in cache for 5 mins
                _cache.Set($"OTP_Activation_{model.Email}", otp, TimeSpan.FromMinutes(5));
                Console.WriteLine($"OTP generated and cached for: {model.Email}");

                // **FIX: Validate email before sending**
                if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains("@"))
                {
                    throw new ArgumentException($"Địa chỉ Email không chính xác: {model.Email}");
                }

                await _emailService.SendOtpEmailAsync(model.Email, otp, "Activation");
                Console.WriteLine($"OTP email sent to: {model.Email}");

                return new RegisterUserResponseDto
                {
                    UserId = newUser.userId,
                    Message = "Đăng ký thành công! OTP đã được gửi tới email của bạn."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
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
                Console.WriteLine($"User not found: {model.Email}");
                return new VerifyOtpResponseDto { IsSuccess = false, Message = "Không tìm thấy người dùng!" };
            }

            // activate user
            user.isActive = true;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CommitAsync();

            // **FIX: Remove correct cache key**
            _cache.Remove(cacheKey);
            Console.WriteLine($"User activated: {model.Email}");

            return new VerifyOtpResponseDto
            {
                IsSuccess = true,
                Message = "Tài khoản đã được kích hoạt thành công!"
            };
        }

        public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(string email)
        {
            try
            {
                Console.WriteLine($"Forgot password request for: {email}");

                // **FIX: Validate email first**
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                {
                    Console.WriteLine($"Invalid email format: {email}");
                    return new ForgotPasswordResponseDto
                    {
                        Email = email,
                        Message = "Địa chỉ email không hợp lệ."
                    };
                }

                var user = await _unitOfWork.Users.GetUserByEmail(email);
                if (user == null)
                {
                    Console.WriteLine($"User not found: {email}");
                    return new ForgotPasswordResponseDto
                    {
                        Email = email,
                        Message = "Email không tồn tại trong hệ thống."
                    };
                }

                var otp = new Random().Next(100000, 999999).ToString();

                _cache.Set($"OTP_ResetPassword_{email}", otp, TimeSpan.FromMinutes(5));
                Console.WriteLine($"OTP generated for password reset: {email}");

                await _emailService.SendOtpEmailAsync(email, otp, "ResetPassword");
                Console.WriteLine($"Password reset OTP sent to: {email}");

                return new ForgotPasswordResponseDto
                {
                    Email = email,
                    Message = "Mã OTP đặt lại mật khẩu đã được gửi tới email của bạn."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Forgot password error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<ForgotPasswordResponseDto?> ResetPasswordAsync(ResetPasswordRequestDto model)
        {
            try
            {
                Console.WriteLine($"Reset password attempt for: {model.Email}");

                // Kiểm tra OTP trong cache
                if (!_cache.TryGetValue($"OTP_ResetPassword_{model.Email}", out string? cachedOtp) || cachedOtp != model.Otp)
                {
                    Console.WriteLine($"Invalid OTP for password reset: {model.Email}");
                    return new ForgotPasswordResponseDto
                    {
                        Email = model.Email,
                        Message = "OTP không hợp lệ hoặc đã hết hạn."
                    };
                }

                var user = await _unitOfWork.Users.GetUserByEmail(model.Email);
                if (user == null)
                {
                    Console.WriteLine($"User not found: {model.Email}");
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
                Console.WriteLine($"Password reset successful for: {model.Email}");

                return new ForgotPasswordResponseDto
                {
                    Email = model.Email,
                    Message = "Mật khẩu đã được đặt lại thành công."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reset password error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
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
                Role = user.role,
                IsActive = (bool)user.isActive,

            };
        }
    }
}