using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.ActivitySchedule;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivityScheduleController : ControllerBase
    {
        private readonly IActivityScheduleService _service;
        private readonly IUserContextService _userContextService;
        public ActivityScheduleController(IActivityScheduleService service, IUserContextService userContextService)
        {
            _service = service;
            _userContextService = userContextService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllSchedulesAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetScheduleByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpPost("core")]
        public async Task<IActionResult> CreateCore([FromBody] ActivityScheduleCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _service.CreateCoreScheduleAsync(dto);
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

        [HttpPost("optional/{coreScheduleId}")]
        public async Task<IActionResult> CreateOptional(int coreScheduleId, [FromBody] OptionalScheduleCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _service.CreateOptionalScheduleAsync(dto, coreScheduleId);
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
        /// Get activity schedules with status "PendingAttendance" by campid and staffid
        /// </summary>

        /// <remarks>
        /// cái get này sẽ get những optional activity schedule mà staff đó đc phân hoặc core activity schedule của group mà có quản lý
        /// </remarks>

        [Authorize(Roles = "Staff")]
        [HttpGet("attendances/camps/{campId}")]
        public async Task<IActionResult> GetByCampAndStaff(int campId)
        {
            try
            {
                var staffId = _userContextService.GetCurrentUserId()
                    ?? throw new UnauthorizedAccessException("User is not authenticated.");
                var result = await _service.GetByCampAndStaffAsync(campId, staffId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("camp/{campId}/camper/{camperId}")]
        public async Task<IActionResult> GetByCamperAndCamp(int campId, int camperId)
        {
            try
            {
                var result = await _service.GetSchedulesByCamperAndCampAsync(campId, camperId);
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

        [HttpGet("optional/camp/{campId}")]
        public async Task<IActionResult> GetOptionalByCamp(int campId)
        {
            try
            {
                var result = await _service.GetOptionalSchedulesByCampAsync(campId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("core/camp/{campId}")]
        public async Task<IActionResult> GetCoreByCamp(int campId)
        {
            try
            {
                var result = await _service.GetCoreSchedulesByCampAsync(campId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("camp/{campId}")]
        public async Task<IActionResult> GetByCamp(int campId)
        {
            try
            {
                var result = await _service.GetSchedulesByCampAsync(campId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("date-range")]
        public async Task<IActionResult> GetByDateRange([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            try
            {
                var result = await _service.GetSchedulesByDateAsync(fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpPut("core/{id}")]
        public async Task<IActionResult> UpdateCoreSchedule(int id, [FromBody] ActivityScheduleCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _service.UpdateCoreScheduleAsync(id, dto);
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

        [HttpPut("change-status-auto")]
        public async Task<IActionResult> ChangeActivityScheduleStatusAuto()
        {
            try
            {
                await _service.ChangeActivityScheduleStatusAuto();
                return Ok(new {message = "Update Status Successfully !!!"});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpPut("{activityScheduleId}/status")]
        public async Task<IActionResult> ChangeStatus(int activityScheduleId, [FromQuery] ActivityScheduleStatus status)
        {
            try
            {
                var updatedActivity = await _service.ChangeStatusActivitySchedule(activityScheduleId, status);
                return Ok(updatedActivity);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{activityScheduleId}/liveStreamStatus")]
        public async Task<IActionResult> ChangeLiveStreamStatus(int activityScheduleId, bool isLiveStream)
        {
            var updated = await _service.UpdateLiveStreamStatus(activityScheduleId, isLiveStream);
            return Ok(updated);
        }

        [HttpDelete("{activityScheduleId}")]
        public async Task<IActionResult> DeleteActivitySchedule(int activityScheduleId)
        {
            var result = await _service.DeleteActivityScheduleAsync(activityScheduleId);
            return result ? NoContent() : NotFound();
        }
    }
}
