using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
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
        private readonly IPromptTemplateService _promptTemplateService; 

        public ChatService(
            IUnitOfWork unitOfWork,
            IUserContextService userContextService,
            IHttpClientFactory httpClientFactory,
            IOptions<GeminiApiSettings> geminiApiSettings,
            IPromptTemplateService promptTemplateService)
        {
            _unitOfWork = unitOfWork;
            _userContextService = userContextService;
            _promptTemplateService = promptTemplateService; 

            _httpClient = httpClientFactory.CreateClient("GeminiApiClient");
            _geminiApiSettings = geminiApiSettings.Value;
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

            var conversation = await GetOrCreateConversationAsync(request.ConversationId, userId, request.Message);

            var userMessage = new ChatMessage
            {
                conversationId = conversation.chatConversationId,
                role = "user",
                content = request.Message,
                sentAt = DateTime.UtcNow
            };
            await _unitOfWork.ChatMessages.CreateAsync(userMessage);
            await _unitOfWork.CommitAsync();

            // logic to determine intent
            bool isPersonalIntent = DetermineIntent(request.Message);
            string systemPromptText;
            if (isPersonalIntent)
            {
                // use personalized prompt
                systemPromptText = _promptTemplateService.GetTemplate("System_PersonalAssistant");
            }
            else
            {
                // use general prompt
                systemPromptText = _promptTemplateService.GetTemplate("System_General");
            }



            // get userid context from database
            var contextPrompt = await GetDatabaseContextAsync(request.Message, userId, isPersonalIntent);
            

            var history = await GetChatHistoryAsync(conversation.chatConversationId);

            string modelResponseText;
            try
            {
                // call Gemini API
                modelResponseText = await CallGeminiApiAsync(contextPrompt, history, systemPromptText);
                
            }
            catch (Exception ex)
            {
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

        /// <summary>
        /// new - determine if user message is personal intent or general intent
        /// </summary>
        private bool DetermineIntent(string userMessage)
        {
            var lowerMessage = userMessage.ToLower();
            var personalKeywords = new[] {
                "con tôi", "bé nhà tôi", "lịch trình của con",
                "đăng ký của tôi", "sức khỏe của bé", "camper của tôi"
            };

            if (personalKeywords.Any(k => lowerMessage.Contains(k)))
            {
                return true; // this is personal intent
            }
            return false; // this is general intent
        }

        /// <summary>
        /// take user message and userId to get relevant context from database
        /// </summary>
        private async Task<string> GetDatabaseContextAsync(string userMessage, int userId, bool isPersonalIntent)
        {
            var contextBuilder = new StringBuilder();
            // This message MUST remain Vietnamese, as it is part of the prompt.
            contextBuilder.AppendLine("Thông tin tham khảo TỪ DATABASE (Bắt buộc dùng):");

            // user ask personal questions about their campers
            if (isPersonalIntent)
            {
                // Get User (Parent) info
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user != null)
                {
                    contextBuilder.AppendLine($"[Thông tin User]: Tên Phụ huynh: {user.firstName} {user.lastName}.");
                }

                // Get User's children (Campers) from ParentCamper table
                var parentCamperLinks = await _unitOfWork.ParentCampers.GetQueryable()
                    .Include(pc => pc.camper) // Include Camper info
                    .Where(pc => pc.parentId == userId)
                    .AsNoTracking()
                    .ToListAsync();

                if (!parentCamperLinks.Any() || parentCamperLinks.All(pc => pc.camper == null))
                {
                    return "Thông tin tham khảo: User này đã đăng nhập, nhưng hệ thống không tìm thấy (hoặc chưa liên kết) họ với camper (con) nào.";
                }

                var camperIds = parentCamperLinks.Select(pc => pc.camperId.Value).ToList();

                contextBuilder.AppendLine("[Thông tin Con cái của User]:");
                foreach (var link in parentCamperLinks)
                {
                    if (link.camper != null)
                        contextBuilder.AppendLine($"  - Con: {link.camper.camperName} (ID Camper: {link.camperId}). Mối quan hệ: {link.relationship}.");
                }

                // Get Registration and Camp info BASED ON camperIds
                var activeRegStatuses = new List<string> {
                    RegistrationStatus.Confirmed.ToString(),
                    RegistrationStatus.OnGoing.ToString()
                };

                var activeCamperRegistrations = await _unitOfWork.RegistrationCampers.GetQueryable()
                    .Include(rc => rc.registration).ThenInclude(r => r.camp).ThenInclude(c => c.promotion)
                    .Where(rc => camperIds.Contains(rc.camperId) &&
                                 activeRegStatuses.Contains(rc.registration.status))
                    .AsNoTracking()
                    .ToListAsync();

                if (!activeCamperRegistrations.Any())
                {
                    return contextBuilder.ToString() + "\nThông tin thêm: User này có liên kết với camper, nhưng các camper đó hiện không có đăng ký (Registration) nào ở trạng thái (Confirmed) hoặc (OnGoing).";
                }

                contextBuilder.AppendLine("[Thông tin Trại hè & Trạng thái của con]:");
                foreach (var rc in activeCamperRegistrations)
                {
                    string camperName = parentCamperLinks.FirstOrDefault(pcl => pcl.camperId == rc.camperId)?.camper.camperName ?? $"Camper ID {rc.camperId}";

                    if (rc.registration != null && rc.registration.camp != null)
                    {
                        var camp = rc.registration.camp;

                        string campPriceInfo = $"Giá gốc: {camp.price:N0} VND";
                        if (camp.promotion != null && camp.promotion.percent.HasValue && camp.price.HasValue)
                        {
                            decimal discount = camp.price.Value * (camp.promotion.percent.Value / 100);
                            if (camp.promotion.maxDiscountAmount.HasValue && discount > camp.promotion.maxDiscountAmount.Value)
                            {
                                discount = camp.promotion.maxDiscountAmount.Value;
                            }
                            decimal finalPrice = camp.price.Value - discount;
                            campPriceInfo = $"Giá gốc: {camp.price:N0} VND, Khuyến mãi: {camp.promotion.name} ({camp.promotion.percent.Value}%), Giá cuối: {finalPrice:N0} VND";
                        }

                        contextBuilder.AppendLine($"- Bé {camperName} (ID Camper: {rc.camperId}) đang ở trại: {camp.name}. {campPriceInfo}");
                        contextBuilder.AppendLine($"  - Trạng thái của bé: {rc.status}. (Trạng thái đơn tổng: {rc.registration.status}).");
                    }
                }

                // 4. Get Schedules
                // (This section is still commented out per your last request)
                /* var today = DateTime.UtcNow.Date;
                var schedules = await _unitOfWork.ActivitySchedules...
                ...
                */
            }

            // user ask general questions about camps
            else
            {
                var publicCampStatuses = new List<string> {
                    CampStatus.Published.ToString(),
                    CampStatus.OpenForRegistration.ToString(),
                    CampStatus.InProgress.ToString()
                };

                // Get all public camps first (including promotion data)
                var allPublicCamps = await _unitOfWork.Camps.GetQueryable()
                    .Include(c => c.promotion) 
                    .Where(c => publicCampStatuses.Contains(c.status))
                    .AsNoTracking()
                    .ToListAsync();

                var lowerUserMessage = userMessage.ToLower();

                // Try to find a specific camp name in the user's message
                var foundCamp = allPublicCamps.FirstOrDefault(c => lowerUserMessage.Contains(c.name.ToLower()));

                var contextBuilderPublic = new StringBuilder();
                contextBuilderPublic.AppendLine("Thông tin tham khảo từ database (Chỉ dùng thông tin này để trả lời):");

                // SCENARIO B.1: Specific camp found
                // (e.g., "trại hè adventure discovery camp bao nhiêu tiền")
                if (foundCamp != null)
                {

                    string campPriceInfo = $"Giá: {foundCamp.price:N0} VND";
                    if (foundCamp.promotion != null && foundCamp.promotion.percent.HasValue && foundCamp.price.HasValue)
                    {
                        decimal discount = foundCamp.price.Value * (foundCamp.promotion.percent.Value / 100);
                        if (foundCamp.promotion.maxDiscountAmount.HasValue && discount > foundCamp.promotion.maxDiscountAmount.Value)
                        {
                            discount = foundCamp.promotion.maxDiscountAmount.Value;
                        }
                        decimal finalPrice = foundCamp.price.Value - discount;
                        campPriceInfo = $"Giá gốc: {foundCamp.price:N0} VND, Khuyến mãi: {foundCamp.promotion.name} ({foundCamp.promotion.percent.Value}%), Giá cuối: {finalPrice:N0} VND";
                    }

                    contextBuilderPublic.AppendLine($"- Tên Trại: {foundCamp.name}, {campPriceInfo}, Bắt đầu: {foundCamp.startDate:dd/MM/yyyy}, Trạng thái: {foundCamp.status}.");
                }
                // SCENARIO B.2: No specific camp, check for general keywords
                // (e.g., "có trại hè nào không?", "giá camp?")
                else
                {

                    var generalKeywords = new[] { "trại hè", "camp", "giá", "lịch trình" };

                    if (generalKeywords.Any(k => lowerUserMessage.Contains(k)))
                    {
                        var campsToShow = allPublicCamps.Take(5); // take top 5 camps
                        if (campsToShow.Any())
                        {
                            foreach (var camp in campsToShow)
                            {
                                // (Price logic is already included in the 'camp' object, no need to query again)
                                string campPriceInfo = $"Giá: {camp.price:N0} VND";
                                if (camp.promotion != null && camp.promotion.percent.HasValue && camp.price.HasValue)
                                {
                                    // (Duplicate logic, consider refactoring to a private method later)
                                    decimal discount = camp.price.Value * (camp.promotion.percent.Value / 100);
                                    if (camp.promotion.maxDiscountAmount.HasValue && discount > camp.promotion.maxDiscountAmount.Value)
                                    {
                                        discount = camp.promotion.maxDiscountAmount.Value;
                                    }
                                    decimal finalPrice = camp.price.Value - discount;
                                    campPriceInfo = $"Giá gốc: {camp.price:N0} VND, KM: {camp.promotion.percent.Value}%, Giá cuối: {finalPrice:N0} VND";
                                }
                                contextBuilderPublic.AppendLine($"- Tên Trại: {camp.name}, {campPriceInfo}, Bắt đầu: {camp.startDate:dd/MM/yyyy}, Trạng thái: {camp.status}.");
                            }
                        }
                        else
                        {
                            return "Thông tin tham khảo: Hiện tại không có trại hè nào (Published, OpenForRegistration, InProgress) trong hệ thống.";
                        }
                    }
                    else
                    {
                        // SCENARIO B.3: No specific camp, no general keywords
                        // (e.g., "xin chào") -> Let the AI answer from its own knowledge.
                        return null; // Return null to signal no DB context is needed.
                    }
                }
                return contextBuilderPublic.ToString();
            }

            return contextBuilder.ToString();
        }

        /// <summary>
        /// call Gemini API with dynamic system prompt
        /// </summary>
        private async Task<string> CallGeminiApiAsync(string contextPrompt, List<GeminiContent> history, string systemPromptText)
        {
            var userMessage = history.Last();
            history.RemoveAt(history.Count - 1);

            // use dynamic system prompt
            var systemInstruction = new GeminiContent
            {
                Role = "system",
                Parts = new List<GeminiPart> { new GeminiPart { Text = systemPromptText } }
            };

            var fullUserMessage = new GeminiContent
            {
                Role = "user",
                Parts = new List<GeminiPart>
                {
                    new GeminiPart { Text = $"{contextPrompt}\n\nCâu hỏi của khách: {userMessage.Parts[0].Text}" }
                }
            };

            var payload = new GeminiRequestPayload
            {
                SystemInstruction = systemInstruction,
                Contents = history
            };
            payload.Contents.Add(fullUserMessage);

            string requestUrl = $"{_geminiApiSettings.ModelName}:generateContent?key={_geminiApiSettings.ApiKey}";
            var response = await _httpClient.PostAsJsonAsync(requestUrl, payload, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error calling Gemini API: {response.StatusCode} - {errorContent}");
            }

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponsePayload>(_jsonOptions);
            var textResponse = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            return string.IsNullOrEmpty(textResponse) ? "Xin lỗi, tôi không thể tìm thấy câu trả lời." : textResponse;
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

            var messages = await _unitOfWork.ChatMessages.GetQueryable()
                .Where(m => m.conversationId == conversationId)
                .ToListAsync();

            _unitOfWork.ChatMessages.RemoveRange(messages);
            _unitOfWork.ChatConversations.RemoveAsync(conversation); // shouldnt use await here

            await _unitOfWork.CommitAsync();
        }

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

            var newConversation = new ChatConversation
            {
                userId = userId,
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
                role = role,
                content = message,
                sentAt = DateTime.UtcNow
            };
            await _unitOfWork.ChatMessages.CreateAsync(modelMessage);
            await _unitOfWork.CommitAsync();
            return modelMessage;
        }

        private async Task<List<GeminiContent>> GetChatHistoryAsync(int conversationId)
        {
            var messages = await _unitOfWork.ChatMessages.GetQueryable()
                .Where(m => m.conversationId == conversationId && m.role != "error")
                .OrderByDescending(m => m.sentAt)
                .Take(20)
                .OrderBy(m => m.sentAt)
                .AsNoTracking()
                .ToListAsync();

            return messages.Select(m => new GeminiContent
            {
                Role = m.role.Trim(),
                Parts = new List<GeminiPart> { new GeminiPart { Text = m.content } }
            }).ToList();
        }
    }
}