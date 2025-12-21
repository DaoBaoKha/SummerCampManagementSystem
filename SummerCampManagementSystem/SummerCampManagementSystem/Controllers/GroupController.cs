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
    [Authorize]
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
            var memoryBefore = GC.GetTotalMemory(false) / 1024 / 1024;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            _logger.LogInformation(
                "[GroupController] [{RequestId}] GET /api/group - GetAllGroups called - MemoryBefore={MemoryMB}MB, ProcessMemory={ProcessMemoryMB}MB", 
                requestId, memoryBefore, Environment.WorkingSet / 1024 / 1024);
            
            try
            {
                var Groups = await _groupService.GetAllGroupsAsync();
                
                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
                
                _logger.LogInformation(
                    "[GroupController] [{RequestId}] GET /api/group - Success, returned {Count} groups, ElapsedMs={ElapsedMs}, MemoryBefore={MemoryBeforeMB}MB, MemoryAfter={MemoryAfterMB}MB, MemoryDelta={MemoryDeltaMB}MB",
                    requestId, Groups.Count(), stopwatch.ElapsedMilliseconds, memoryBefore, memoryAfter, memoryAfter - memoryBefore);
                    
                return Ok(Groups);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                stopwatch.Stop();
                _logger.LogError(dbEx, 
                    "[GroupController] [{RequestId}] GET /api/group - DATABASE ERROR: {ErrorMessage}, InnerException={InnerException}, StackTrace={StackTrace}", 
                    requestId, dbEx.Message, dbEx.InnerException?.Message, dbEx.StackTrace);
                return StatusCode(500, new { message = "Database error occurred.", detail = dbEx.Message });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
                
                _logger.LogError(ex, 
                    "[GroupController] [{RequestId}] GET /api/group - ERROR: {ErrorMessage}, ExceptionType={ExceptionType}, MemoryBefore={MemoryBeforeMB}MB, MemoryAfter={MemoryAfterMB}MB, StackTrace={StackTrace}", 
                    requestId, ex.Message, ex.GetType().Name, memoryBefore, memoryAfter, ex.StackTrace);
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
            var memoryBefore = GC.GetTotalMemory(false) / 1024 / 1024;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            _logger.LogInformation(
                "[GroupController] [{RequestId}] GET /api/group/camp/{CampId} called - MemoryBefore={MemoryMB}MB", 
                requestId, campId, memoryBefore);
            
            try
            {
                var groups = await _groupService.GetGroupsByCampIdAsync(campId);
                
                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
                
                _logger.LogInformation(
                    "[GroupController] [{RequestId}] GET /api/group/camp/{CampId} - Success, returned {Count} groups, ElapsedMs={ElapsedMs}, MemoryDelta={MemoryDeltaMB}MB", 
                    requestId, campId, groups.Count(), stopwatch.ElapsedMilliseconds, memoryAfter - memoryBefore);
                    
                return Ok(groups);
            }
            catch (NotFoundException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[GroupController] [{RequestId}] GET /api/group/camp/{CampId} - Not Found: {Message}, ElapsedMs={ElapsedMs}", 
                    requestId, campId, ex.Message, stopwatch.ElapsedMilliseconds);
                return NotFound(new { message = ex.Message });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                stopwatch.Stop();
                _logger.LogError(dbEx, 
                    "[GroupController] [{RequestId}] GET /api/group/camp/{CampId} - DATABASE ERROR: {ErrorMessage}, StackTrace={StackTrace}", 
                    requestId, campId, dbEx.Message, dbEx.StackTrace);
                return StatusCode(500, new { message = "Database error occurred.", detail = dbEx.Message });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
                
                _logger.LogError(ex, 
                    "[GroupController] [{RequestId}] GET /api/group/camp/{CampId} - ERROR: {ErrorMessage}, ExceptionType={ExceptionType}, MemoryBefore={MemoryBeforeMB}MB, MemoryAfter={MemoryAfterMB}MB, StackTrace={StackTrace}", 
                    requestId, campId, ex.Message, ex.GetType().Name, memoryBefore, memoryAfter, ex.StackTrace);
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
            catch (BadRequestException ex)
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
