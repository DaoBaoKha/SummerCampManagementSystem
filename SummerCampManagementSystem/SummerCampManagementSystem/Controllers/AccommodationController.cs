using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Accommodation;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccommodationController : ControllerBase
    {
        private readonly IAccommodationService _accommodationService;
        private readonly IUserContextService _userContextService;   
        public AccommodationController(IAccommodationService accommodationService, IUserContextService userContextService)
        {
            _accommodationService = accommodationService;
            _userContextService = userContextService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCommodations()
        {
            var result = await _accommodationService.GetAllAccommodationsAsync();
            return Ok(result);
        }

        [HttpGet("{accommodationId}")]
        public async Task<IActionResult> GetAccommodationById(int accommodationId)
        {
            try
            {
                var accommodation = await _accommodationService.GetAccommodationByIdAsync(accommodationId);
                return Ok(accommodation);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ: " + ex.Message });
            }
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetAllActiveAccommodations()
        {
            try
            {
                var accommodations = await _accommodationService.GetActiveAccommodationsAsync();
                return Ok(accommodations);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ: " + ex.Message });
            }
        }

        [HttpGet("camp/{campId}")]
        public async Task<IActionResult> GetAccommodationsByCampId(int campId)
        {
            try
            {
                var accommodations = await _accommodationService.GetAccommodationsByCampIdAsync(campId);
                return Ok(accommodations);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ: " + ex.Message });
            }
        }

        [HttpGet("supervisor/{supervisorId}")]
        public async Task<IActionResult> GetAccommodationsBySupervisorId(int supervisorId, int campId)
        {
            try
            {
                var accommodations = await _accommodationService.GetBySupervisorIdAsync(supervisorId, campId);
                return Ok(accommodations);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> CreateAccommodation([FromBody] AccommodationRequestDto accommodationRequestDto)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                                   ?? "Dữ liệu yêu cầu không hợp lệ.";
                return BadRequest(new { message = errorMessage });
            }

            try
            {
                var result = await _accommodationService.CreateAccommodationAsync(accommodationRequestDto);
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

        [HttpPut("{accommodationId}")]
        public async Task<IActionResult> UpdateAccommodation(int accommodationId, [FromBody] AccommodationRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                                   ?? "Dữ liệu yêu cầu không hợp lệ.";
                return BadRequest(new { message = errorMessage });
            }

            try
            {
                var updatedAccommodation = await _accommodationService.UpdateAccommodationAsync(accommodationId, requestDto);
                return Ok(updatedAccommodation);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ: " + ex.Message });
            }
        }


        [HttpPatch("{accommodationId}/status")]
        public async Task<IActionResult> UpdateAccommodationStatus(int accommodationId, [FromQuery] bool isActive)
        {
            try
            {
                var updatedAccommodation = await _accommodationService.UpdateAccommodationStatusAsync(accommodationId, isActive);
                return Ok(updatedAccommodation);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ: " + ex.Message });
            }

        }

        [HttpDelete("{accommodationId}")]
        public async Task<IActionResult> DeleteAccommodation(int accommodationId)
        {
            try
            {
                var result = await _accommodationService.DeleteAccommodationAsync(accommodationId);
                return Ok(new { message = "Xóa chỗ ở thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ: " + ex.Message });
            }
        }
    }
}
