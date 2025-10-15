using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Requests.Camp;
using SummerCampManagementSystem.BLL.DTOs.Responses.Camp;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/camp")]
    [ApiController]
    public class CampController : ControllerBase
    {
        private readonly ICampService _campService;

        public CampController(ICampService campService)
        {
            _campService = campService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCamps()
        {
            var camps = await _campService.GetAllCampsAsync();
            return Ok(camps);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCampById(int id)
        {
            var camp = await _campService.GetCampByIdAsync(id);
            if (camp == null)
            {
                return NotFound(new { message = $"Camp with ID {id} not found." });
            }
            return Ok(camp);
        }

        [HttpGet("status")]
        public async Task<ActionResult<IEnumerable<CampResponseDto>>> GetCampsByStatus([FromQuery] CampStatus? status) 
        {
            try
            {
                var camps = await _campService.GetCampsByStatusAsync(status);
                return Ok(camps);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while filtering camps by status." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCamp(int id)
        {
            var result = await _campService.DeleteCampAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"Camp with ID {id} not found." });
            }
            return NoContent(); 
        }

        [HttpPost]
        public async Task<IActionResult> CreateCamp([FromBody] CampRequestDto camp)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var createdCamp = await _campService.CreateCampAsync(camp);
                return CreatedAtAction(nameof(GetCampById), new { id = createdCamp.CampId }, createdCamp);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred during camp creation." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCamp(int id, [FromBody] CampRequestDto camp)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var updatedCamp = await _campService.UpdateCampAsync(id, camp);
                return Ok(updatedCamp);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) when (ex.Message.Contains("Camp not found"))
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while updating the camp." });
            }
        }

        [HttpPatch("{campId}/status")]
        public async Task<ActionResult<CampResponseDto>> UpdateCampStatus(int campId,[FromBody] CampStatusUpdateRequestDto statusUpdate)
        {
            try
            {
                if (statusUpdate.Status.ToString() == "0") // check valid enum 
                {
                    return BadRequest("Invalid or missing status value.");
                }

                var updatedCamp = await _campService.UpdateCampStatusAsync(campId, statusUpdate);

                return Ok(updatedCamp);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) when (ex.Message.Contains("Camp not found"))
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating camp status." });
            }
        }

    }
}
