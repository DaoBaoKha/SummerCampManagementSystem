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

        [Authorize(Roles = "Staff")]
        [HttpPost("core-activity/list-attendance")]
        public async Task<IActionResult> CoreActivityAttendance([FromBody] AttendanceLogListRequestDto dto)
        {
            var staffId = _userContextService.GetCurrentUserId();
            var result = await _attendanceLogService.CoreActivityAttendanceAsync(dto, staffId.Value, true);
            return Ok(result);
        }


        // POST api/<AttendanceLogController>
        [HttpPost]
        [Route("core-activity")]

        public async Task<IActionResult> Create([FromBody] AttendanceLogRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _attendanceLogService.CoreActivityAttendanceAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.AttendanceLogId }, result);
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
        [Route("optional-activity")]
        public async Task<IActionResult> CreateOptionalActivityLog([FromBody] AttendanceLogRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _attendanceLogService.OptionalActivityAttendanceAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.AttendanceLogId }, result);
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

        [HttpPost]
        [Route("resting-activity")]
        public async Task<IActionResult> RestingActivityLog([FromBody] AttendanceLogRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _attendanceLogService.RestingAttendanceAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.AttendanceLogId }, result);
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


    }
}
