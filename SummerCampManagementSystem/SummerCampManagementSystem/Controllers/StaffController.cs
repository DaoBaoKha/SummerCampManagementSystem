using Google.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly IAccommodationService _accommodationService;
        private readonly IActivityScheduleService _activityScheduleService;
        private readonly ICamperGroupService _camperGroupService;
        private readonly ICampService _campService;
        private readonly IUserContextService _userContextService;

        public StaffController(
            IAccommodationService accommodationService,
            IActivityScheduleService activityScheduleService,
            ICamperGroupService camperGroupService,
            IUserContextService userContextService,
            ICampService campService)
        {
            _accommodationService = accommodationService;
            _activityScheduleService = activityScheduleService;
            _camperGroupService = camperGroupService;
            _userContextService = userContextService;
            _campService = campService;
        }


        [Authorize(Roles = "Staff, Manager")]
        [HttpGet("camps/{campId}/activities")]
        public async Task<IActionResult> GetAllByStaffId(int campId)
        {
            try
            {
                var staffId = _userContextService.GetCurrentUserId();
                var result = await _activityScheduleService.GetAllSchedulesByStaffIdAsync(staffId.Value, campId);
                return Ok(result);
            }

            catch (KeyNotFoundException knfEx)
            {
                return NotFound(new { message = knfEx.Message });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }


        [Authorize(Roles = "Staff, Manager")]
        [HttpGet("camps/{campId}/group")]
        public async Task<IActionResult> GetAllGroupsBySupervisorId(int campId)
        {
            try
        {
                var staffId = _userContextService.GetCurrentUserId();
                var camperGroups = await _camperGroupService.GetGroupBySupervisorIdAsync(staffId.Value, campId);
            return Ok(camperGroups);
        }
            catch (KeyNotFoundException knfEx)
            {
                return NotFound(new { message = knfEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "Staff, Manager")]
        [HttpGet("camps/{campId}/accomodation")]
        public async Task<IActionResult> GetAllBySupervisorIdAsync(int campId)
        {
            try
            {
                var staffId = _userContextService.GetCurrentUserId();
                var accommodations = await _accommodationService.GetBySupervisorIdAsync(staffId.Value, campId);
            return Ok(accommodations);
        }
            catch (KeyNotFoundException knfEx)
            {
                return NotFound(new { message = knfEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "Staff, Manager")]
        [HttpGet("my-camps")]
        public async Task<IActionResult> GetMyCamps()
        {
            var staffId = _userContextService.GetCurrentUserId();
            if (staffId == null)
                return Unauthorized();

            var result = await _campService.GetCampsByStaffIdAsync(staffId.Value);
            return Ok(result);
        }
    }
}
