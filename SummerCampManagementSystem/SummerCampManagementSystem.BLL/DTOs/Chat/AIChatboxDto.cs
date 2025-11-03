using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Chat
{
    public class AIChatboxDto
    {
        public class ChatMessageDto
        {
            [Required]
            public string Role { get; set; }

            [Required]
            public string Content { get; set; }
        }


        public class ChatRequestDto
        {
            /// <summary>
            /// all chat history, 
            /// including recent message from last user
            /// </summary>
            [Required]
            [MinLength(1, ErrorMessage = "Phải có ít nhất một tin nhắn.")]
            public List<ChatMessageDto> Messages { get; set; } = new List<ChatMessageDto>();
        }


        public class ChatResponseDto
        {
            public string TextResponse { get; set; }
        }
    }
}
