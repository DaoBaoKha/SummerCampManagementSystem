using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Feedback;
using SummerCampManagementSystem.BLL.DTOs.Report;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;
        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }
        // GET: api/<FeedbackController>

        [HttpGet]
        public async Task<IActionResult> GetAllFeedback()
        {
            var feedbacks = await _feedbackService.GetAllFeedbacksAsync();
            return Ok(feedbacks);
        }

        // GET api/<FeedbackController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFeedbackById(int id)
        {
            var feedback = await _feedbackService.GetFeedbackByIdAsync(id);
            if (feedback == null) return NotFound();
            return Ok(feedback);
        }

        [HttpGet("camps/{campId}")]
        public async Task<IActionResult> GetFeedbackByCamp(int campId)
        {
            try
            {
                var feedbacks = await _feedbackService.GetFeedbacksByCampIdAsync(campId);
                return Ok(feedbacks);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error", detail = ex.Message });
            }
        }


        [HttpGet("registrattions/{registrationId}")]
        public async Task<IActionResult> GetFeedbackByRegistration(int registrationId)
        {
            try 
            {
                var feedbacks = await _feedbackService.GetFeedbacksByRegistrationIdAsync(registrationId);
                return Ok(feedbacks);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error", detail = ex.Message });
            }

        }

        // POST api/<FeedbackController>
        [HttpPost]
        public async Task<IActionResult> CreateFeedback([FromBody] FeedbackRequestDto feedback)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdFeedback = await _feedbackService.CreateFeedbackAsync(feedback);
                return CreatedAtAction(nameof(GetFeedbackById), new { id = createdFeedback.FeedbackId }, createdFeedback);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error", detail = ex.Message });
            }
        }

        // PUT api/<FeedbackController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFeedback(int id, [FromBody] FeedbackRequestDto feedback)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var updatedFeedback = await _feedbackService.UpdateFeedbackAsync(id, feedback);
                if (updatedFeedback == null) return NotFound();
                return Ok(updatedFeedback);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error", detail = ex.Message });
            }
        }

        [Authorize(Roles = "Manager")]
        [HttpPut("manager-reply/{id}")]
        public async Task<IActionResult> ReplyFeedback(int id, FeedbackReplyRequestDto reply)
        {
            try
            {
                var repliedFeedback = await _feedbackService.ReplyFeedback(id, reply);
                return Ok(repliedFeedback);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error", detail = ex.Message });
            }
        }

        [Authorize(Roles = "Manager")]
        [HttpPut("reject/{id}")]
        public async Task<IActionResult> RejectFeedback(int id, FeedbackRejectedRequestDto reject)
        {
            try
            {
                var rejectedFeedback = await _feedbackService.RejectFeedback(id, reject);
                return Ok(rejectedFeedback);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error", detail = ex.Message });
            }
        }


        // DELETE api/<FeedbackController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            var result = await _feedbackService.DeleteFeedbackAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }


    }
}
