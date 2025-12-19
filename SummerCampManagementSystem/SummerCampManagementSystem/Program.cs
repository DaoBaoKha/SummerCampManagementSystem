using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Net.payOS;
using SummerCampManagementSystem.API.Hubs;
using SummerCampManagementSystem.API.Middlewares;
using SummerCampManagementSystem.API.Services;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.HostedServices;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Jobs;
using SummerCampManagementSystem.BLL.Mappings;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.Core.Config;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.Repositories.Repository;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
    EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
});

// Load configuration 
// auto load appsettings.json and environment variables
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Fail fast check essential configurations
var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var jwtKeyCheck = builder.Configuration["Jwt:Key"];

if (string.IsNullOrEmpty(dbConnectionString) || string.IsNullOrEmpty(jwtKeyCheck))
{
    Console.WriteLine("CRITICAL CONFIG ERROR: Database Connection String or JWT Key is missing.");
    throw new InvalidOperationException("CRITICAL CONFIG ERROR: Missing essential configuration. Check Cloud Run Secrets/Env Variables.");
}

// Configure DbContext with No Tracking as default
builder.Services.AddDbContext<CampEaseDatabaseContext>(options =>
    options.UseSqlServer(dbConnectionString)
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

// Singleton Services Configuration

// PayOS
builder.Services.AddSingleton(sp => new PayOS(
    builder.Configuration["PayOS:ClientId"] ?? "",
    builder.Configuration["PayOS:ApiKey"] ?? "",
    builder.Configuration["PayOS:ChecksumKey"] ?? ""
));

// Supabase - Use ServiceRoleKey for admin operations (storage uploads, etc.)
var supabaseUrl = builder.Configuration["Supabase:Url"] ?? "";
var supabaseServiceRoleKey = builder.Configuration["Supabase:Key"] ?? "";
var supabase = new Supabase.Client(supabaseUrl, supabaseServiceRoleKey);
await supabase.InitializeAsync();
builder.Services.AddSingleton(supabase);

// Configure Options pattern for settings
// Map configuration sections to strongly typed classes
builder.Services.Configure<AppSetting>(builder.Configuration.GetSection("AppSetting"));

builder.Services.Configure<GeminiApiSettings>(opts =>
{
    opts.ApiKey = builder.Configuration["GeminiApi:ApiKey"] ?? "";
    opts.ApiBaseUrl = builder.Configuration["GeminiApi:ApiBaseUrl"] ?? "";
    opts.ModelName = builder.Configuration["GeminiApi:ModelName"] ?? "";
});

builder.Services.Configure<EmailSetting>(opts =>
{
    opts.SmtpServer = builder.Configuration["EmailSetting:SmtpServer"] ?? "";
    opts.Port = int.TryParse(builder.Configuration["EmailSetting:Port"], out var p) ? p : 587;
    opts.SenderName = builder.Configuration["EmailSetting:SenderName"] ?? "";
    opts.SenderEmail = builder.Configuration["EmailSetting:SenderEmail"] ?? "";
    opts.Password = builder.Configuration["EmailSetting:Password"] ?? "";
});

// Override Python AI Service URL based on environment
if (builder.Environment.IsProduction())
{
    // Production: Use Render deployment
    builder.Configuration["AIServiceSettings:BaseUrl"] = "https://capstone-faceattendanceai-production.up.railway.app/";
}
else
{
    // Development: Use localhost (can be overridden in appsettings.Development.json)
    builder.Configuration["AIServiceSettings:BaseUrl"] ??= "http://localhost:5000";
}

// DI
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddScoped<IBankUserService, BankUserService>();
builder.Services.AddScoped<IBankUserRepository, BankUserRepository>();
builder.Services.AddScoped<ICamperGroupRepository, CamperGroupRepository>();
builder.Services.AddScoped<ICampService, CampService>();
builder.Services.AddScoped<ICampRepository, CampRepository>();
builder.Services.AddScoped<ICampTypeService, CampTypeService>();
builder.Services.AddScoped<ICampTypeRepository, CampTypeRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPayOSService, PayOSService>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IPromotionTypeRepository, PromotionTypeRepository>();
builder.Services.AddScoped<IPromotionTypeService, PromotionTypeService>();
builder.Services.AddScoped<ICamperRepository, CamperRepository>();
builder.Services.AddScoped<ICamperService, CamperService>();
builder.Services.AddScoped<ICampStaffAssignmentRepository, CampStaffAssignmentRepository>();
builder.Services.AddScoped<ICampStaffAssignmentService, CampStaffAssignmentService>();
builder.Services.AddScoped<ICamperTransportRepository, CamperTransportRepository>();
builder.Services.AddScoped<ICamperTransportService, CamperTransportService>();
builder.Services.AddScoped<ICamperGroupRepository, CamperGroupRepository>();
builder.Services.AddScoped<ICamperGroupService, CamperGroupService>();
builder.Services.AddScoped<ICamperAccommodationService, CamperAccommodationService>();
builder.Services.AddScoped<IGuardianRepository, GuardianRepository>();
builder.Services.AddScoped<IGuardianService, GuardianService>();
builder.Services.AddScoped<IHealthRecordRepository, HealthRecordRepository>();
builder.Services.AddScoped<IAccommodationTypeRepository, AccommodationTypeRepository>();
builder.Services.AddScoped<IAccommodationTypeService, AccommodationTypeService>();
builder.Services.AddScoped<IAlbumPhotoFaceRepository, AlbumPhotoFaceRepository>();
builder.Services.AddScoped<IAlbumPhotoRepository, AlbumPhotoRepository>();
builder.Services.AddScoped<IAlbumPhotoService, AlbumPhotoService>();
builder.Services.AddScoped<IAlbumRepository, AlbumRepository>();
builder.Services.AddScoped<IAlbumService, AlbumService>();
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IActivityScheduleRepository, ActivityScheduleRepository>();
builder.Services.AddScoped<IActivityScheduleService, ActivityScheduleService>();
builder.Services.AddScoped<ICamperActivityRepository, CamperActivityRepository>();
builder.Services.AddScoped<ICamperActivityService, CamperActivityService>();
builder.Services.AddScoped<IDriverRepository, DriverRepository>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<IGroupActivityRepository, GroupActivityRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<IUserAccountRepository, UserAccountRepository>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleTypeService, VehicleTypeService>();
builder.Services.AddScoped<IVehicleTypeRepository, VehicleTypeRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();
builder.Services.AddScoped<IRegistrationOptionalActivityService, RegistrationOptionalActivityService>();
builder.Services.AddScoped<IRegistrationOptionalActivityRepository, RegistrationOptionalActivityRepository>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<IRouteStopService, RouteStopService>();
builder.Services.AddScoped<IRouteStopRepository, RouteStopRepository>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IGroupActivityRepository, GroupActivityRepository>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransportScheduleRepository, TransportScheduleRepository>();
builder.Services.AddScoped<ITransportScheduleService, TransportScheduleService>();
builder.Services.AddScoped<IAttendanceLogRepository, AttendanceLogRepository>();
builder.Services.AddScoped<IAttendanceLogService, AttendanceLogService>();
builder.Services.AddScoped<ICamperAccommodationRepository, CamperAccommodationRepository>();
builder.Services.AddScoped<IRegistrationCamperRepository, RegistrationCamperRepository>();
builder.Services.AddScoped<IRegistrationCamperService, RegistrationCamperService>();
builder.Services.AddScoped<IParentCamperRepository, ParentCamperRepository>();
builder.Services.AddScoped<IAccommodationRepository, AccommodationRepository>();
builder.Services.AddScoped<IAccommodationService, AccommodationService>();
builder.Services.AddScoped<ICamperGuardianRepository, CamperGuardianRepository>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<ILiveStreamRepository, LiveStreamRepository>();
builder.Services.AddScoped<ILiveStreamService, LiveStreamService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<IRegistrationCancelRepository, RegistrationCancelRepository>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IFAQService, FAQService>();
builder.Services.AddScoped<IFAQRepository, FAQRepository>();
builder.Services.AddScoped<IAttendanceFolderService, AttendanceFolderService>();
builder.Services.AddScoped<IAccommodationActivityRepository, AccommodationActivityRepository>();
builder.Services.AddScoped<ITransportStaffAssignmentRepository, TransportStaffAssignmentRepository>();
builder.Services.AddScoped<ITransportStaffAssignmentService, TransportStaffAssignmentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<ICampReportExportService, CampReportExportService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Attendance webhook services (new - for real-time updates)
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();

// Chat service (DI for Hub)
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IChatRoomUserRepository, ChatRoomUserRepository>();
builder.Services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
builder.Services.AddScoped<IChatRoomService, ChatRoomService>(); // service for real-time chat
builder.Services.AddScoped<IChatNotifier, SignalRChatNotifier>(); // interface for BLL to call signalR

// Configure HttpClient with extended timeout for Python AI Service
builder.Services.AddHttpClient("PythonAiClient", client =>
{
    var aiBaseUrl = builder.Configuration.GetValue<string>("AIServiceSettings:BaseUrl", "http://localhost:5000");
    var aiTimeout = builder.Configuration.GetValue<int>("AIServiceSettings:Timeout", 300);

    client.BaseAddress = new Uri(aiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(aiTimeout);

    Console.WriteLine($"[PythonAiClient] HttpClient configured - BaseUrl: {aiBaseUrl}, Timeout: {aiTimeout}s");
});
builder.Services.AddHttpClient();

// Register Hangfire jobs
builder.Services.AddScoped<AttendanceFolderCreationJob>();
builder.Services.AddScoped<PreloadCampFaceDbJob>();
builder.Services.AddScoped<CleanupCampFaceDbJob>();

// Register Python AI service
builder.Services.AddScoped<IPythonAiService, PythonAiService>();

// Register hosted services
builder.Services.AddHostedService<CampBackgroundInitializer>();

builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

// Helper
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IUploadSupabaseService, UploadSupabaseService>();

// Email service
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddMemoryCache();

// Chat service
builder.Services.AddScoped<IChatConversationRepository, ChatConversationRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IPromptTemplateService, PromptTemplateService>();
builder.Services.AddHttpClient();

// Forwarded Headers Middleware
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Controllers and JSON Options
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;

    // Add custom DateTime converter for Vietnam timezone
    options.JsonSerializerOptions.Converters.Add(new VietnamDateTimeConverter());

    // add custom timeOnly converter for Vietnam timezone
    options.JsonSerializerOptions.Converters.Add(new VietnamTimeOnlyConverter());
});

// JWT Authentication - Support both user tokens and service tokens
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Accept multiple issuers (user backend + Python service)
            ValidIssuers = new[]
            {
                builder.Configuration["Jwt:Issuer"] ?? "SummerCampBackend",
                builder.Configuration["Jwt:ServiceIssuer"] ?? "PythonAiService"
            },
            // Accept multiple audiences
            ValidAudiences = new[]
            {
                builder.Configuration["Jwt:Audience"] ?? "SummerCampBackend",
                builder.Configuration["Jwt:ServiceAudience"] ?? "SummerCampBackend"
            },
            // Use IssuerSigningKeyResolver to validate with the appropriate key
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                var jwtToken = securityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
                var issuer = jwtToken?.Issuer;

                // If token is from Python service, use service secret
                if (issuer == builder.Configuration["Jwt:ServiceIssuer"])
                {
                    var serviceSecret = builder.Configuration["Jwt:ServiceSecret"] ?? "";
                    return new[] { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(serviceSecret)) };
                }

                // Otherwise use regular user JWT key
                var userSecret = builder.Configuration["Jwt:Key"] ?? "";
                return new[] { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(userSecret)) };
            }
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = MediaTypeNames.Application.Json;

                var responseBody = new
                {
                    status = 401,
                    error = "Unauthorized",
                    message = "Bạn cần đăng nhập để thực hiện hành động này. Vui lòng cung cấp token hợp lệ."
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(responseBody));
            },

            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var responseBody = new
                {
                    status = 403,
                    error = "Forbidden",
                    Message = "You do not have permission to access this resource"
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(responseBody));
            }
        };
    })
    .AddGoogle(opts =>
    {
        opts.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        opts.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    });

// Swagger Configuration
var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.DescribeAllParametersInCamelCase();
    option.ResolveConflictingActions(conf => conf.First());
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type=ReferenceType.SecurityScheme, Id="Bearer" } },
            new string[]{}
        }
    });
    option.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// Hangfire Configuration
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseIgnoredAssemblyVersionTypeResolver() // Add this to handle version mismatches
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true,
        PrepareSchemaIfNecessary = true // Ensure schema is created/updated
    }));

// Add Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.ServerName = "CampEaseServer";
    options.WorkerCount = 5; // Number of background workers
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
});

// Add Redis Config
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddSignalR()
        .AddStackExchangeRedis(redisConnectionString, options =>
        {
            options.Configuration.ChannelPrefix = "CampEase_Chat";
        })
        .AddJsonProtocol(options =>
        {
            // convert to VietNamTime when send to client
            options.PayloadSerializerOptions.Converters.Add(new VietnamDateTimeConverter());
            options.PayloadSerializerOptions.Converters.Add(new VietnamTimeOnlyConverter());
            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
}
else
{
    // fallback for local if no redis
    builder.Services.AddSignalR()
        .AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.Converters.Add(new VietnamDateTimeConverter());
            options.PayloadSerializerOptions.Converters.Add(new VietnamTimeOnlyConverter());
            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
}

var app = builder.Build();

// Middleware Pipeline
app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// map hub path
app.MapHub<ChatRoomHub>("/hubs/chat");
app.MapHub<SummerCampManagementSystem.BLL.Hubs.AttendanceHub>("/hubs/attendance");

// Configure Hangfire Dashboard with authorization
var dashboardEnabled = app.Configuration.GetValue<bool>("Hangfire:DashboardEnabled", true);
if (dashboardEnabled)
{
    var dashboardOptions = new DashboardOptions
    {
        Authorization = new[]
        {
            new HangfireAuthorizationFilter(
                app.Services.GetRequiredService<ILogger<HangfireAuthorizationFilter>>(),
                app.Configuration
            )
        },
        DashboardTitle = "CampEase Background Jobs"
    };
    app.UseHangfireDashboard("/hangfire", dashboardOptions);
}

// Register recurring jobs
RecurringJob.AddOrUpdate<PreloadCampFaceDbJob>(
    "preload-camp-face-db-daily",
    job => job.ExecuteAsync(),
    "0 19 * * *", // Run daily at 19:00 UTC (02:00 UTC+7)
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Utc
    });

app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();
app.Run();
