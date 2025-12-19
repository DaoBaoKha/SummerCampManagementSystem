using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.DTOs.Group;
using SummerCampManagementSystem.BLL.Exceptions;
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
        private readonly ILogger<GroupController> _logger;

        public GroupController(IGroupService GroupService, IUserContextService userContextService, ILogger<GroupController> logger)
        {
            _groupService = GroupService;
            _userContextService = userContextService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGroups()
        {
            var requestId = Guid.NewGuid();
            _logger.LogInformation("[GroupController] [{RequestId}] GET /api/group - GetAllGroups called", requestId);
            
            try
            {
                var Groups = await _groupService.GetAllGroupsAsync();
                _logger.LogInformation("[GroupController] [{RequestId}] GET /api/group - Success, returned {Count} groups", requestId, Groups.Count());
                return Ok(Groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GroupController] [{RequestId}] GET /api/group - ERROR: {ErrorMessage}", 
                    requestId, ex.Message);
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGroupById(int id)
        {
            var requestId = Guid.NewGuid();
            _logger.LogInformation("[GroupController] [{RequestId}] GET /api/group/{GroupId} called", requestId, id);
            
            try 
            {
                var Group = await _groupService.GetGroupByIdAsync(id);
                _logger.LogInformation("[GroupController] [{RequestId}] GET /api/group/{GroupId} - Success", requestId, id);
                return Ok(Group);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("[GroupController] [{RequestId}] GET /api/group/{GroupId} - Not Found: {Message}", requestId, id, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GroupController] [{RequestId}] GET /api/group/{GroupId} - ERROR: {ErrorMessage}", 
                    requestId, id, ex.Message);
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("activityScheduleId/{id}")]
        public async Task<IActionResult> GetGroupsByActivityScheduleId(int id)
        {
            var requestId = Guid.NewGuid();
            _logger.LogInformation("[GroupController] [{RequestId}] GET /api/group/activityScheduleId/{ActivityScheduleId} called", requestId, id);
            
            try
            {
                var Groups = await _groupService.GetGroupsByActivityScheduleId(id);
                _logger.LogInformation("[GroupController] [{RequestId}] GET /api/group/activityScheduleId/{ActivityScheduleId} - Success, returned {Count} groups", 
                    requestId, id, Groups.Count());
                return Ok(Groups);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("[GroupController] [{RequestId}] GET /api/group/activityScheduleId/{ActivityScheduleId} - Not Found: {Message}", 
                    requestId, id, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GroupController] [{RequestId}] GET /api/group/activityScheduleId/{ActivityScheduleId} - ERROR: {ErrorMessage}", 
                    requestId, id, ex.Message);
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("camp/{campId}")]
        public async Task<IActionResult> GetGroupsByCampId(int campId)
        {
            var requestId = Guid.NewGuid();
            _logger.LogInformation("[GroupController] [{RequestId}] GET /api/group/camp/{CampId} called", requestId, campId);
            
            try
            {
                var groups = await _groupService.GetGroupsByCampIdAsync(campId);
                _logger.LogInformation("[GroupController] [{RequestId}] GET /api/group/camp/{CampId} - Success, returned {Count} groups", 
                    requestId, campId, groups.Count());
                return Ok(groups);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning("[GroupController] [{RequestId}] GET /api/group/camp/{CampId} - Not Found: {Message}", requestId, campId, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GroupController] [{RequestId}] GET /api/group/camp/{CampId} - ERROR: {ErrorMessage}", 
                    requestId, campId, ex.Message);
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
