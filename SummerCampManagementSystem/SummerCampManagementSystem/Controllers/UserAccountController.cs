using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;
using SummerCampManagementSystem.BLL.Interfaces;

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


        [HttpGet("user")]
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

        [HttpPatch("user")]
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
    }
}
