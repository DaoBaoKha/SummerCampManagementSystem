using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.CamperGroup;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/campergroup")]
    [ApiController]
    public class CamperGroupController : ControllerBase
    {
        private readonly ICamperGroupService _camperGroupService;

        public CamperGroupController(ICamperGroupService camperGroupService)
        {
            _camperGroupService = camperGroupService;
        }

        /// <summary>
        /// Get list of Camper Group based on search criteria
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCamperGroups([FromQuery] CamperGroupSearchDto searchDto)
        {
            var result = await _camperGroupService.GetCamperGroupsAsync(searchDto);
            return Ok(result);
        }

        /// <summary>
        /// Get list of campers waiting for manual group assignment
        /// </summary>
        [HttpGet("pending-assign")]
        public async Task<IActionResult> GetPendingAssignCamperGroups([FromQuery] int? campId)
        {
            var result = await _camperGroupService.GetPendingAssignCampersAsync(campId);
            return Ok(result);
        }

        /// <summary>
        /// Manual Add Camper Into Group. Used for pending assign group state
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCamperGroup([FromBody] CamperGroupRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _camperGroupService.CreateCamperGroupAsync(requestDto);

            return CreatedAtAction(nameof(GetCamperGroups), new { camperGroupId = result.camperGroupId }, result);
        }

        /// <summary>
        /// Manual Update Group
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCamperGroup(int id, [FromBody] CamperGroupRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _camperGroupService.UpdateCamperGroupAsync(id, requestDto);
            return Ok(result);
        }

        /// <summary>
        /// Soft Delete Group
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCamperGroup(int id)
        {
            var isDeleted = await _camperGroupService.DeleteCamperGroupAsync(id);

            // extra in case service not return exception
            if (!isDeleted)
            {
                return NotFound(new { message = "Không tìm thấy thông tin phân nhóm để xóa." });
            }

            return NoContent();
        }
    }
}