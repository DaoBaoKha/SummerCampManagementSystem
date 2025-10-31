using Microsoft.AspNetCore.Http;
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

        [HttpGet]
        public async Task<IActionResult> GetAllCamperGroups()
        {
            var camperGroups = await _camperGroupService.GetAllCamperGroupsAsync();
            return Ok(camperGroups);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCamperGroupById(int id)
        {
            var camperGroup = await _camperGroupService.GetCamperGroupByIdAsync(id);
            if (camperGroup == null) return NotFound();
            return Ok(camperGroup);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCamperGroup([FromBody] CamperGroupRequestDto camperGroup)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var newCamperGroup = await _camperGroupService.CreateCamperGroupAsync(camperGroup);

                return CreatedAtAction(nameof(GetCamperGroupById),
                    new { id = newCamperGroup.CamperGroupId }, newCamperGroup);
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCamperGroup(int id, [FromBody] CamperGroupRequestDto camperGroup)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updatedCamperGroup = await _camperGroupService.UpdateCamperGroupAsync(id, camperGroup);
            if (updatedCamperGroup == null)
                return NotFound(new { message = $"CamperGroup with ID {id} not found" });

            return Ok(updatedCamperGroup);
        }

        [HttpPut("{camperGroupId}/assign-staff/{staffId}")]
        public async Task<IActionResult> AssignStaffToGroup(int camperGroupId, int staffId)
        {
            try
            {
                var updatedCamperGroup = await _camperGroupService.AssignStaffToGroup(camperGroupId, staffId);
                return Ok(updatedCamperGroup);
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


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCamperGroup(int id)
        {
            var result = await _camperGroupService.DeleteCamperGroupAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }


    }
}
