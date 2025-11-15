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
        private readonly IStaffService _staffService;
        private readonly IUserContextService _userContextService;

        public StaffController(
            IAccommodationService accommodationService,
            IActivityScheduleService activityScheduleService,
            ICamperGroupService camperGroupService,
            IUserContextService userContextService,
            IStaffService staffService,
            ICampService campService)
        {
            _accommodationService = accommodationService;
            _activityScheduleService = activityScheduleService;
            _camperGroupService = camperGroupService;
            _userContextService = userContextService;
            _campService = campService;
            _staffService = staffService;
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

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet("camps/{campId}/available-activity-staff/{activityScheduleId}")]
        public async Task<IActionResult> GetAvailableActivityStaff(int campId, int activityScheduleId)
        {
            try
            {
                var result = await _staffService.GetAvailableActivityStaffs(campId, activityScheduleId);
                return Ok(result);
            }
            catch (KeyNotFoundException knfEx)
            {
                return NotFound(new { message = knfEx.Message });
            }
            catch (ArgumentException arEx)
            {
                return BadRequest(new { message = arEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet("camps/{campId}/available-group-staff")]
        public async Task<IActionResult> GetAvailableGroupStaff(int campId)
        {
            try
            {
                var result = await _staffService.GetAvailableGroupStaffs(campId);
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

        [Authorize(Roles = "Admin, Manager")]
        [HttpGet("camps/{campId}/available-accomodation-staff")]
        public async Task<IActionResult> GetAvailableAccomodationStaff(int campId)
        {
            try
            {
                var result = await _staffService.GetAvailableAccomodationStaffs(campId);
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
    }
}
