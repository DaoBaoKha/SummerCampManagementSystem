using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static SummerCampManagementSystem.BLL.DTOs.Chat.AIChatboxDto;
using static SummerCampManagementSystem.BLL.Helpers.GeminiPayLoad;

namespace SummerCampManagementSystem.BLL.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContextService;
        private readonly HttpClient _httpClient;
        private readonly GeminiApiSettings _geminiApiSettings;
        private readonly JsonSerializerOptions _jsonOptions;

        public ChatService(
            IUnitOfWork unitOfWork,
            IUserContextService userContextService,
            IHttpClientFactory httpClientFactory,
            IOptions<GeminiApiSettings> geminiApiSettings) 
        {
            _unitOfWork = unitOfWork;
            _userContextService = userContextService;

            _httpClient = httpClientFactory.CreateClient("GeminiApiClient");

            _geminiApiSettings = geminiApiSettings.Value;

            // base address setup
            _httpClient.BaseAddress = new Uri(_geminiApiSettings.ApiBaseUrl);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }


        public async Task<ChatResponseDto> GenerateResponseAsync(ChatRequestDto request)
        {
            var userId = _userContextService.GetCurrentUserId()
                ?? throw new UnauthorizedAccessException("Người dùng không được xác thực. Vui lòng đăng nhập lại.");

            // get or create conversation
            var conversation = await GetOrCreateConversationAsync(request.ConversationId, userId, request.Message);

            // save user messages into db
            var userMessage = new ChatMessage
            {
                conversationId = conversation.chatConversationId,
                role = "user",
                content = request.Message,
                sentAt = DateTime.UtcNow
            };
            await _unitOfWork.ChatMessages.CreateAsync(userMessage);
            await _unitOfWork.CommitAsync(); 

            // get context RAG and hsitory
            var contextPrompt = await GetDatabaseContextAsync(request.Message);
            var history = await GetChatHistoryAsync(conversation.chatConversationId);

            // gemini api
            string modelResponseText;
            try
            {
                modelResponseText = await CallGeminiApiAsync(contextPrompt, history);
            }
            catch (Exception ex)
            {
                // if fail, save into db and tell user
                modelResponseText = $"Lỗi: Hệ thống AI đang gặp sự cố. Vui lòng thử lại sau. (Chi tiết: {ex.Message})";
                await SaveModelMessageAsync(conversation.chatConversationId, modelResponseText, "error");

                throw new InvalidOperationException("Lỗi khi kết nối đến dịch vụ AI.", ex);
            }

            await SaveModelMessageAsync(conversation.chatConversationId, modelResponseText, "model");

            return new ChatResponseDto
            {
                TextResponse = modelResponseText,
                ConversationId = conversation.chatConversationId,
                Title = conversation.title
            };
        }


        public async Task<IEnumerable<ChatConversationDto>> GetConversationHistoryAsync()
        {
            var userId = _userContextService.GetCurrentUserId()
                ?? throw new UnauthorizedAccessException("Người dùng không được xác thực.");

            var conversations = await _unitOfWork.ChatConversations.GetQueryable()
                .Where(c => c.userId == userId)
                .OrderByDescending(c => c.createdAt)
                .ToListAsync(); 

            return conversations.Select(c => new ChatConversationDto
            {
                ConversationId = c.chatConversationId,
                Title = c.title,
                CreatedAt = c.createdAt ?? DateTime.MinValue 
            });
        }

        public async Task<IEnumerable<ChatMessageDto>> GetMessagesByConversationIdAsync(int conversationId)
        {
            var userId = _userContextService.GetCurrentUserId()
                ?? throw new UnauthorizedAccessException("Người dùng không được xác thực.");

            var conversation = await _unitOfWork.ChatConversations.GetByIdAsync(conversationId);
            if (conversation == null || conversation.userId != userId)
            {
                throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện hoặc bạn không có quyền truy cập.");
            }

            var messages = await _unitOfWork.ChatMessages.GetQueryable()
                .Where(m => m.conversationId == conversationId)
                .OrderBy(m => m.sentAt)
                .ToListAsync();

            return messages.Select(m => new ChatMessageDto
            {
                MessageId = m.chatMessageId,
                Role = m.role,
                Content = m.content,
                SentAt = m.sentAt ?? DateTime.MinValue 
            });
        }


        public async Task DeleteConversationAsync(int conversationId)
        {
            var userId = _userContextService.GetCurrentUserId()
                ?? throw new UnauthorizedAccessException("Người dùng không được xác thực.");

            var conversation = await _unitOfWork.ChatConversations.GetByIdAsync(conversationId);
            if (conversation == null || conversation.userId != userId)
            {
                throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện hoặc bạn không có quyền xóa.");
            }

            // delete messages first if havent set Cascade delete in db
            var messages = await _unitOfWork.ChatMessages.GetQueryable()
                .Where(m => m.conversationId == conversationId)
                .ToListAsync();

            _unitOfWork.ChatMessages.RemoveRange(messages);
            _unitOfWork.ChatConversations.RemoveAsync(conversation);

            await _unitOfWork.CommitAsync();
        }



        /*
         PRIVATE HELPERS
         */
        private async Task<ChatConversation> GetOrCreateConversationAsync(int? conversationId, int userId, string firstMessage)
        {
            if (conversationId.HasValue)
            {
                var existingConversation = await _unitOfWork.ChatConversations.GetByIdAsync(conversationId.Value);
                if (existingConversation != null && existingConversation.userId == userId)
                {
                    return existingConversation;
                }
                throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện hoặc bạn không có quyền truy cập.");
            }

            // if id null
            var newConversation = new ChatConversation
            {
                userId = userId,
                // get first 100 words for title
                title = firstMessage.Length > 100 ? firstMessage.Substring(0, 100) + "..." : firstMessage,
                createdAt = DateTime.UtcNow
            };

            await _unitOfWork.ChatConversations.CreateAsync(newConversation);
            await _unitOfWork.CommitAsync(); 

            return newConversation;
        }

        private async Task<ChatMessage> SaveModelMessageAsync(int conversationId, string message, string role)
        {
            var modelMessage = new ChatMessage
            {
                conversationId = conversationId,
                role = role, // "model" or "error"
                content = message,
                sentAt = DateTime.UtcNow
            };
            await _unitOfWork.ChatMessages.CreateAsync(modelMessage);
            await _unitOfWork.CommitAsync();
            return modelMessage;
        }

        private async Task<List<GeminiContent>> GetChatHistoryAsync(int conversationId)
        {
            // get only 20 messages for context
            var messages = await _unitOfWork.ChatMessages.GetQueryable()
                .Where(m => m.conversationId == conversationId && m.role != "error")
                .OrderByDescending(m => m.sentAt)
                .Take(20)
                .OrderBy(m => m.sentAt) 
                .AsNoTracking()
                .ToListAsync();

            return messages.Select(m => new GeminiContent
            {
                Role = m.role.Trim(), // "user" or "model"
                Parts = new List<GeminiPart> { new GeminiPart { Text = m.content } }
            }).ToList();
        }

        private async Task<string> GetDatabaseContextAsync(string userMessage)
        {
            // Logic RAG (Retrieval-Augmented Generation)
            // TODO: UPGRADE THIS FOR BETTER INFORMATION
            var keywords = new[] { "trại hè", "camp", "giá", "lịch trình", "Đà Lạt", "Nha Trang" };

            if (keywords.Any(k => userMessage.ToLower().Contains(k)))
            {
                var camps = await _unitOfWork.Camps.GetQueryable()
                    .Where(c => c.status == "Active") // only active camp
                    .Take(5) 
                    .ToListAsync();

                var contextBuilder = new StringBuilder();
                contextBuilder.AppendLine("Thông tin tham khảo từ database (Chỉ dùng thông tin này để trả lời):");
                foreach (var camp in camps)
                {
                    // use camelCase from c$ models
                    contextBuilder.AppendLine($"- Tên Trại: {camp.name}, Giá: {camp.price}, Bắt đầu: {camp.startDate}, Kết thúc: {camp.endDate}, Địa điểm: {camp.locationId}.");
                }
                return contextBuilder.ToString();
            }
            return null; 
        }

        private async Task<string> CallGeminiApiAsync(string contextPrompt, List<GeminiContent> history)
        {
            var userMessage = history.Last();
            // merge into context so we del it
            history.RemoveAt(history.Count - 1);

            // build System Prompt (AI role)
            var systemInstruction = new GeminiContent
            {
                Role = "system", // 'system' role or first user role
                Parts = new List<GeminiPart> { new GeminiPart { Text = "Bạn là một trợ lý ảo thân thiện của hệ thống trại hè CampEase. Hãy trả lời ngắn gọn, lịch sự bằng tiếng Việt." } }
            };

            // context (RAG) and user last message
            // improve gemini focusness on db
            var fullUserMessage = new GeminiContent
            {
                Role = "user",
                Parts = new List<GeminiPart>
                {
                    new GeminiPart { Text = $"{contextPrompt}\n\nCâu hỏi của khách: {userMessage.Parts[0].Text}" }
                }
            };

            // build payload
            var payload = new GeminiRequestPayload
            {
                SystemInstruction = systemInstruction,
                Contents = history // history (last message deleted)
            };
            payload.Contents.Add(fullUserMessage); // add last message ( used RAG)

            // call api
            string requestUrl = $"{_geminiApiSettings.ModelName}:generateContent?key={_geminiApiSettings.ApiKey}";

            // make sure HttpClient has BaseAddress set in constructor)
            var response = await _httpClient.PostAsJsonAsync(requestUrl, payload, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error calling Gemini API: {response.StatusCode} - {errorContent}");
            }

            // read response
            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponsePayload>(_jsonOptions);
            var textResponse = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            return string.IsNullOrEmpty(textResponse) ? "Xin lỗi, tôi không thể tìm thấy câu trả lời." : textResponse;
        }
    }
}

