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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroupActivity(int id)
        {
            try
            {
                var result = await _groupActivityService.RemoveGroupActivity(id);

                if (!result)
                {
                    return NotFound(new { message = $"Group activity with ID {id} not found." });
                }
                return Ok(new { message = "Đã xóa thành công." });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }


        }
    }
}
