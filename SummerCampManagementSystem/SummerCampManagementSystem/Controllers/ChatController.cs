using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Chat;
using SummerCampManagementSystem.BLL.Interfaces;


namespace SummerCampManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// send message (and history) to AI chatbox and receive answer
        /// </summary>
        /// <param name="requestDto">Request contains message history</param>
        /// <returns>AI Chatbox Response</returns>
        [HttpPost]
        [ProducesResponseType(typeof(AIChatboxDto.ChatResponseDto), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> GenerateChatResponse([FromBody] AIChatboxDto.ChatRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _chatService.GenerateResponseAsync(requestDto);
                return Ok(response);
            }
            catch (ArgumentException ex) // no message
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex) // api gemini error
            {
                // server error (api gemini or internet)
                return StatusCode(500, new { message = "Lỗi khi kết nối đến dịch vụ AI.", details = ex.Message });
            }
            catch (Exception ex) // others
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn.", details = ex.Message });
            }
        }
    }
}

