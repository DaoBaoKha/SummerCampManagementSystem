using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.CamperGroup;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/campergroup")]
    [ApiController]
    public class CamperGroupController : ControllerBase
    {
        private readonly ICamperGroupService _camperGroupService;
        private readonly IUserContextService _userContextService;

        public CamperGroupController(ICamperGroupService camperGroupService, IUserContextService userContextService)
        {
            _camperGroupService = camperGroupService;
            _userContextService = userContextService;
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
            try 
            {
                var camperGroup = await _camperGroupService.GetCamperGroupByIdAsync(id);
                return Ok(camperGroup);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("activityScheduleId/{id}")]
        public async Task<IActionResult> GetCamperGroupsByActivityScheduleId(int id)
        {
            try
            {
                var camperGroups = await _camperGroupService.GetGroupsByActivityScheduleId(id);
                return Ok(camperGroups);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });

            }
        }

        [HttpGet("camp/{campId}")]
        public async Task<IActionResult> GetGroupsByCampId(int campId)
        {
            try
            {
                var groups = await _camperGroupService.GetGroupsByCampIdAsync(campId);
                return Ok(groups);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("activityScheduleId/{id}")]
        public async Task<IActionResult> GetCamperGroupsByActivityScheduleId(int id)
        {
            try
            {
                var camperGroups = await _camperGroupService.GetGroupsByActivityScheduleId(id);
                return Ok(camperGroups);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });

            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCamperGroup([FromBody] CamperGroupRequestDto camperGroup)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                                   ?? "Dữ liệu yêu cầu không hợp lệ.";
                return BadRequest(new { message = errorMessage });
            }

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
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                                   ?? "Dữ liệu yêu cầu không hợp lệ.";
                return BadRequest(new { message = errorMessage });
            }

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
            try
            {
                await _camperGroupService.DeleteCamperGroupAsync(id);
                return NoContent(); 
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message }); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }


    }
}
