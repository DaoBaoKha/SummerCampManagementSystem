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
        private readonly IUserContextService _userContextService;

        public StaffController(
            IAccommodationService accommodationService,
            IActivityScheduleService activityScheduleService,
            ICamperGroupService camperGroupService,
            IUserContextService userContextService)
        {
            _accommodationService = accommodationService;
            _activityScheduleService = activityScheduleService;
            _camperGroupService = camperGroupService;
            _userContextService = userContextService;
        }


        [Authorize(Roles = "Staff")]
        [HttpGet("my-activities")]
        public async Task<IActionResult> GetAllByStaffId()
        {
            try
            {
                var staffId = _userContextService.GetCurrentUserId();
                var result = await _activityScheduleService.GetAllSchedulesByStaffIdAsync(staffId.Value);
                return Ok(result);
            }

            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }


        [Authorize(Roles = "Staff")]
        [HttpGet("my-groups")]
        public async Task<IActionResult> GetAllGroupsBySupervisorId()
        {
            var staffid = _userContextService.GetCurrentUserId();
            var camperGroups = await _camperGroupService.GetAllGroupsBySupervisorIdAsync(staffid.Value);
            return Ok(camperGroups);
        }

        [Authorize(Roles = "Staff")]
        [HttpGet("my-accomodations")]
        public async Task<IActionResult> GetAllBySupervisorIdAsync()
        {
            var supervisorId = _userContextService.GetCurrentUserId();
            var accommodations = await _accommodationService.GetAllBySupervisorIdAsync(supervisorId.Value);
            return Ok(accommodations);
        }
    }
}
