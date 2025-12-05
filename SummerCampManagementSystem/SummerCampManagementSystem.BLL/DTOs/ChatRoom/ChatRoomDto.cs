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
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string Avatar { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
    }
}
