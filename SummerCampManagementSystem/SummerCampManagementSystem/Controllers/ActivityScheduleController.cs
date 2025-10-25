using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.ActivitySchedule;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivityScheduleController : ControllerBase
    {
        private readonly IActivityScheduleService _service;
        public ActivityScheduleController(IActivityScheduleService service)
        {
            _service = service;
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
    }
}
