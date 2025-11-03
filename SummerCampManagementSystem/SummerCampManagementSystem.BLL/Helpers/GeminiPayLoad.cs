using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Helpers
{
    public class GeminiPayLoad
    {
        // REQUEST

        /// <summary>
        /// Payload chính gửi đến API của Gemini
        /// </summary>
        public class GeminiRequestPayload
        {
            [JsonPropertyName("contents")]
            public List<GeminiContent> Contents { get; set; } = new List<GeminiContent>();

            [JsonPropertyName("systemInstruction")]
            public GeminiContent SystemInstruction { get; set; }
        }

        /// <summary>
        /// Đại diện cho một "phần" của hội thoại (của user hoặc model)
        /// </summary>
        public class GeminiContent
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } // "user" or "model"

            [JsonPropertyName("parts")]
            public List<GeminiPart> Parts { get; set; } = new List<GeminiPart>();
        }

        /// <summary>
        /// Nội dung text thực tế
        /// </summary>
        public class GeminiPart
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        // RESPONSE

        /// <summary>
        /// Payload chính nhận về từ API của Gemini
        /// </summary>
        public class GeminiResponsePayload
        {
            [JsonPropertyName("candidates")]
            public List<GeminiCandidate> Candidates { get; set; } = new List<GeminiCandidate>();
        }

        /// <summary>
        /// Một câu trả lời tiềm năng (thường chỉ có 1)
        /// </summary>
        public class GeminiCandidate
        {
            [JsonPropertyName("content")]
            public GeminiContent Content { get; set; }
        }
    }
}
