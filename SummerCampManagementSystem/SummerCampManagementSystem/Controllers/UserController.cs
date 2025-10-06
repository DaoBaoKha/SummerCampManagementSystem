using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SummerCampManagementSystem.BLL.DTOs.Requests.User;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService  _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (authResponse, errorMessage) = await _userService.LoginAsync(model);

            if (errorMessage != null)
            {
                return Unauthorized(new { message = errorMessage });
            }

            return Ok(authResponse);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User is not authenticated" });

            try
            {
                if (int.TryParse(userId, out var userIdInt))
                {
                    await _userService.LogoutAsync(userIdInt);
                    return Ok(new { message = "Logged out successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Invalid user ID format" });
                }
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while logging out" });
            }
        }
    }
}
