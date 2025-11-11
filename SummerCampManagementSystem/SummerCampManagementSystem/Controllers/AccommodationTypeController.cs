using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.AccommodationType;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccommodationTypeController : ControllerBase
    {
        private readonly IAccommodationTypeService _accommodationTypeService;

        public AccommodationTypeController(IAccommodationTypeService accommodationTypeService)
        {
            _accommodationTypeService = accommodationTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAccommodationTypes()
        {
            var accommodationTypes = await _accommodationTypeService.GetAllAsync();
            return Ok(accommodationTypes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccommodationTypeById(int id)
        {
            try
            {
                var accommodationType = await _accommodationTypeService.GetByIdAsync(id);
                return Ok(accommodationType);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccommodationType([FromBody] AccommodationTypeRequestDto accommodationTypeRequestDto)
        {
            try
            {
                var createdAccommodationType = await _accommodationTypeService.CreateAsync(accommodationTypeRequestDto);
                return CreatedAtAction(nameof(GetAccommodationTypeById), new { id = createdAccommodationType.Id }, createdAccommodationType);
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccommodationType(int id, [FromBody] AccommodationTypeRequestDto accommodationTypeRequestDto)
        {
            try
            {
                var updatedAccommodationType = await _accommodationTypeService.UpdateAsync(id, accommodationTypeRequestDto);
                return Ok(updatedAccommodationType);
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
