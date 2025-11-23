using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.Interfaces;
using static SummerCampManagementSystem.BLL.DTOs.Chat.AIChatboxDto;

namespace SummerCampManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize] 
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _chatService.GenerateResponseAsync(request);
            return Ok(response);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var history = await _chatService.GetConversationHistoryAsync();
            return Ok(history);
        }

        [HttpGet("conversation/{id}")]
        public async Task<IActionResult> GetMessages(int id)
        {
            try
            {
                var messages = await _chatService.GetMessagesByConversationIdAsync(id);
                return Ok(messages);
            }
            catch (KeyNotFoundException ex)
            {
                // error when user 1 try to get user b history
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConversation(int id)
        {
            try
            {
                await _chatService.DeleteConversationAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}

