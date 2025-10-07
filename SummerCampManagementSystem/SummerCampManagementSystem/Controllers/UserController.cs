using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Requests.User;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using System.Security.Claims;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (authResponse, errorMessage) = await _userService.LoginAsync(model);

            if (errorMessage != null)
                return Unauthorized(new { message = errorMessage });

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
        public async Task<IActionResult> Register(RegisterUserRequestDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var registerResponse = await _userService.RegisterAsync(model);

            if (registerResponse == null)
                return BadRequest(new { message = "Đăng ký không thành công. Email này đã được sử dụng!" });

            return Ok(registerResponse);
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
    }
}
