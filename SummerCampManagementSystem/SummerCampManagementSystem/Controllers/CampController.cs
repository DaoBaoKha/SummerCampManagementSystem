using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Camp;
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
            try
            {
                var camp = await _campService.GetCampByIdAsync(id);
                return Ok(camp);
            }
            catch (Exception ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving the camp." });
            }
        }

        [HttpGet("status")]
        public async Task<ActionResult<IEnumerable<CampResponseDto>>> GetCampsByStatus([FromQuery] CampStatus? status)
        {
            try
            {
                var camps = await _campService.GetCampsByStatusAsync(status);
                return Ok(camps);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while filtering camps by status." });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
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
        [Authorize]
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
            catch (UnauthorizedAccessException ex) // authorization error
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) 
            {
                // detailed error for unexpected exceptions
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred during camp creation.", detail = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
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

        [HttpPut("{campId}/approve")]
        [Authorize]
        // [Authorize(Roles = "Manager")] 
        public async Task<IActionResult> ApproveCamp(int campId)
        {
            try
            {
                // approve camp
                var approvedCamp = await _campService.TransitionCampStatusAsync(campId, CampStatus.Published);
                return Ok(approvedCamp);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex) // error in business flow
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ: " + ex.Message });
            }
        }

        [HttpPut("{campId}/reject")]
        [Authorize]
        // [Authorize(Roles = "Manager")] 
        public async Task<IActionResult> RejectCamp(int campId)
        {
            try
            {
                // reject camp
                var rejectedCamp = await _campService.TransitionCampStatusAsync(campId, CampStatus.Rejected);
                return Ok(rejectedCamp);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex) // error in business flow
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ: " + ex.Message });
            }
        }

        [HttpPatch("{campId}/status")]
        [Authorize]
        public async Task<ActionResult<CampResponseDto>> UpdateCampStatus(int campId, [FromBody] CampStatusUpdateRequestDto statusUpdate)
        {
            try
            {
                // use TransitionCampStatusAsync to check bussiness flow
                var updatedCamp = await _campService.TransitionCampStatusAsync(campId, statusUpdate.Status);

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

        [HttpPatch("{campId}/submit-for-approval")]
        [Authorize]
        public async Task<ActionResult<CampResponseDto>> SubmitForApproval(int campId)
        {
            try
            {
                // use SubmitForApprovalAsync, check Activity/Group/Staff
                var updatedCamp = await _campService.SubmitForApprovalAsync(campId);

                return Ok(updatedCamp);
            }
            catch (ArgumentException ex) // flow error 
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) when (ex.Message.Contains("not found"))
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error submitting camp for approval." });
            }
        }
    }
}