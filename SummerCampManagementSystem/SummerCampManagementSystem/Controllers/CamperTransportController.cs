using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.CamperTransport;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/camper-transport")]
    [ApiController]
    [Authorize] 
    public class CamperTransportController : ControllerBase
    {
        private readonly ICamperTransportService _camperTransportService;

        public CamperTransportController(ICamperTransportService camperTransportService)
        {
            _camperTransportService = camperTransportService;
        }

        [HttpGet("schedule/{transportScheduleId}")]
        [Authorize(Roles = "Driver, Staff, Manager, Admin")]
        public async Task<IActionResult> GetCampersByScheduleId(int transportScheduleId, [FromQuery] int? camperId = null)
        {
            var campers = await _camperTransportService.GetCampersByScheduleIdAsync(transportScheduleId, camperId);
            return Ok(campers);
        }

        /// <summary>
        /// Get active camper transports
        /// </summary>
        [HttpGet("schedule/{transportScheduleId}/active")]
        [Authorize(Roles = "Driver, Staff, Manager, Admin")]
        public async Task<IActionResult> GetActiveCampersByScheduleId(int transportScheduleId)
        {
            var campers = await _camperTransportService.GetActiveCamperTransportsByScheduleIdAsync(transportScheduleId);
            return Ok(campers);
        }

        [HttpGet]
        [Authorize(Roles = "Driver, Staff, Manager, Admin")]
        public async Task<IActionResult> GetAllCamperTransports()
        {
            var campers = await _camperTransportService.GetAllCamperTransportAsync();
            return Ok(campers);
        }

        /// <summary>
        /// Auto generate camperTransport list from transportSchedule
        /// </summary>
        [HttpPost("schedule/{transportScheduleId}/generate")]
        [Authorize(Roles = "Admin, Manager")] 
        public async Task<IActionResult> GenerateCamperList(int transportScheduleId)
        {
            var result = await _camperTransportService.GenerateCamperListForScheduleAsync(transportScheduleId);

            if (result)
            {
                return Ok(new { message = "Đã tạo danh sách đưa đón thành công." });
            }
            else
            {
                return Ok(new { message = "Không tìm thấy đơn đăng ký mới nào đủ điều kiện (Đã thanh toán) để thêm vào danh sách." });
            }
        }

        /// <summary>
        /// Update CamperTransport Status
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Driver, Staff, Manager, Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] CamperTransportUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedCamperTransport = await _camperTransportService.UpdateStatusAsync(id, updateDto);
            return Ok(updatedCamperTransport);
        }

        /// <summary>
        /// Check-in Camper
        /// </summary>
        [HttpPatch("check-in")]
        [Authorize(Roles = "Driver, Staff, Manager, Admin")]
        public async Task<IActionResult> CheckIn([FromBody] CamperTransportAttendanceDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _camperTransportService.CamperCheckInAsync(request);
            return Ok(new { message = "Check-in thành công (Đã lên xe)." });
        }

        /// <summary>
        /// Check-out Camper
        /// </summary>
        [HttpPatch("check-out")]
        [Authorize(Roles = "Driver, Staff, Manager, Admin")]
        public async Task<IActionResult> CheckOut([FromBody] CamperTransportAttendanceDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Middleware handles exceptions (NotFound, BusinessRule, 500)
            await _camperTransportService.CamperCheckOutAsync(request);
            return Ok(new { message = "Check-out thành công (Đã xuống xe)." });
        }

        /// <summary>
        /// Mark Camper Absence
        /// </summary>
        [HttpPatch("absent")]
        [Authorize(Roles = "Driver, Staff, Manager, Admin")]
        public async Task<IActionResult> MarkAbsent([FromBody] CamperTransportAttendanceDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _camperTransportService.CamperMarkAbsentAsync(request);
            return Ok(new { message = "Đã đánh dấu vắng mặt." });
        }
    }
}