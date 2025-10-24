using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Promotion;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/promotion")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public PromotionController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPromotions()
        {
            var promotions = await _promotionService.GetAllPromotionsAsync();
            return Ok(promotions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPromotionById(int id)
        {
            var promotion = await _promotionService.GetPromotionByIdAsync(id);
            if (promotion == null)
            {
                return NotFound(new { message = $"Promotion with ID {id} not found." });
            }
            return Ok(promotion);
        }

        [HttpGet("valid")]
        public async Task<IActionResult> GetValidPromotions()
        {
            try
            {
                var promotions = await _promotionService.GetValidPromotionsAsync();

                return Ok(promotions);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving valid promotions.", details = ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePromotion([FromBody] PromotionRequestDto promotionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var newPromotion = await _promotionService.CreatePromotionAsync(promotionDto);
                return CreatedAtAction(nameof(GetPromotionById), new { id = newPromotion.Id }, newPromotion);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] PromotionRequestDto promotionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedPromotion = await _promotionService.UpdatePromotionAsync(id, promotionDto);
                return Ok(updatedPromotion);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var result = await _promotionService.DeletePromotionAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"Promotion with ID {id} not found." });
            }
            return NoContent();
        }
    }
}
