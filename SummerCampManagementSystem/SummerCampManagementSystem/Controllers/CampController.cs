using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.DTOs.Camper;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/camp")]
    [ApiController]
    public class CampController : ControllerBase
    {
        private readonly ICampService _campService;
        private readonly ILogger<CampController> _logger;

        public CampController(ICampService campService, ILogger<CampController> logger)
        {
            _campService = campService;
            _logger = logger;
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
                return NotFound(new { message = $"Camp with ID {id} not found." });

            return Ok(camp);
        }

        [HttpGet("status")]
        public async Task<ActionResult<IEnumerable<CampResponseDto>>> GetCampsByStatus([FromQuery] CampStatus? status)
        {
            var camps = await _campService.GetCampsByStatusAsync(status);
            return Ok(camps);
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
                return BadRequest(ModelState);

            var createdCamp = await _campService.CreateCampAsync(camp);

            return CreatedAtAction(nameof(GetCampById),     
                new { id = createdCamp.CampId }, createdCamp);
        }

        /// <summary>
        /// Endpoint to run scheduled status updates for camps.
        /// </summary>
        /// <returns></returns>
        [HttpPost("scheduled-status-update")]
        public async Task<IActionResult> RunScheduledStatusUpdates()
        {
            _logger.LogInformation("API Worker triggered by Cloud Scheduler.");
            try
            {
                await _campService.RunScheduledStatusTransitionsAsync();

                // Log success and return 200 OK
                _logger.LogInformation("API Worker finished successfully.");
                return Ok(new { message = "Scheduled status updates executed successfully." });
            }
            catch (Exception ex)
            {
                // Log error and return 500
                _logger.LogError(ex, "API Worker failed during status updates.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi khi chạy tác vụ cập nhật trạng thái: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCamp(int id, [FromBody] CampRequestDto camp)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            var updatedCamp = await _campService.UpdateCampAsync(id, camp);

            return Ok(updatedCamp);
        }


        [HttpPut("{campId}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveCamp(int campId)
        {
            var approvedCamp = await _campService.TransitionCampStatusAsync(campId, CampStatus.Published);

            return Ok(approvedCamp);
        }

        [HttpPut("{campId}/reject")]
        [Authorize(Roles = "Admin")]  
        public async Task<IActionResult> RejectCamp(int campId)
        {
            var rejectedCamp = await _campService.TransitionCampStatusAsync(campId, CampStatus.Rejected);

            return Ok(rejectedCamp);
        }

        [HttpPatch("{id}/extend-registration")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> ExtendRegistration(int id, [FromBody] CampExtensionRequestDto request)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            var result = await _campService.ExtendRegistrationAsync(id, request.NewRegistrationEndDate);

            return Ok(result);
        }

        [HttpPatch("{campId}/status")]
        [Authorize]
        public async Task<ActionResult<CampResponseDto>> UpdateCampStatus(int campId, [FromBody] CampStatusUpdateRequestDto statusUpdate)
        {
            var updatedCamp = await _campService.UpdateCampStatusAsync(campId, statusUpdate);

            return Ok(updatedCamp);
        }

        [HttpPatch("{campId}/submit-for-approval")]
        [Authorize]
        public async Task<ActionResult<CampResponseDto>> SubmitForApproval(int campId)
        {
            var updatedCamp = await _campService.SubmitForApprovalAsync(campId);

            return Ok(updatedCamp);
        }
    }
}