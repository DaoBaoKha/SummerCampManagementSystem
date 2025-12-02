using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.CamperTransport;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/campertransport")]
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
        public async Task<IActionResult> GetCampersByScheduleId(int transportScheduleId)
        {
            try
            {
                var campers = await _camperTransportService.GetCampersByScheduleIdAsync(transportScheduleId);
                return Ok(campers);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống.", detail = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Driver, Staff, Manager, Admin")]
        public async Task<IActionResult> GetAllCamperTransports()
        {
            try
            {
                var campers = await _camperTransportService.GetAllCamperTransportAsync();
                return Ok(campers);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Auto generate camperTransport list from transportSchedule
        /// </summary>
        [HttpPost("schedule/{transportScheduleId}/generate")]
        [Authorize(Roles = "Admin, Manager")] 
        public async Task<IActionResult> GenerateCamperList(int transportScheduleId)
        {
            try
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống.", detail = ex.Message });
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

            try
            {
                var updatedCamperTransport = await _camperTransportService.UpdateStatusAsync(id, updateDto);
                return Ok(updatedCamperTransport);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Check-in Camper
        /// </summary>
        [HttpPatch("check-in")]
        [Authorize(Roles = "Driver, Staff, Manager, Admin")]
        public async Task<IActionResult> CheckIn([FromBody] CamperTransportAttendanceDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _camperTransportService.CamperCheckInAsync(request);
                return Ok(new { message = "Check-in thành công (Đã lên xe)." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // flow error (havent assigned)
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Check-out Camper
        /// </summary>
        [HttpPatch("check-out")]
        [Authorize(Roles = "Driver, Staff, Manager, Admin")]
        public async Task<IActionResult> CheckOut([FromBody] CamperTransportAttendanceDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _camperTransportService.CamperCheckOutAsync(request);
                return Ok(new { message = "Check-out thành công (Đã xuống xe)." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // flow error (havent checkIn)
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Mark Camper Absence
        /// </summary>
        [HttpPatch("absent")]
        [Authorize(Roles = "Driver, Staff, Manager, Admin")]
        public async Task<IActionResult> MarkAbsent([FromBody] CamperTransportAttendanceDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _camperTransportService.CamperMarkAbsentAsync(request);
                return Ok(new { message = "Đã đánh dấu vắng mặt." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống.", detail = ex.Message });
            }
        }
    }
}