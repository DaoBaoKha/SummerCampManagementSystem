using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.CamperAccommodation;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/camper-accommodation")]
    [ApiController]
    public class CamperAccommodationController : ControllerBase
    {
        private readonly ICamperAccommodationService _camperAccommodationService;

        public CamperAccommodationController(ICamperAccommodationService camperAccommodationService)
        {
            _camperAccommodationService = camperAccommodationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCamperAccommodations([FromQuery] CamperAccommodationSearchDto searchDto)
        {
            var result = await _camperAccommodationService.GetCamperAccommodationsAsync(searchDto);
            return Ok(result);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingAssignCampers([FromQuery] int? campId)
        {
            var result = await _camperAccommodationService.GetPendingAssignCampersAsync(campId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCamperAccommodation([FromBody] CamperAccommodationRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _camperAccommodationService.CreateCamperAccommodationAsync(requestDto);
            return CreatedAtAction(nameof(GetCamperAccommodations), new { camperAccommodationId = result.camperAccommodationId }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCamperAccommodation(int id, [FromBody] CamperAccommodationRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _camperAccommodationService.UpdateCamperAccommodationAsync(id, requestDto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCamperAccommodation(int id)
        {
            var isDeleted = await _camperAccommodationService.DeleteCamperAccommodationAsync(id);

            if (!isDeleted)
            {
                return NotFound(new { message = "Không tìm thấy thông tin phân chỗ ở để xóa." });
            }

            return NoContent();
        }
    }
}
