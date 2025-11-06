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

        [HttpGet("camp/{campId}/staff/{staffId}")]
        public async Task<IActionResult> GetByCampAndStaff(int campId, int staffId, [FromQuery] ActivityScheduleType? status)
        {
            try
            {
                var result = await _service.GetByCampAndStaffAsync(campId, staffId, status);
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

        [HttpGet("camper/{camperId}/camp/{campId}")]
        public async Task<IActionResult> GetByCamperAndCamp(int camperId, int campId)
        {
            try
            {
                var result = await _service.GetSchedulesByCamperAndCampAsync(camperId, campId);
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

        [Authorize(Roles = "Staff")]
        [HttpGet("my-schedules")]
        public async Task<IActionResult> GetAllByStaffId()
        {
            try
            {
                var staffId = _userContextService.GetCurrentUserId();
                var result = await _service.GetAllSchedulesByStaffIdAsync(staffId.Value);
                return Ok(result);
            }
           
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }
    }
}
