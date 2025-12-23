using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.GroupActivity;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/group-activity")]
    [ApiController]
    //[Authorize]
    public class GroupActivityController : ControllerBase
    {
        private readonly IGroupActivityService _groupActivityService;

        public GroupActivityController(IGroupActivityService groupActivityService)
        {
            _groupActivityService = groupActivityService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroupActivity([FromBody] GroupActivityDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Dữ liệu yêu cầu không hợp lệ.";
                
                return BadRequest(new { message = errorMessage });
            }

            try
            {
                var created = await _groupActivityService.CreateGroupActivity(dto);
                
                return CreatedAtAction(
                    nameof(CreateGroupActivity),
                    new { id = created.groupActivityId },
                    created);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }  
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteGroupActivity([FromQuery] int groupId, [FromQuery] int activityScheduleId)
        {
            try
            {
                var result = await _groupActivityService.RemoveGroupActivity(groupId, activityScheduleId);

                if (!result)
                {
                    return NotFound(new { message = $"Không tìm thấy hoạt động cho nhóm {groupId} và lịch hoạt động {activityScheduleId}." });
                }
                return Ok(new { message = "Đã xóa thành công." });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn khi xóa hoạt động nhóm." });
            }
        }
    }
}
