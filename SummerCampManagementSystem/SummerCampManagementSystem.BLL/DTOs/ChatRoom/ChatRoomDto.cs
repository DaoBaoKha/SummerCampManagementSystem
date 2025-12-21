using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.ChatRoom
{
    public class ChatRoomDto
    {
    }

    public class SendMessageDto
    {
        public int ChatRoomId { get; set; }
        public string Content { get; set; }
    }

    public class ChatRoomMessageDto
    {
        public int MessageId { get; set; }
        public int ChatRoomId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string Avatar { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
    }

    public class ChatRoomDetailDto
    {
        public int ChatRoomId { get; set; }
        public string Name { get; set; }
        public int Type { get; set; } // private or group
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class CreateOrGetPrivateRoomRequestDto
    {
        [Required(ErrorMessage = "ID người nhận là bắt buộc.")]
        public int RecipientUserId { get; set; }
    }

    public class CreateOrGetPrivateRoomResponseDto
    {
        public int ChatRoomId { get; set; }
        public bool IsNewRoom { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientAvatar { get; set; } = string.Empty;
        public int RecipientUserId { get; set; }
    }
}
