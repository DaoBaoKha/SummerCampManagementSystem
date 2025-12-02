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
        public async Task<IActionResult> GetCamperGroups([FromQuery] CamperGroupSearchDto searchDto)
        {
            try
            {
                var result = await _camperGroupService.GetCamperGroupsAsync(searchDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống", detail = ex.Message });
            }
        }

        /// <summary>
        /// Manual Add Camper Into Group
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCamperGroup([FromBody] CamperGroupRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _camperGroupService.CreateCamperGroupAsync(requestDto);
                return CreatedAtAction(nameof(GetCamperGroups), new { camperGroupId = result.camperGroupId }, result);
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
                return StatusCode(500, new { message = "Lỗi khi tạo phân nhóm", detail = ex.Message });
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCamperGroup(int id, [FromBody] CamperGroupRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _camperGroupService.UpdateCamperGroupAsync(id, requestDto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật phân nhóm", detail = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCamperGroup(int id)
        {
            try
            {
                var isDeleted = await _camperGroupService.DeleteCamperGroupAsync(id);
                if (!isDeleted)
                {
                    return NotFound(new { message = "Không tìm thấy thông tin phân nhóm để xóa." });
                }

                return NoContent(); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa phân nhóm", detail = ex.Message });
            }
        }
    }
}