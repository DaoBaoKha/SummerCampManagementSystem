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

        // determine if user message is personal intent or general intent
        private bool DetermineIntent(string userMessage)
        {
            var lowerMessage = userMessage.ToLower();
            
            // keywords indicating user asking about their own data (personal)
            var personalKeywords = new[] {
                "con tôi", "bé nhà tôi", "lịch trình của con",
                "đăng ký của tôi", "sức khỏe của bé", "camper của tôi",
                "con của tôi", "bé của tôi", "con mình", "bé mình",
                "trại của con", "camp của bé",
                // keywords for camper/student
                "trại viên của tôi", "trại viên nhà tôi", "trại viên con tôi",
                "học viên của tôi", "học viên nhà tôi", "các bé",
                "trại viên mình", "học viên mình",
                // keywords for schedule/camper name queries
                "lịch của", "camper", "schedule của", "hoạt động của",
                "lịch trình", "lịch học", "lịch hoạt động",
                // keywords for camp/program queries
                "chương trình của", "hội trại của"
            };

            if (personalKeywords.Any(k => lowerMessage.Contains(k)))
            {
                return true; // personal intent
            }
            return false; // general intent
        }

        // extract price range from user message (e.g. "dưới 5 triệu", "từ 2 đến 4 triệu")
        // returns (minPrice, maxPrice) in VND. null means no limit
        private (decimal? minPrice, decimal? maxPrice) ExtractPriceRange(string userMessage)
        {
            var lowerMessage = userMessage.ToLower();
            decimal? minPrice = null;
            decimal? maxPrice = null;

            // pattern: "dưới X triệu" or "X triệu trở xuống"
            var underMatch = System.Text.RegularExpressions.Regex.Match(lowerMessage, @"dưới\s+(\d+)\s*triệu|under\s+(\d+)\s*million|(\d+)\s*triệu\s*trở\s*xuống");
            if (underMatch.Success)
            {
                var priceStr = underMatch.Groups[1].Value != "" ? underMatch.Groups[1].Value : 
                               underMatch.Groups[2].Value != "" ? underMatch.Groups[2].Value : 
                               underMatch.Groups[3].Value;
                if (decimal.TryParse(priceStr, out var price))
                {
                    maxPrice = price * 1_000_000; // convert triệu to VND
                }
                return (minPrice, maxPrice);
            }

            // pattern: "trên X triệu" or "X triệu trở lên"
            var aboveMatch = System.Text.RegularExpressions.Regex.Match(lowerMessage, @"trên\s+(\d+)\s*triệu|above\s+(\d+)\s*million|(\d+)\s*triệu\s*trở\s*lên");
            if (aboveMatch.Success)
            {
                var priceStr = aboveMatch.Groups[1].Value != "" ? aboveMatch.Groups[1].Value : 
                               aboveMatch.Groups[2].Value != "" ? aboveMatch.Groups[2].Value : 
                               aboveMatch.Groups[3].Value;
                if (decimal.TryParse(priceStr, out var price))
                {
                    minPrice = price * 1_000_000; // convert triệu to VND
                }
                return (minPrice, maxPrice);
            }

            // pattern: "từ X đến Y triệu" or "khoảng X-Y triệu"
            var rangeMatch = System.Text.RegularExpressions.Regex.Match(lowerMessage, @"từ\s+(\d+)\s*đến\s+(\d+)\s*triệu|khoảng\s+(\d+)\s*-\s*(\d+)\s*triệu|(\d+)\s*-\s*(\d+)\s*triệu");
            if (rangeMatch.Success)
            {
                var minStr = rangeMatch.Groups[1].Value != "" ? rangeMatch.Groups[1].Value : 
                             rangeMatch.Groups[3].Value != "" ? rangeMatch.Groups[3].Value : 
                             rangeMatch.Groups[5].Value;
                var maxStr = rangeMatch.Groups[2].Value != "" ? rangeMatch.Groups[2].Value : 
                             rangeMatch.Groups[4].Value != "" ? rangeMatch.Groups[4].Value : 
                             rangeMatch.Groups[6].Value;
                if (decimal.TryParse(minStr, out var min) && decimal.TryParse(maxStr, out var max))
                {
                    minPrice = min * 1_000_000; // convert triệu to VND
                    maxPrice = max * 1_000_000;
                }
                return (minPrice, maxPrice);
            }

            // pattern: "khoảng X triệu" (use ±20% range)
            var aroundMatch = System.Text.RegularExpressions.Regex.Match(lowerMessage, @"khoảng\s+(\d+)\s*triệu|(\d+)\s*triệu\s*gì\s*đó");
            if (aroundMatch.Success)
            {
                var priceStr = aroundMatch.Groups[1].Value != "" ? aroundMatch.Groups[1].Value : aroundMatch.Groups[2].Value;
                if (decimal.TryParse(priceStr, out var price))
                {
                    var priceVnd = price * 1_000_000;
                    minPrice = priceVnd * 0.8m; // -20%
                    maxPrice = priceVnd * 1.2m; // +20%
                }
                return (minPrice, maxPrice);
            }

            // pattern: "giá rẻ" (cheap, under 3 million)
            if (lowerMessage.Contains("giá rẻ") || lowerMessage.Contains("rẻ nhất") || lowerMessage.Contains("phải chăng"))
            {
                maxPrice = 3_000_000;
                return (minPrice, maxPrice);
            }

            // pattern: "giá cao" (expensive, above 5 million)
            if (lowerMessage.Contains("giá cao") || lowerMessage.Contains("cao cấp") || lowerMessage.Contains("sang trọng"))
            {
                minPrice = 5_000_000;
                return (minPrice, maxPrice);
            }

            return (minPrice, maxPrice); // no price constraint found
        }

        // get relevant context from database based on user message and userId
        private async Task<string> GetDatabaseContextAsync(string userMessage, int userId, bool isPersonalIntent)
        {
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("Thông tin tham khảo TỪ DATABASE (Bắt buộc dùng):");

            // always include FAQ for both personal and general questions
            var faqs = await _unitOfWork.FAQs.GetAllAsync();
            if (faqs != null && faqs.Any())
            {
                contextBuilder.AppendLine("[Câu hỏi thường gặp (FAQ)]:");
                foreach (var faq in faqs)
                {
                    contextBuilder.AppendLine($"Q: {faq.question}");
                    contextBuilder.AppendLine($"A: {faq.answer}");
                }
                contextBuilder.AppendLine();
            }

            // user asking personal questions about their campers
            if (isPersonalIntent)
            {
                // get User (Parent) info
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user != null)
                {
                    contextBuilder.AppendLine($"[Thông tin User]: Tên Phụ huynh: {user.firstName} {user.lastName}.");
                }

                // get User's children (Campers) from ParentCamper table
                var parentCamperLinks = await _unitOfWork.ParentCampers.GetQueryable()
                    .Include(pc => pc.camper)
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

                // get Registration and Camp info based on camperIds
                var activeRegStatuses = new List<string> {
                    RegistrationStatus.Confirmed.ToString()
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

                contextBuilder.AppendLine("[Thông tin Đăng ký & Trạng thái của con]:");
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

                        // add detailed camp link with campId
                        string campDetailLink = $"https://summer-camp-web-seven.vercel.app/camp/{camp.campId}";
                        string campDuration = $"{camp.startDate:dd/MM/yyyy} - {camp.endDate:dd/MM/yyyy}";
                        contextBuilder.AppendLine($"- Bé {camperName} đã đăng ký trại: {camp.name}");
                        contextBuilder.AppendLine($"  - Thời gian trái: {campDuration}");
                        contextBuilder.AppendLine($"  - Giá: {campPriceInfo}");
                        contextBuilder.AppendLine($"  - Trạng thái đăng ký: {rc.status} (Đơn: {rc.registration.status})");
                        contextBuilder.AppendLine($"  - Chi tiết: {campDetailLink}");
                    }
                }

                // add camper activity schedule - smart camp detection
                contextBuilder.AppendLine("\n[Lịch trình hoạt động trong trại]:");
                
                // check if user is asking about specific camp
                var lowerUserMessage = userMessage.ToLower();
                RegistrationCamper targetCamp = null;
                
                // try to find camp by name in user message
                foreach (var rc in activeCamperRegistrations)
                {
                    var campName = rc.registration?.camp?.name?.ToLower();
                    if (!string.IsNullOrEmpty(campName) && lowerUserMessage.Contains(campName))
                    {
                        targetCamp = rc;
                        break;
                    }
                    
                    // also check camper name
                    var camperName = parentCamperLinks.FirstOrDefault(pcl => pcl.camperId == rc.camperId)?.camper.camperName?.ToLower();
                    if (!string.IsNullOrEmpty(camperName) && lowerUserMessage.Contains(camperName))
                    {
                        targetCamp = rc;
                        break;
                    }
                }
                
                // if user asks for "gần đây nhất" or "mới nhất", get most recent camp
                if (targetCamp == null && (lowerUserMessage.Contains("gần đây") || lowerUserMessage.Contains("mới nhất") || 
                    lowerUserMessage.Contains("hiện tại") || lowerUserMessage.Contains("đang diễn ra")))
                {
                    var now = DateTime.UtcNow;
                    targetCamp = activeCamperRegistrations
                        .Where(rc => rc.registration?.camp?.startDate != null)
                        .OrderBy(rc => Math.Abs((rc.registration.camp.startDate.Value - now).TotalDays))
                        .FirstOrDefault();
                }
                
                // if we found a specific camp, show only that camp's schedule
                if (targetCamp != null)
                {
                    string camperName = parentCamperLinks.FirstOrDefault(pcl => pcl.camperId == targetCamp.camperId)?.camper.camperName ?? $"Camper ID {targetCamp.camperId}";
                    var campId = targetCamp.registration?.camp?.campId;
                    var campName = targetCamp.registration?.camp?.name;
                    
                    if (campId.HasValue)
                    {
                        var today = DateTime.UtcNow;
                        
                        // get ALL activities in the camp
                        var campActivities = await _unitOfWork.ActivitySchedules.GetQueryable()
                            .Include(asch => asch.activity)
                            .Include(asch => asch.location)
                            .Where(asch => asch.activity.campId == campId.Value &&
                                          asch.startTime.HasValue &&
                                          asch.startTime >= today.AddDays(-7))
                            .OrderBy(asch => asch.startTime)
                            .Take(10)
                            .AsNoTracking()
                            .ToListAsync();
                        
                        if (campActivities.Any())
                        {
                            contextBuilder.AppendLine($"Lịch trình trại {campName} (cho bé {camperName}):");
                            
                            foreach (var schedule in campActivities)
                            {
                                if (schedule.activity != null)
                                {
                                    var activity = schedule.activity;
                                    string locationInfo = schedule.location != null ? schedule.location.address : "Chưa xác định";
                                    string timeInfo = schedule.startTime.HasValue ? 
                                        $"{schedule.startTime.Value:dd/MM/yyyy HH:mm}" : "Chưa xác định";
                                    string endTimeInfo = schedule.endTime.HasValue ?
                                        $" - {schedule.endTime.Value:HH:mm}" : "";
                                    string activityType = schedule.isOptional == true ? "(Tự chọn)" : "(Bắt buộc)";
                                    
                                    contextBuilder.AppendLine($"  • {activity.name} {activityType}: {timeInfo}{endTimeInfo} tại {locationInfo}");
                                }
                            }
                        }
                        else
                        {
                            contextBuilder.AppendLine($"Hiện tại chưa có lịch trình hoạt động nào gần đây cho trại {campName}.");
                        }
                    }
                }
                else
                {
                    // no specific camp detected - list all camps and ask
                    bool hasMultipleCamps = activeCamperRegistrations.Count > 1;
                    
                    if (hasMultipleCamps)
                    {
                        contextBuilder.AppendLine("Các trại viên đã đăng ký các trại sau:");
                        
                        foreach (var rc in activeCamperRegistrations)
                        {
                            string camperName = parentCamperLinks.FirstOrDefault(pcl => pcl.camperId == rc.camperId)?.camper.camperName ?? $"Camper ID {rc.camperId}";
                            var campName = rc.registration?.camp?.name;
                            var campId = rc.registration?.camp?.campId;
                            
                            if (campName != null && campId.HasValue)
                            {
                                string campDetailLink = $"https://summer-camp-web-seven.vercel.app/camp/{campId}";
                                contextBuilder.AppendLine($"- {camperName}: Trại {campName} (Link: {campDetailLink})");
                            }
                        }
                        
                        contextBuilder.AppendLine("\nAnh/chị muốn xem lịch trình của trại nào? Vui lòng cho biết tên trại hoặc tên trại viên.");
                    }
                    else if (activeCamperRegistrations.Count == 1)
                    {
                        // only one camp - show schedule directly
                        var rc = activeCamperRegistrations.First();
                        string camperName = parentCamperLinks.FirstOrDefault(pcl => pcl.camperId == rc.camperId)?.camper.camperName ?? $"Camper ID {rc.camperId}";
                        var campId = rc.registration?.camp?.campId;
                        var campName = rc.registration?.camp?.name;
                        
                        if (campId.HasValue)
                        {
                            var today = DateTime.UtcNow;
                            
                            var campActivities = await _unitOfWork.ActivitySchedules.GetQueryable()
                                .Include(asch => asch.activity)
                                .Include(asch => asch.location)
                                .Where(asch => asch.activity.campId == campId.Value &&
                                              asch.startTime.HasValue &&
                                              asch.startTime >= today.AddDays(-7))
                                .OrderBy(asch => asch.startTime)
                                .Take(10)
                                .AsNoTracking()
                                .ToListAsync();
                            
                            if (campActivities.Any())
                            {
                                contextBuilder.AppendLine($"Lịch trình trại {campName} (cho bé {camperName}):");
                                
                                foreach (var schedule in campActivities)
                                {
                                    if (schedule.activity != null)
                                    {
                                        var activity = schedule.activity;
                                        string locationInfo = schedule.location != null ? schedule.location.address : "Chưa xác định";
                                        string timeInfo = schedule.startTime.HasValue ? 
                                            $"{schedule.startTime.Value:dd/MM/yyyy HH:mm}" : "Chưa xác định";
                                        string endTimeInfo = schedule.endTime.HasValue ?
                                            $" - {schedule.endTime.Value:HH:mm}" : "";
                                        string activityType = schedule.isOptional == true ? "(Tự chọn)" : "(Bắt buộc)";
                                        
                                        contextBuilder.AppendLine($"  • {activity.name} {activityType}: {timeInfo}{endTimeInfo} tại {locationInfo}");
                                    }
                                }
                            }
                            else
                            {
                                contextBuilder.AppendLine($"Hiện tại chưa có lịch trình hoạt động nào gần đây cho trại {campName}.");
                            }
                        }
                    }
                    else
                    {
                        contextBuilder.AppendLine("Hiện tại không có trại viên nào đã đăng ký trại.");
                    }
                }

                // suggest suitable camps based on camper age
                contextBuilder.AppendLine("\n[Gợi ý trại phù hợp theo độ tuổi]:");
                
                // get camps that are open for registration
                var openForRegCampStatuses = new List<string> {
                    CampStatus.OpenForRegistration.ToString()
                };
                
                var availableCamps = await _unitOfWork.Camps.GetQueryable()
                    .Include(c => c.promotion)
                    .Include(c => c.campType)
                    .Where(c => openForRegCampStatuses.Contains(c.status) && 
                                c.minAge.HasValue && 
                                c.maxAge.HasValue)
                    .OrderBy(c => c.startDate)
                    .AsNoTracking()
                    .ToListAsync();

                bool foundSuitableCamp = false;
                foreach (var link in parentCamperLinks)
                {
                    if (link.camper != null && link.camper.dob.HasValue)
                    {
                        // calculate camper age
                        var currentDate = DateTime.Today;
                        var dobDateTime = link.camper.dob.Value.ToDateTime(new TimeOnly());
                        int age = currentDate.Year - dobDateTime.Year;
                        if (dobDateTime.Date > currentDate.AddYears(-age)) age--;

                        // find camps suitable for this camper's age
                        var suitableCamps = availableCamps
                            .Where(c => age >= c.minAge.Value && age <= c.maxAge.Value)
                            .Take(3)
                            .ToList();

                        if (suitableCamps.Any())
                        {
                            foundSuitableCamp = true;
                            contextBuilder.AppendLine($"- Bé {link.camper.camperName} ({age} tuổi) phù hợp với các trại:");
                            foreach (var camp in suitableCamps)
                            {
                                // xây dựng thông tin giá với khuyến mãi
                                string campPriceInfo = $"{camp.price:N0} VND";
                                if (camp.promotion != null && camp.promotion.percent.HasValue && camp.price.HasValue)
                                {
                                    decimal discount = camp.price.Value * (camp.promotion.percent.Value / 100);
                                    if (camp.promotion.maxDiscountAmount.HasValue && discount > camp.promotion.maxDiscountAmount.Value)
                                    {
                                        discount = camp.promotion.maxDiscountAmount.Value;
                                    }
                                    decimal finalPrice = camp.price.Value - discount;
                                    campPriceInfo = $"{finalPrice:N0} VND (giảm {camp.promotion.percent.Value}%)";
                                }

                                string campTypeInfo = camp.campType != null ? camp.campType.name : "N/A";
                                string campDetailLink = $"https://summer-camp-web-seven.vercel.app/camp/{camp.campId}";
                                contextBuilder.AppendLine($"  • {camp.name} ({campTypeInfo}, độ tuổi: {camp.minAge}-{camp.maxAge}, giá: {campPriceInfo})");
                                contextBuilder.AppendLine($"    Link: {campDetailLink}");
                            }
                        }
                    }
                }

                if (!foundSuitableCamp)
                {
                    // fallback 1: recommend camps where their campers are currently enrolled
                    var enrolledCamps = activeCamperRegistrations
                        .Where(rc => rc.registration?.camp != null)
                        .Select(rc => rc.registration.camp)
                        .Distinct()
                        .ToList();

                    if (enrolledCamps.Any())
                    {
                        contextBuilder.AppendLine("- Các bé hiện đang tham gia các trại sau, anh/chị có thể tham khảo:");
                        foreach (var camp in enrolledCamps.Take(3))
                        {
                            string campDetailLink = $"https://summer-camp-web-seven.vercel.app/camp/{camp.campId}";
                            contextBuilder.AppendLine($"  • {camp.name} - {campDetailLink}");
                        }
                    }
                    else
                    {
                        // fallback 2: recommend all open camps
                        if (availableCamps.Any())
                        {
                            contextBuilder.AppendLine("- Hiện tại chưa tìm thấy trại phù hợp với độ tuổi cụ thể, nhưng các trại sau đang mở đăng ký:");
                            foreach (var camp in availableCamps.Take(3))
                            {
                                string campPriceInfo = camp.price.HasValue ? $"{camp.price:N0} VND" : "Liên hệ";
                                string campTypeInfo = camp.campType != null ? camp.campType.name : "N/A";
                                string campDetailLink = $"https://summer-camp-web-seven.vercel.app/camp/{camp.campId}";
                                contextBuilder.AppendLine($"  • {camp.name} ({campTypeInfo}, độ tuổi: {camp.minAge}-{camp.maxAge}, giá: {campPriceInfo})");
                                contextBuilder.AppendLine($"    Link: {campDetailLink}");
                            }
                        }
                        else
                        {
                            contextBuilder.AppendLine("- Hiện tại không tìm thấy trại phù hợp với độ tuổi của các bé. Vui lòng xem tất cả trại tại: https://summer-camp-web-seven.vercel.app/camp");
                        }
                    }
                }

                // add registration and management guide for user
                contextBuilder.AppendLine("\n[Hướng dẫn quản lý và đăng ký]:");
                contextBuilder.AppendLine("- Để xem danh sách con em của bạn, truy cập: https://summer-camp-web-seven.vercel.app/user/my-campers");
                contextBuilder.AppendLine("- Để xem tất cả đơn đăng ký, truy cập: https://summer-camp-web-seven.vercel.app/user/my-registrations");
                contextBuilder.AppendLine("- Để xem chi tiết tất cả các trại hè và đăng ký thêm, truy cập: https://summer-camp-web-seven.vercel.app/camp");
                contextBuilder.AppendLine("- Để quản lý thông tin đăng ký của con, vui lòng đăng nhập vào hệ thống.");


            }

            // user asking general questions about camps
            else
            {
                var publicCampStatuses = new List<string> {
                    CampStatus.Published.ToString(),
                    CampStatus.OpenForRegistration.ToString(),
                    CampStatus.InProgress.ToString()
                };

                // get all public camps with full details
                var allPublicCampsQuery = _unitOfWork.Camps.GetQueryable()
                    .Include(c => c.promotion)
                    .Include(c => c.location)
                    .Include(c => c.campType)
                    .Where(c => publicCampStatuses.Contains(c.status));

                // extract price range from query and filter camps
                var (minPrice, maxPrice) = ExtractPriceRange(userMessage);
                if (minPrice.HasValue || maxPrice.HasValue)
                {
                    allPublicCampsQuery = allPublicCampsQuery.Where(c => c.price.HasValue);
                    if (minPrice.HasValue)
                    {
                        allPublicCampsQuery = allPublicCampsQuery.Where(c => c.price >= minPrice.Value);
                    }
                    if (maxPrice.HasValue)
                    {
                        allPublicCampsQuery = allPublicCampsQuery.Where(c => c.price <= maxPrice.Value);
                    }
                }

                var allPublicCamps = await allPublicCampsQuery
                    .OrderBy(c => c.startDate)
                    .AsNoTracking()
                    .ToListAsync();

                var lowerUserMessage = userMessage.ToLower();

                // search for specific camp name in user's message
                var foundCamp = allPublicCamps.FirstOrDefault(c => lowerUserMessage.Contains(c.name.ToLower()));

                var contextBuilderPublic = new StringBuilder();
                contextBuilderPublic.AppendLine("Thông tin tham khảo từ database (Chỉ dùng thông tin này để trả lời):");

                // SCENARIO B.1: specific camp found
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

                    string campDetailLink = $"https://summer-camp-web-seven.vercel.app/camp/{foundCamp.campId}";
                    contextBuilderPublic.AppendLine($"- Tên Trại: {foundCamp.name}, {campPriceInfo}, Bắt đầu: {foundCamp.startDate:dd/MM/yyyy}, Trạng thái: {foundCamp.status}.");
                    contextBuilderPublic.AppendLine($"- Xem chi tiết: {campDetailLink}");
                }
                // scenario b.2: no specific camp, check for general keywords 
                else
                {
                    // keywords for price, activities, and registration
                    var generalKeywords = new[] { 
                        "trại hè", "camp", "giá", "lịch trình", "hoạt động", "đang mở", "active",
                        "bao nhiêu tiền", "chi phí", "học phí", "phí", "đăng ký",
                        "khoảng", "triệu", "rẻ", "cao", "price",
                        "chương trình", "hội trại"
                    };

                    if (generalKeywords.Any(k => lowerUserMessage.Contains(k)))
                    {
                        var campsToShow = allPublicCamps.Take(5);
                        if (campsToShow.Any())
                        {
                            // show price range info if available
                            if (minPrice.HasValue || maxPrice.HasValue)
                            {
                                string priceRangeInfo = "";
                                if (minPrice.HasValue && maxPrice.HasValue)
                                    priceRangeInfo = $" (khoảng giá {minPrice.Value / 1_000_000:N0}-{maxPrice.Value / 1_000_000:N0} triệu VND)";
                                else if (maxPrice.HasValue)
                                    priceRangeInfo = $" (dưới {maxPrice.Value / 1_000_000:N0} triệu VND)";
                                else if (minPrice.HasValue)
                                    priceRangeInfo = $" (trên {minPrice.Value / 1_000_000:N0} triệu VND)";
                                contextBuilderPublic.AppendLine($"[Danh sách các trại đang hoạt động/mở đăng ký{priceRangeInfo}]:");
                            }
                            else
                            {
                                contextBuilderPublic.AppendLine("[Danh sách các trại đang hoạt động/mở đăng ký]:");
                            }
                            foreach (var camp in campsToShow)
                            {
                                // xây dựng thông tin giá với khuyến mãi
                                string campPriceInfo = $"Giá: {camp.price:N0} VND";
                                if (camp.promotion != null && camp.promotion.percent.HasValue && camp.price.HasValue)
                                {
                                    decimal discount = camp.price.Value * (camp.promotion.percent.Value / 100);
                                    if (camp.promotion.maxDiscountAmount.HasValue && discount > camp.promotion.maxDiscountAmount.Value)
                                    {
                                        discount = camp.promotion.maxDiscountAmount.Value;
                                    }
                                    decimal finalPrice = camp.price.Value - discount;
                                    campPriceInfo = $"Giá gốc: {camp.price:N0} VND, KM: {camp.promotion.percent.Value}%, Giá cuối: {finalPrice:N0} VND";
                                }
                                
                                // add location and camp type details
                                string locationInfo = camp.location != null ? camp.location.address : "N/A";
                                string campTypeInfo = camp.campType != null ? camp.campType.name : "N/A";
                                string campDetailLink = $"https://summer-camp-web-seven.vercel.app/camp/{camp.campId}";
                               
                                contextBuilderPublic.AppendLine($"- Tên: {camp.name}");
                                contextBuilderPublic.AppendLine($"  Loại: {campTypeInfo}");
                                contextBuilderPublic.AppendLine($"  Địa điểm: {locationInfo}");
                                contextBuilderPublic.AppendLine($"  Thời gian: {camp.startDate:dd/MM/yyyy} - {camp.endDate:dd/MM/yyyy}");
                                contextBuilderPublic.AppendLine($"  {campPriceInfo}");
                                contextBuilderPublic.AppendLine($"  Trạng thái: {camp.status}");
                                contextBuilderPublic.AppendLine($"  Chi tiết: {campDetailLink}");
                                contextBuilderPublic.AppendLine();
                            }

                            // registration guide with website link
                            contextBuilderPublic.AppendLine("\n[Hướng dẫn đăng ký]:");
                            contextBuilderPublic.AppendLine("- Để xem chi tiết tất cả các trại hè, truy cập: https://summer-camp-web-seven.vercel.app/camp");
                            contextBuilderPublic.AppendLine("- Để đăng ký trại hè, vui lòng chọn trại ưng ý và làm theo hướng dẫn trên website.");
                            contextBuilderPublic.AppendLine("- Hotline hỗ trợ: [Thêm số hotline nếu có]");
                        }
                        else
                        {
                            // no camps found matching the criteria
                            if (minPrice.HasValue || maxPrice.HasValue)
                            {
                                string priceRangeMsg = "";
                                if (minPrice.HasValue && maxPrice.HasValue)
                                    priceRangeMsg = $" trong khoảng giá {minPrice.Value / 1_000_000:N0}-{maxPrice.Value / 1_000_000:N0} triệu VND";
                                else if (maxPrice.HasValue)
                                    priceRangeMsg = $" dưới {maxPrice.Value / 1_000_000:N0} triệu VND";
                                else if (minPrice.HasValue)
                                    priceRangeMsg = $" trên {minPrice.Value / 1_000_000:N0} triệu VND";
                                return $"Thông tin tham khảo: Hiện tại không có trại hè nào{priceRangeMsg}. Vui lòng xem tất cả trại tại: https://summer-camp-web-seven.vercel.app/camp";
                            }
                            return "Thông tin tham khảo: Hiện tại không có trại hè nào (Published, OpenForRegistration, InProgress) trong hệ thống. Vui lòng xem thêm tại: https://summer-camp-web-seven.vercel.app/camp";
                        }
                    }
                    else
                    {
                        // scenario b.3: no specific keywords, let AI answer from general knowledge
                        return null;
                    }
                }
                return contextBuilderPublic.ToString();
            }

            return contextBuilder.ToString();
        }

        // call Gemini API with dynamic system prompt
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