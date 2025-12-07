using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.Chat
{
    public class AIChatboxDto
    {

        public class ChatRequestDto
        {
            /*
            ID of conversation
            leave NULL if NEW conversation.
            */

            public int? ConversationId { get; set; }

            [Required(ErrorMessage = "Message content is required.")]
            [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters.")]
            public string Message { get; set; }
        }


        public class ChatResponseDto
        {
            // AI response
            public string? TextResponse { get; set; }


            public int ConversationId { get; set; }

            public string? Title { get; set; }
        }


        public class ChatMessageDto
        {
            public int MessageId { get; set; }
            public string Role { get; set; } // "user" or "model"
            public string Content { get; set; }
            public DateTime SentAt { get; set; }
        }


        // chat history
        public class ChatConversationDto
        {
            public int ConversationId { get; set; }
            public string Title { get; set; }
            public DateTime CreatedAt { get; set; }
        }

    } 
}

