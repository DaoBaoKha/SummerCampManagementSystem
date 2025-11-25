using Google.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.TransportSchedule;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;

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
                // conflict error or status validation error
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ khi cập nhật lịch trình.", detail = ex.Message });
            }
        }

        /// <summary>
        /// API to update status (NotYet, Rejected, Canceled)
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> UpdateScheduleStatus(int id, [FromBody] TransportScheduleStatusUpdateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // check if status is valid to update
            if (model.Status == TransportScheduleStatus.InProgress || model.Status == TransportScheduleStatus.Completed)
            {
                return BadRequest(new { message = $"Không thể chuyển thủ công sang trạng thái '{model.Status}'. Trạng thái này được xác định tự động." });
            }

            try
            {
                var response = await _scheduleService.UpdateScheduleStatusAsync(id, model.Status, model.CancelReasons);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // status flow validation error
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ khi cập nhật trạng thái lịch trình.", detail = ex.Message });
            }
        }


        /// <summary>
        /// API to update actual start time
        /// </summary>
        [HttpPatch("{id}/start-trip")]
        [Authorize(Roles = "Admin, Manager, Driver")]
        public async Task<IActionResult> StartTrip(int id)
        {
            try
            {
                var currentTime = TimeOnly.FromDateTime(DateTime.Now);

                var response = await _scheduleService.UpdateActualTimeAsync(id, currentTime, null);
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ khi ghi nhận giờ bắt đầu chuyến đi.", detail = ex.Message });
            }
        }

        /// <summary>
        /// API to update actual end time
        /// </summary>
        [HttpPatch("{id}/end-trip")]
        [Authorize(Roles = "Admin, Manager, Driver")]
        public async Task<IActionResult> EndTrip(int id)
        {
            try
            {
                var currentTime = TimeOnly.FromDateTime(DateTime.Now);

                var response = await _scheduleService.UpdateActualTimeAsync(id, null, currentTime);
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ khi ghi nhận giờ kết thúc chuyến đi.", detail = ex.Message });
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