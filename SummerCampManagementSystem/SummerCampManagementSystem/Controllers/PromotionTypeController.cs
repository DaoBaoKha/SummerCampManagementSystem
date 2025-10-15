using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.PromotionType;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionTypeController : ControllerBase
    {
        private readonly IPromotionTypeService _promotionTypeService;
        public PromotionTypeController(IPromotionTypeService promotionTypeService)
        {
            _promotionTypeService = promotionTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPromotionTypes()
        {
            var promotionTypes = await _promotionTypeService.GetAllPromotionTypesAsync();
            return Ok(promotionTypes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPromotionTypeById(int id)
        {
            var promotionType = await _promotionTypeService.GetPromotionTypeByIdAsync(id);
            if (promotionType == null)
            {
                return NotFound(new { message = "Promotion type not found." });
            }
            return Ok(promotionType);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePromotionType([FromBody] PromotionTypeRequestDto promotionTypeDto)
        {
            var createdPromotionType = await _promotionTypeService.CreatePromotionTypeAsync(promotionTypeDto);
            return CreatedAtAction(nameof(GetPromotionTypeById), new { id = createdPromotionType.Id }, createdPromotionType);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePromotionType(int id, [FromBody] PromotionTypeRequestDto promotionTypeDto)
        {
            var updatedPromotionType = await _promotionTypeService.UpdatePromotionTypeAsync(id, promotionTypeDto);
            if (updatedPromotionType == null)
            {
                return NotFound(new { message = "Promotion type not found." });
            }
            return Ok(updatedPromotionType);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromotionType(int id)
        {
            var isDeleted = await _promotionTypeService.DeletePromotionTypeAsync(id);
            if (!isDeleted)
            {
                return NotFound(new { message = "Promotion type not found." });
            }
            return NoContent();
        }
    }
}
