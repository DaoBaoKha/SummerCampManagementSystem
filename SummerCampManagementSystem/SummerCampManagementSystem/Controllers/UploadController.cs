using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Photo;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.Core.Enums; 
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/upload")]
    [ApiController]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IUploadSupabaseService _uploadService;
        private readonly IUserContextService _userContextService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDriverService _driverService;
        private readonly IStaffService _staffService;
        private readonly IUserAccountService _userAccountService;

        public UploadController(IUploadSupabaseService uploadService,
                                IUserContextService userContextService,
                                IUnitOfWork unitOfWork,
                                IDriverService driverService,
                                IStaffService staffService,
                                IUserAccountService userAccountService)
        {
            _uploadService = uploadService;
            _userContextService = userContextService;
            _unitOfWork = unitOfWork;
            _driverService = driverService;
            _staffService = staffService;
            _userAccountService = userAccountService;
        }

        /// <summary>
        /// API to upload avatar for the current user with Smart Routing based on Role
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPut("my-avatar")]
        public async Task<IActionResult> UploadMyAvatar(IFormFile file)
        {
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "User not found." });

            try
            {
                // take user from DB to get Role
                var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
                if (user == null) return NotFound(new { message = "User not found in database." });

                string url;

                // smart routing based on role
                if (string.Equals(user.role, UserRole.Driver.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    // if driver -> save to 'driver-avatars' bucket
                    url = await _driverService.UpdateDriverAvatarAsync(userId.Value, file);
                }
                else if (string.Equals(user.role, UserRole.Staff.ToString(), StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(user.role, UserRole.Manager.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    // if staff or manager -> save to 'staff-avatars' bucket
                    url = await _staffService.UpdateStaffAvatarAsync(userId.Value, file);
                }
                else
                {
                    // if normal user -> save to 'user-avatars' bucket
                    url = await _userAccountService.UpdateUserAvatarAsync(userId.Value, file);
                }

                return Ok(new UploadPhotoDto { Url = url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }



        [HttpPut("admin/staff/{userId}/avatar")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> UploadStaffAvatarByAdmin(int userId, IFormFile file)
        {
            try
            {
                var url = await _staffService.UpdateStaffAvatarAsync(userId, file);
                return Ok(new UploadPhotoDto { Url = url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("admin/driver/{userId}/avatar")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> UploadDriverAvatarByAdmin(int userId, IFormFile file)
        {
            try
            {
                var url = await _driverService.UpdateDriverAvatarAsync(userId, file);
                return Ok(new UploadPhotoDto { Url = url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}