using Google.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.TransportSchedule;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/transportschedules")]
    [ApiController]
    [Authorize(Roles = "Admin, Manager")] 
    public class TransportScheduleController : ControllerBase
    {
        private readonly ITransportScheduleService _scheduleService;

        public TransportScheduleController(ITransportScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSchedule([FromBody] TransportScheduleRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _scheduleService.CreateScheduleAsync(model);
                return CreatedAtAction(nameof(GetScheduleById), new { id = response.TransportScheduleId }, response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message }); 
            }
            catch (InvalidOperationException ex)
            {
                // conflict error
                return BadRequest(new { message = ex.Message }); 
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ khi tạo lịch trình.", detail = ex.Message });
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetScheduleById(int id)
        {
            try
            {
                var response = await _scheduleService.GetScheduleByIdAsync(id);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ khi lấy lịch trình.", detail = ex.Message });
            }
        }


        /// <summary>
        /// get list or search transport schedules
        /// </summary>
        /// <param name="searchDto"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransportScheduleResponseDto>>> Get([FromQuery] TransportScheduleSearchDto searchDto)
        {
            // check if any search criteria is provided
            if(searchDto.RouteId.HasValue || searchDto.DriverId.HasValue || searchDto.VehicleId.HasValue ||
               searchDto.Date.HasValue || searchDto.StartDate.HasValue || searchDto.EndDate.HasValue ||
               !string.IsNullOrEmpty(searchDto.Status))
            {
                var searchResults = await _scheduleService.SearchAsync(searchDto);
                return Ok(searchResults);
            }
            else
            {
                var allSchedules = await _scheduleService.GetAllSchedulesAsync();
                return Ok(allSchedules);
            }

        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] TransportScheduleRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _scheduleService.UpdateScheduleAsync(id, model);
                return Ok(response); 
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // conflict error
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ khi cập nhật lịch trình.", detail = ex.Message });
            }
        }


        [HttpPatch("{id}/actual-time")]
        [Authorize(Roles = "Admin, Manager, Driver")] 
        public async Task<IActionResult> UpdateActualTime(
            int id,
            [FromQuery] TimeOnly? startTime,
            [FromQuery] TimeOnly? endTime)
        {
            if (!startTime.HasValue && !endTime.HasValue)
            {
                return BadRequest(new { message = "Phải cung cấp ít nhất startTime hoặc endTime." });
            }

            try
            {
                var response = await _scheduleService.UpdateActualTimeAsync(id, startTime, endTime);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // status update error
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ khi cập nhật thời gian thực tế.", detail = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            try
            {
                await _scheduleService.DeleteScheduleAsync(id);
                return NoContent(); 
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // status update error
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ khi xóa lịch trình.", detail = ex.Message });
            }
        }
    }
}