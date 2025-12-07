using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.CamperTransport;
using SummerCampManagementSystem.BLL.DTOs.TransportSchedule;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/transport-schedules")]
    [ApiController]
    public class TransportScheduleController : ControllerBase
    {
        private readonly ITransportScheduleService _scheduleService;

        public TransportScheduleController(ITransportScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> CreateSchedule([FromBody] TransportScheduleRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _scheduleService.CreateScheduleAsync(model);

            return CreatedAtAction(nameof(GetScheduleById), new { id = response.TransportScheduleId }, response);
        }


        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetScheduleById(int id)
        {
            var response = await _scheduleService.GetScheduleByIdAsync(id);
            return Ok(response);
        }

        [HttpGet("driver-schedule")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> GetDriverScheduleAsync()
        {
            var response = await _scheduleService.GetDriverSchedulesAsync();
            return Ok(response);
        }

        /// <summary>
        /// get list camper in one transport schedule
        /// </summary>
        [HttpGet("{id}/campers")]
        [Authorize(Roles = "Admin, Manager, Driver")] 
        public async Task<ActionResult<IEnumerable<CamperInScheduleResponseDto>>> GetCampersInSchedule(int id)
        {
            var response = await _scheduleService.GetCampersInScheduleAsync(id);
            return Ok(response);
        }

        /// <summary>
        /// get camper transport schedule
        /// </summary>
        [HttpGet("camper/{camperId}")]
        [Authorize(Roles = "Admin, Manager, Parent")] 
        public async Task<ActionResult<IEnumerable<TransportScheduleResponseDto>>> GetSchedulesByCamperId(int camperId)
        {
            var response = await _scheduleService.GetSchedulesByCamperIdAsync(camperId);
            return Ok(response);
        }


        /// <summary>
        /// get list or search transport schedules
        /// </summary>
        /// <param name="searchDto"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "Admin, Manager, User")]
        public async Task<ActionResult<IEnumerable<TransportScheduleResponseDto>>> Get([FromQuery] TransportScheduleSearchDto searchDto)
        {
            // check if any search criteria is provided
            if (searchDto.CampId.HasValue || searchDto.RouteId.HasValue || searchDto.DriverId.HasValue || searchDto.VehicleId.HasValue ||
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
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] TransportScheduleRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Middleware sẽ bắt NotFoundException (404), BusinessRuleException (400), và Exception (500)
            var response = await _scheduleService.UpdateScheduleAsync(id, model);
            return Ok(response);
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

            var response = await _scheduleService.UpdateScheduleStatusAsync(id, model.Status, model.CancelReasons);
            return Ok(response);
        }


        /// <summary>
        /// API to update actual start time
        /// </summary>
        [HttpPatch("{id}/start-trip")]
        [Authorize(Roles = "Admin, Manager, Driver")]
        public async Task<IActionResult> StartTrip(int id)
        {
            var currentTime = TimeOnly.FromDateTime(DateTime.Now);

            var response = await _scheduleService.UpdateActualTimeAsync(id, currentTime, null);
            return Ok(response);
        }

        /// <summary>
        /// API to update actual end time
        /// </summary>
        [HttpPatch("{id}/end-trip")]
        [Authorize(Roles = "Admin, Manager, Driver")]
        public async Task<IActionResult> EndTrip(int id)
        {
            var currentTime = TimeOnly.FromDateTime(DateTime.Now);

            var response = await _scheduleService.UpdateActualTimeAsync(id, null, currentTime);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            await _scheduleService.DeleteScheduleAsync(id);
            return NoContent();
        }
    }
}