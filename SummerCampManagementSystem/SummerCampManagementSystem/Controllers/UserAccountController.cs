using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;
using SummerCampManagementSystem.BLL.Interfaces;
using System.Security.Claims;
using static SummerCampManagementSystem.BLL.DTOs.User.EmailDto;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/user")]
    [ApiController]
    [Authorize] 
    public class UserAccountController : ControllerBase
    {
        private readonly IUserAccountService _userAccountService;

        public UserAccountController(IUserAccountService userAccountService)
        {
            _userAccountService = userAccountService;
        }


        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            try
            {
                var userProfile = await _userAccountService.GetCurrentUserProfileAsync();
                return Ok(userProfile); 
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message); 
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving user profile.");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var updatedUser = await _userAccountService.UpdateUserProfileAsync(updateDto);
                return Ok(updatedUser); 
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error updating user profile: {ex.Message}");
            }
        }


        [HttpGet]
        // [Authorize(Roles = "Admin")] // Cần thêm Role check thực tế
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userAccountService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{userId}")]
        // [Authorize(Roles = "Admin, Staff")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            var user = await _userAccountService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }
            return Ok(user);
        }

        [HttpPatch("{userId}/admin-update")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserByAdmin(int userId, [FromBody] UserAdminUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var updatedUser = await _userAccountService.UpdateUserByAdminAsync(userId, updateDto);
                return Ok(updatedUser);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error updating user: {ex.Message}");
            }
        }

        [HttpDelete("{userId}")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var result = await _userAccountService.DeleteUserAsync(userId);
            if (!result)
            {
                return NotFound($"User with ID {userId} not found.");
            }
            return NoContent();
        }


        [HttpPost("email/initiate-update")]
        public async Task<IActionResult> InitiateEmailUpdate([FromBody] EmailUpdateRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var message = await _userAccountService.InitiateEmailUpdateAsync(model);
                return Ok(new { Message = message });
            }
            catch (UnauthorizedAccessException ex)
            {
                // password incorrect
                return Unauthorized(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                // Xnew email is used
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Đã xảy ra lỗi trong quá trình gửi OTP." });
            }
        }


        [HttpPost("email/verify-update")]
        public async Task<IActionResult> VerifyEmailUpdate([FromBody] EmailUpdateVerificationDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var updatedUser = await _userAccountService.VerifyEmailUpdateAsync(model);
                return Ok(updatedUser);
            }
            catch (InvalidOperationException ex)
            {
                // otp expired or error
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                // no user found
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Đã xảy ra lỗi trong quá trình cập nhật email." });
            }
        }


        [HttpPost("reset-password")] 
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Token không hợp lệ hoặc thiếu thông tin người dùng." });
            }

            try
            {
                var (isSuccess, message) = await _userAccountService.ChangePasswordAsync(userId, model);

                if (!isSuccess)
                {
                    return BadRequest(new { message = message });
                }

                return Ok(new { message = message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Đã xảy ra lỗi hệ thống khi đổi mật khẩu." });
            }
        }
    }
}
