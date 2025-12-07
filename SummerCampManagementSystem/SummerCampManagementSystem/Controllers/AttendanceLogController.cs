using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.AttendanceLog;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceLogController : ControllerBase
    {

        private readonly IAttendanceLogService _attendanceLogService;
        private readonly IUserContextService _userContextService;
        public AttendanceLogController(IAttendanceLogService attendanceLogService, IUserContextService userContextService)
        {
            _attendanceLogService = attendanceLogService;
            _userContextService = userContextService;

        }

        // GET: api/<AttendanceLogController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _attendanceLogService.GetAllAttendanceLogsAsync();
            return Ok(result);
        }

        // GET api/<AttendanceLogController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _attendanceLogService.GetAttendanceLogByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        // POST api/<AttendanceLogController>
      

        /// <summary>
        /// attendance log for check-in and check-out activity
        /// </summary>

        /// <remarks>
        /// api "attendances/camps/{campId}" mà có activityType = check-in hoặc check-out thì gọi api này
        /// </remarks>

        ///<param name="status">Chọn checkin hoặc checkout thôi
        ///</param>

        [Authorize(Roles = "Staff")]
        [HttpPost]
        [Route("checkin_checkout-activity")]
        public async Task<IActionResult> CheckIn_CheckoutActivityLog([FromBody] AttendanceLogListRequestDto dto, [FromQuery] RegistrationCamperStatus status)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var staffId = _userContextService.GetCurrentUserId();
                var result = await _attendanceLogService.Checkin_CheckoutAttendanceAsync(dto, staffId.Value, status);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }


        /// <summary>
        /// Update status attendancelog
        /// </summary>

        /// <remarks>
        /// Dùng api getCampersByCoreActivity hoặc byOptional để lấy attendanceLogId
        /// </remarks>

       
        [Authorize(Roles = "Staff")]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] List<AttendanceLogUpdateRequest> updates)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var staffId = _userContextService.GetCurrentUserId()
                    ?? throw new UnauthorizedAccessException("User is not authenticated.");
                await _attendanceLogService.UpdateAttendanceLogAsync(updates, staffId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }


        [Authorize(Roles = "Staff")]
        [HttpPut("v2")]
        public async Task<IActionResult> Update([FromBody] AttendanceLogUpdateListRequest updates)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var staffId = _userContextService.GetCurrentUserId()
                    ?? throw new UnauthorizedAccessException("User is not authenticated.");
                await _attendanceLogService.UpdateAttendanceLogV2Async(updates, staffId);
                return Ok(new {message = "Điểm danh thành công"});
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpPost]
        [Route("create-logs-for-registrationClosed-camps")]
        public async Task<IActionResult> CreateAttendanceLogsForClosedCamps()
        {
            try
            {
                await _attendanceLogService.CreateAttendanceLogsForClosedCampsAsync();
                return Ok(new { message = "Attendance logs for registrationClosed camps created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("campers/{camperId}/attended-activities")]
        public async Task<IActionResult> GetAttendedActivitiesByCamperId (int camperId)
        {
            var attendedActivities = await _attendanceLogService.GetAttendedActivitiesByCamperId(camperId);
            return Ok(attendedActivities);
        }

        [HttpGet("activitySchedules/{activityScheduleId}/attended-campers")]
        public async Task<IActionResult> GetAttendedCampersByActivityScheduleId (int activityScheduleId)
        {
            var attendedCampers = await _attendanceLogService.GetAttendedCampersByActivityScheduleId(activityScheduleId);
            return Ok(attendedCampers);
        }
    }
}
