using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Group;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/group")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly IUserContextService _userContextService;

        public GroupController(IGroupService GroupService, IUserContextService userContextService)
        {
            _groupService = GroupService;
            _userContextService = userContextService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGroups()
        {
            var Groups = await _groupService.GetAllGroupsAsync();
            return Ok(Groups);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGroupById(int id)
        {
            try 
            {
                var Group = await _groupService.GetGroupByIdAsync(id);
                return Ok(Group);
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
        public async Task<IActionResult> GetGroupsByActivityScheduleId(int id)
        {
            try
            {
                var Groups = await _groupService.GetGroupsByActivityScheduleId(id);
                return Ok(Groups);
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
                var groups = await _groupService.GetGroupsByCampIdAsync(campId);
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

        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] GroupRequestDto Group)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                                   ?? "Dữ liệu yêu cầu không hợp lệ.";
                return BadRequest(new { message = errorMessage });
            }

            try
            {
                var newGroup = await _groupService.CreateGroupAsync(Group);

                return CreatedAtAction(nameof(GetGroupById),
                    new { id = newGroup.GroupId }, newGroup);
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
        public async Task<IActionResult> UpdateGroup(int id, [FromBody] GroupRequestDto Group)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updatedGroup = await _groupService.UpdateGroupAsync(id, Group);
            if (updatedGroup == null)
                return NotFound(new { message = $"Group with ID {id} not found" });

            return Ok(updatedGroup);
        }

        [HttpPut("{GroupId}/assign-staff/{staffId}")]
        public async Task<IActionResult> AssignStaffToGroup(int GroupId, int staffId)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                                   ?? "Dữ liệu yêu cầu không hợp lệ.";
                return BadRequest(new { message = errorMessage });
            }

            try
            {
                var updatedGroup = await _groupService.AssignStaffToGroup(GroupId, staffId);
                return Ok(updatedGroup);
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
        public async Task<IActionResult> DeleteGroup(int id)
        {
            try
            {
                await _groupService.DeleteGroupAsync(id);
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
