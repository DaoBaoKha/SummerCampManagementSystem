using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.FAQ;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FAQController : ControllerBase
    {
        private readonly IFAQService _faqService;

        public FAQController(IFAQService faqService)
        {
            _faqService = faqService;
        }

        /// <summary>
        /// Get all FAQs
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllFAQs()
        {
            var faqs = await _faqService.GetAllFAQsAsync();
            return Ok(faqs);
        }

        /// <summary>
        /// Get FAQ by ID
        /// </summary>
        [HttpGet("{faqId}")]
        public async Task<IActionResult> GetFAQById(int faqId)
        {
            var faq = await _faqService.GetFAQByIdAsync(faqId);
            
            if (faq == null)
                return NotFound(new { message = $"FAQ with id {faqId} not found" });
            
            return Ok(faq);
        }

        /// <summary>
        /// Create new FAQ
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateFAQ([FromBody] FAQRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var createdFAQ = await _faqService.CreateFAQAsync(dto);
            return CreatedAtAction(nameof(GetFAQById), new { faqId = createdFAQ.FaqId }, createdFAQ);
        }

        /// <summary>
        /// Update existing FAQ
        /// </summary>
        [HttpPut("{faqId}")]
        public async Task<IActionResult> UpdateFAQ(int faqId, [FromBody] FAQRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var updatedFAQ = await _faqService.UpdateFAQAsync(faqId, dto);
            
            if (updatedFAQ == null)
                return NotFound(new { message = $"FAQ with id {faqId} not found" });
            
            return Ok(updatedFAQ);
        }

        /// <summary>
        /// Delete FAQ
        /// </summary>
        [HttpDelete("{faqId}")]
        public async Task<IActionResult> DeleteFAQ(int faqId)
        {
            var result = await _faqService.DeleteFAQAsync(faqId);
            
            if (!result)
                return NotFound(new { message = $"FAQ with id {faqId} not found" });
            
            return Ok(new { message = "FAQ deleted successfully" });
        }
    }
}
