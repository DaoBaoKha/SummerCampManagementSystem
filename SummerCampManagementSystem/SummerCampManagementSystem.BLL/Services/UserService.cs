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
                var user = await _unitOfWork.Users.GetUserByEmail(model.Email);

                if (user == null || string.IsNullOrEmpty(user.password) || !VerifyPassword(model.Password, user.password))
                {
                    return (null, "Invalid email or password.");
                }


                var authResponse = await CreateAuthResponseAsync(user);
                authResponse.Message = "Login successful!";

                return (authResponse, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login failed: {ex.Message}");
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

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            {
                throw new InvalidOperationException("JWT configuration is incomplete.");
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
                dob = model.Dob,
                role = "User", // Default role
                isActive = false,
                createAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.CreateAsync(newUser);
            await _unitOfWork.CommitAsync();


            var otp = new Random().Next(100000, 999999).ToString();

            // store otp in cache for 5 mins
            _cache.Set($"OTP_{model.Email}", otp, TimeSpan.FromMinutes(5));

            // send otp
            if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains("@"))
            {
                throw new ArgumentException($"Địa chỉ Email không chính xác: {model.Email}");
            }

            await _emailService.SendOtpEmailAsync(model.Email, otp);

            return new RegisterUserResponseDto
            {
                UserId = newUser.userId,
                Message = "Đăng ký thành công! OTP đã được gửi tới email của bạn."
            };

        }

        public async Task<VerifyOtpResponseDto?> VerifyOtpAsync(VerifyOtpRequestDto model)
        {
            if(!_cache.TryGetValue($"OTP_{model.Email}", out string? cachedOtp) || cachedOtp != model.Otp)
            {
                return new VerifyOtpResponseDto { IsSuccess = false, Message = "OTP Expired or Not Found!"};
            }

            if(cachedOtp != model.Otp)
            {
                return new VerifyOtpResponseDto { IsSuccess = false, Message = "Invalid OTP!"};
            }

            var user = await _unitOfWork.Users.GetUserByEmail(model.Email);
            if(user == null)
            {
                return new VerifyOtpResponseDto { IsSuccess = false, Message = "User Not Found!"};
            }

            user.isActive = true;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.CommitAsync();

            _cache.Remove($"OTP_{model.Email}");

            return new VerifyOtpResponseDto { IsSuccess = true, Message = "Tài Khoản Đã Được Xác Minh Thành Công" };
            }
    }
}
