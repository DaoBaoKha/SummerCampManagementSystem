using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.ChatRoom;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/chat-rooms")]
    [ApiController]
    [Authorize]
    public class ChatRoomController : ControllerBase
    {
        private readonly IChatRoomService _chatRoomService;
        private readonly IUserContextService _userContextService;

        public ChatRoomController(IChatRoomService chatRoomService, IUserContextService userContextService)
        {
            _chatRoomService = chatRoomService;
            _userContextService = userContextService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto request)
        {
            var userId = _userContextService.GetCurrentUserId();

            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Không xác định được người dùng." });
            }

            var result = await _chatRoomService.SendMessageAsync(userId.Value, request);

            return Ok(result);
        }

        [HttpGet("my-rooms")]
        public async Task<IActionResult> GetMyRooms()
        {
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Không xác định được người dùng." });

            var rooms = await _chatRoomService.GetMyChatRoomsAsync(userId.Value);
            return Ok(rooms);
        }

        [HttpGet("{roomId}/messages")]
        public async Task<IActionResult> GetMessages(int roomId)
        {
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Không xác định được người dùng." });

            var messages = await _chatRoomService.GetMessagesByRoomIdAsync(userId.Value, roomId);
            return Ok(messages);
        }

        [HttpPost("create-or-get-private")]
        public async Task<IActionResult> CreateOrGetPrivateRoom([FromBody] CreateOrGetPrivateRoomRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Không xác định được người dùng." });

            var result = await _chatRoomService.CreateOrGetPrivateRoomAsync(userId.Value, request.RecipientUserId);
            return Ok(result);
        }

        [HttpGet("{roomId}/details")]
        public async Task<IActionResult> GetRoomDetails(int roomId)
        {
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized(new { message = "Không xác định được người dùng." });

            var details = await _chatRoomService.GetRoomDetailsAsync(userId.Value, roomId);
            return Ok(details);
        }
    }
}