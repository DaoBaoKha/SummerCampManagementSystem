using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.User;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (authResponse, errorMessage, errorCode) = await _userService.LoginAsync(model);

            if (errorCode == "ACCOUNT_NOT_ACTIVE")
                return BadRequest(new { message = errorMessage, code = errorCode });

            if (errorMessage != null)
                return Unauthorized(new { message = errorMessage, code = errorCode });

            return Ok(authResponse);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User is not authenticated" });

            if (!int.TryParse(userId, out var userIdInt))
                return BadRequest(new { message = "Invalid user ID format" });

            try
            {
                await _userService.LogoutAsync(userIdInt);
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while logging out" });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequestDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var registerResponse = await _userService.RegisterAsync(model);

            if (registerResponse == null)
                return BadRequest(new { message = "Đăng ký không thành công. Email này đã được sử dụng!" });

            return Ok(registerResponse);
        }

        [HttpPost("create-staff")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateStaff([FromBody] RegisterStaffRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
          
            var result = await _userService.CreateStaffAccountAsync(dto);

            if (result == null)
                return BadRequest(new { message = "Tạo tài khoản nhân viên không thành công. Email này đã được sử dụng!" });

            return Ok(result);
            
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpRequestDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var verifyResponse = await _userService.VerifyOtpAsync(model);

            if (verifyResponse == null)
                return BadRequest(new { message = "Gửi mã OTP không thành công!" });

            return Ok(verifyResponse);
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendActivationOtp(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email không được để trống." });

            var response = await _userService.ResendActivationOtpAsync(email);

            if (!response.IsSuccess)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Email is required" });
            var forgotPasswordResponse = await _userService.ForgotPasswordAsync(email);
            if (forgotPasswordResponse == null)
                return BadRequest(new { message = "Yêu cầu quên mật khẩu không thành công!" });
            return Ok(forgotPasswordResponse);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var resetResponse = await _userService.ResetPasswordAsync(model);
            if (resetResponse == null)
                return BadRequest(new { message = "Đặt lại mật khẩu không thành công!" });
            return Ok(resetResponse);
        }
    }
}
