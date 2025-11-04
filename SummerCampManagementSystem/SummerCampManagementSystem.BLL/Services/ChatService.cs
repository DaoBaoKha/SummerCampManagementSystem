using Microsoft.Extensions.Options;
using SummerCampManagementSystem.BLL.DTOs.Chat;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SummerCampManagementSystem.BLL.Services
{

    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiApiSettings _settings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JsonSerializerOptions _jsonOptions;

        public ChatService(
            IHttpClientFactory httpClientFactory, 
            IOptions<GeminiApiSettings> settings,
            IUnitOfWork unitOfWork)
        {
            _httpClient = httpClientFactory.CreateClient();
            _settings = settings.Value;
            _unitOfWork = unitOfWork;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }


        public async Task<AIChatboxDto.ChatResponseDto> GenerateResponseAsync(AIChatboxDto.ChatRequestDto requestDto)
        {
            // prepare URL and API Key
            var fullUrl = $"{_settings.ApiBaseUrl}{_settings.ModelName}:generateContent?key={_settings.ApiKey}";

            // take last answer from user
            // use .Messages và .Content
            var userQuestion = requestDto.Messages.LastOrDefault(m => m.Role == "user")?.Content;
            if (string.IsNullOrEmpty(userQuestion))
            {
                throw new ArgumentException("User question not found in history.");
            }

            // read from database
            string context = await GetDatabaseContextAsync(userQuestion);

            // build payload to send to gemini
            var payload = BuildGeminiPayload(requestDto.Messages, context);

            // prepare request
            var jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, MediaTypeNames.Application.Json);

            // send request and take response
            var httpResponse = await _httpClient.PostAsync(fullUrl, httpContent);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error calling Gemini API: {httpResponse.StatusCode} - {errorBody}");
            }

            // read and return response
            var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiPayLoad.GeminiResponsePayload>(jsonResponse, _jsonOptions);

            var aiTextResponse = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                                 ?? "Xin lỗi, tôi không thể tạo câu trả lời vào lúc này.";

            return new AIChatboxDto.ChatResponseDto { TextResponse = aiTextResponse };
        }



        private GeminiPayLoad.GeminiRequestPayload BuildGeminiPayload(List<AIChatboxDto.ChatMessageDto> history, string context)
        {
            var payload = new GeminiPayLoad.GeminiRequestPayload();

            // System Instruction (AI role)
            payload.SystemInstruction = new GeminiPayLoad.GeminiContent
            {
                Parts = new List<GeminiPayLoad.GeminiPart>
                {
                    new GeminiPayLoad.GeminiPart { Text = "Bạn là một trợ lý AI thân thiện và chuyên nghiệp của hệ thống trại hè CampEase. Hãy trả lời câu hỏi của người dùng một cách ngắn gọn, tập trung vào câu hỏi. LUÔN LUÔN sử dụng thông tin trong phần 'BỐI CẢNH DỮ LIỆU' để trả lời nếu có." }
                }
            };

            // add context
            if (!string.IsNullOrEmpty(context))
            {
                payload.Contents.Add(new GeminiPayLoad.GeminiContent
                {
                    Role = "user",
                    Parts = new List<GeminiPayLoad.GeminiPart> { new GeminiPayLoad.GeminiPart { Text = $"BỐI CẢNH DỮ LIỆU:\n{context}\n---" } }
                });

                payload.Contents.Add(new GeminiPayLoad.GeminiContent
                {
                    Role = "model",
                    Parts = new List<GeminiPayLoad.GeminiPart> { new GeminiPayLoad.GeminiPart { Text = "Tôi đã sẵn sàng. Vui lòng đặt câu hỏi." } }
                });
            }

            // add chat history (skip last message as its the current request)
            foreach (var message in history.Take(history.Count - 1))
            {
                payload.Contents.Add(new GeminiPayLoad.GeminiContent
                {
                    Role = message.Role, // "user" or "model"
                    Parts = new List<GeminiPayLoad.GeminiPart> { new GeminiPayLoad.GeminiPart { Text = message.Content } }
                });
            }

            // add user last message
            var lastMessage = history.Last();
            payload.Contents.Add(new GeminiPayLoad.GeminiContent
            {
                Role = lastMessage.Role,
                Parts = new List<GeminiPayLoad.GeminiPart> { new GeminiPayLoad.GeminiPart { Text = lastMessage.Content } }
            });

            return payload;
        }

        /// <summary>
        /// READ FROM DATABASE TO ANSWER QUESTIONS
        /// logic RAG (Retrieval-Augmented Generation)
        /// </summary>
        private async Task<string> GetDatabaseContextAsync(string userQuestion)
        {
            var contextBuilder = new StringBuilder();
            var lowerQuestion = userQuestion.ToLower();

            // Logic 1: if ask about camp
            if (lowerQuestion.Contains("trại hè") || lowerQuestion.Contains("camp"))
            {
                var camps = await _unitOfWork.Camps.GetQueryable()
                                    .Include(c => c.location)
                                    .Take(5) // take 5 camps as example
                                    .ToListAsync();

                if (camps.Any())
                {
                    contextBuilder.AppendLine("Thông tin các trại hè có sẵn:");
                    foreach (var camp in camps)
                    {
                        contextBuilder.AppendLine($"- Tên: {camp.name}, Địa điểm: {camp.location?.name ?? "N/A"}, Giá: {camp.price?.ToString("N0")} VND.");
                    }
                }
            }

            // Logic 2: if ask about price
            if (lowerQuestion.Contains("giá") || lowerQuestion.Contains("bao nhiêu tiền"))
            {
                // add more details logic here
                contextBuilder.AppendLine("Để biết giá chi tiết, xin vui lòng xem thông tin trại hè cụ thể.");
            }

            // more logic here

            return contextBuilder.ToString();
        }
    }
}

