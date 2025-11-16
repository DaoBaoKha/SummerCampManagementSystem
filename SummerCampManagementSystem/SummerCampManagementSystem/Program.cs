using Google.Cloud.SecretManager.V1;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Net.payOS;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
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
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Default local values
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")?.Trim() ?? "";
string jwtKey = builder.Configuration["Jwt:Key"]?.Trim() ?? "";
string jwtIssuer = builder.Configuration["Jwt:Issuer"]?.Trim() ?? "";
string jwtAudience = builder.Configuration["Jwt:Audience"]?.Trim() ?? "";

// Email settings
string emailSmtp = builder.Configuration["EmailSetting:SmtpServer"]?.Trim() ?? "smtp.example.com";
int emailPort = int.TryParse(builder.Configuration["EmailSetting:Port"], out var port) ? port : 587;
string emailSenderName = builder.Configuration["EmailSetting:SenderName"]?.Trim() ?? "CampEase";
string emailSenderEmail = builder.Configuration["EmailSetting:SenderEmail"]?.Trim() ?? "no-reply@campease.com";
string emailPass = builder.Configuration["EmailSetting:Password"]?.Trim() ?? "password123";

// PayOS settings
string payosClientId = builder.Configuration["PayOS:ClientId"]?.Trim() ?? "";
string payosApiKey = builder.Configuration["PayOS:ApiKey"]?.Trim() ?? "";
string payosChecksumKey = builder.Configuration["PayOS:ChecksumKey"]?.Trim() ?? "";
string payosReturnUrl = builder.Configuration["PayOS:ReturnUrl"]?.Trim() ?? "";
string payosCancelUrl = builder.Configuration["PayOS:CancelUrl"]?.Trim() ?? "";

// Gemini settings (local default)
string geminiApiKey = builder.Configuration["GeminiApi:ApiKey"]?.Trim() ?? "";
string geminiBaseUrl = builder.Configuration["GeminiApi:ApiBaseUrl"]?.Trim() ?? "";
string geminiModelName = builder.Configuration["GeminiApi:ModelName"]?.Trim() ?? "";

// Mobile PayOS settings
string payosMobileReturnUrl = builder.Configuration["PayOS:MobileReturnUrl"]?.Trim() ?? "";
string payosMobileCancelUrl = builder.Configuration["PayOS:MobileCancelUrl"]?.Trim() ?? "";
string apiBaseUrl = builder.Configuration["ApiBaseUrl"]?.Trim() ?? "";

//Supabase settings
string supabaseUrl = builder.Configuration["Supabase:Url"]?.Trim() ?? "";
string supabaseKey = builder.Configuration["Supabase:Key"]?.Trim() ?? "";

// Load GCP secrets if Production
if (builder.Environment.IsProduction())
{
    try
    {
        var client = SecretManagerServiceClient.Create();
        string projectId = "campease-473401";

        connectionString = client.AccessSecretVersion(new SecretVersionName(projectId, "db-connection-string", "latest"))
                                 .Payload.Data.ToStringUtf8().Trim();
        jwtKey = client.AccessSecretVersion(new SecretVersionName(projectId, "jwt-secret", "latest"))
            .Payload.Data.ToStringUtf8().Trim();
        jwtIssuer = client.AccessSecretVersion(new SecretVersionName(projectId, "jwt-issuer", "latest"))
            .Payload.Data.ToStringUtf8().Trim();
        jwtAudience = client.AccessSecretVersion(new SecretVersionName(projectId, "jwt-audience", "latest"))
            .Payload.Data.ToStringUtf8().Trim();

        // email
        emailSmtp = client.AccessSecretVersion(new SecretVersionName(projectId, "email-smtpserver", "latest"))
                  .Payload.Data.ToStringUtf8().Trim().Trim('"');

        emailPort = int.Parse(client.AccessSecretVersion(new SecretVersionName(projectId, "email-port", "latest"))
                                       .Payload.Data.ToStringUtf8().Trim().Trim('"'));

        emailSenderName = client.AccessSecretVersion(new SecretVersionName(projectId, "email-sendername", "latest"))
                                .Payload.Data.ToStringUtf8().Trim().Trim('"');

        emailSenderEmail = client.AccessSecretVersion(new SecretVersionName(projectId, "email-senderemail", "latest"))
                                 .Payload.Data.ToStringUtf8().Trim().Trim('"');

        emailPass = client.AccessSecretVersion(new SecretVersionName(projectId, "email-pass", "latest"))
                          .Payload.Data.ToStringUtf8().Trim().Trim('"');


        // payos
        payosClientId = client.AccessSecretVersion(new SecretVersionName(projectId, "payos-client-id", "latest"))
            .Payload.Data.ToStringUtf8().Trim();
        payosApiKey = client.AccessSecretVersion(new SecretVersionName(projectId, "payos-api-key", "latest"))
            .Payload.Data.ToStringUtf8().Trim();
        payosChecksumKey = client.AccessSecretVersion(new SecretVersionName(projectId, "payos-checksum-key", "latest"))
            .Payload.Data.ToStringUtf8().Trim();
        payosReturnUrl = client.AccessSecretVersion(new SecretVersionName(projectId, "payos-return-url", "latest"))
            .Payload.Data.ToStringUtf8().Trim();
        payosCancelUrl = client.AccessSecretVersion(new SecretVersionName(projectId, "payos-cancel-url", "latest"))
            .Payload.Data.ToStringUtf8().Trim();


        // Mobile PayOS
        payosMobileReturnUrl = client.AccessSecretVersion(new SecretVersionName(projectId, "payos-mobile-return-url", "latest"))
            .Payload.Data.ToStringUtf8().Trim();
        payosMobileCancelUrl = client.AccessSecretVersion(new SecretVersionName(projectId, "payos-mobile-cancel-url", "latest"))
            .Payload.Data.ToStringUtf8().Trim();
        apiBaseUrl = client.AccessSecretVersion(new SecretVersionName(projectId, "api-base-url", "latest"))
            .Payload.Data.ToStringUtf8().Trim();

        // gemini
        geminiApiKey = client.AccessSecretVersion(new SecretVersionName(projectId, "gemini-api-key", "latest"))
            .Payload.Data.ToStringUtf8().Trim();
        geminiBaseUrl = client.AccessSecretVersion(new SecretVersionName(projectId, "gemini-base-url", "latest"))
            .Payload.Data.ToStringUtf8().Trim();
        geminiModelName = client.AccessSecretVersion(new SecretVersionName(projectId, "gemini-model-name", "latest"))
            .Payload.Data.ToStringUtf8().Trim();

        //Supabase
        supabaseUrl = client
       .AccessSecretVersion(new SecretVersionName(projectId, "supabase-url", "latest"))
       .Payload.Data.ToStringUtf8().Trim();

        supabaseKey = client
            .AccessSecretVersion(new SecretVersionName(projectId, "supabase-key", "latest"))
            .Payload.Data.ToStringUtf8().Trim();



        var inMemorySettings = new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", connectionString},
            {"Jwt:Key", jwtKey},
            {"Jwt:Issuer", jwtIssuer},
            {"Jwt:Audience", jwtAudience},
            {"EmailSetting:SmtpServer", emailSmtp},
            {"EmailSetting:Port", emailPort.ToString()},
            {"EmailSetting:SenderName", emailSenderName},
            {"EmailSetting:SenderEmail", emailSenderEmail},
            {"EmailSetting:Password", emailPass},

            // PayOS
            {"PayOS:ClientId", payosClientId},
            {"PayOS:ApiKey", payosApiKey},
            {"PayOS:ChecksumKey", payosChecksumKey},
            {"PayOS:ReturnUrl", payosReturnUrl},
            {"PayOS:CancelUrl", payosCancelUrl},

            // Mobile PayOS
            {"PayOS:MobileReturnUrl", payosMobileReturnUrl},
            {"PayOS:MobileCancelUrl", payosMobileCancelUrl},
            {"ApiBaseUrl", apiBaseUrl},


            // Gemini
            {"GeminiApi:ApiKey", geminiApiKey},
            {"GeminiApi:ApiBaseUrl", geminiBaseUrl},
            {"GeminiApi:ModelName", geminiModelName},

            // Supabase
            {"Supabase:Url", supabaseUrl},
            {"Supabase:Key", supabaseKey}


        };
        builder.Configuration.AddInMemoryCollection(inMemorySettings);

        Console.WriteLine("Secrets loaded from GCP successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Cannot load secrets from GCP: {ex.Message}");
        Console.WriteLine("Using local appsettings.json values.");
    }
}
else
{
    Console.WriteLine($"Running in {builder.Environment.EnvironmentName} mode - using local appsettings.json");
}

// Configure DbContext
builder.Services.AddDbContext<CampEaseDatabaseContext>(options =>
    options.UseSqlServer(connectionString)
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));



// singleton services
builder.Services.AddSingleton(sp => new PayOS(payosClientId, payosApiKey, payosChecksumKey));

var supabase = new Supabase.Client(supabaseUrl, supabaseKey);
await supabase.InitializeAsync();

builder.Services.AddSingleton(supabase);


// DI
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();

builder.Services.AddScoped<ICamperGroupService, CamperGroupService>();
builder.Services.AddScoped<ICamperGroupRepository, CamperGroupRepository>();
builder.Services.AddScoped<ICampService, CampService>();
builder.Services.AddScoped<ICampRepository, CampRepository>();
builder.Services.AddScoped<ICampTypeService, CampTypeService>();
builder.Services.AddScoped<ICampTypeRepository, CampTypeRepository>();

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IPromotionTypeRepository, PromotionTypeRepository>();
builder.Services.AddScoped<IPromotionTypeService, PromotionTypeService>();

builder.Services.AddScoped<ICamperRepository, CamperRepository>();
builder.Services.AddScoped<ICamperService, CamperService>();
builder.Services.AddScoped<ICampStaffAssignmentRepository, CampStaffAssignmentRepository>();
builder.Services.AddScoped<ICampStaffAssignmentService, CampStaffAssignmentService>();


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

builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransportScheduleRepository, TransportScheduleRepository>();

builder.Services.AddScoped<IAttendanceLogRepository, AttendanceLogRepository>();
builder.Services.AddScoped<IAttendanceLogService, AttendanceLogService>();

builder.Services.AddScoped<ICamperAccomodationRepository, CamperAccomodationRepository>();
builder.Services.AddScoped<IRegistrationCamperRepository, RegistrationCamperRepository>();

builder.Services.AddScoped<IParentCamperRepository, ParentCamperRepository>();

builder.Services.AddScoped<IAccommodationRepository, AccommodationRepository>();
builder.Services.AddScoped<IAccommodationService, AccommodationService>();

builder.Services.AddScoped<ICamperGuardianRepository, CamperGuardianRepository>();

builder.Services.AddScoped<IStaffService, StaffService>();

builder.Services.AddScoped<IUploadSupabaseService, UploadSupabaseService>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

builder.Services.AddDbContext<CampEaseDatabaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



// Helper
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextService, UserContextService>();

// Chat service
builder.Services.AddScoped<IChatConversationRepository, ChatConversationRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IPromptTemplateService, PromptTemplateService>();
builder.Services.AddHttpClient();

// Gemini API Setting
var geminiApiSettings = new GeminiApiSettings
{
    ApiKey = geminiApiKey,
    ApiBaseUrl = geminiBaseUrl,
    ModelName = geminiModelName
};

builder.Services.Configure<GeminiApiSettings>(opts =>
{
    opts.ApiKey = geminiApiSettings.ApiKey;
    opts.ApiBaseUrl = geminiApiSettings.ApiBaseUrl;
    opts.ModelName = geminiApiSettings.ModelName;
});


// Email service
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddMemoryCache();

// Email Setting
var emailSetting = new EmailSetting
{
    SmtpServer = emailSmtp,
    Port = emailPort,
    SenderName = emailSenderName,
    SenderEmail = emailSenderEmail,
    Password = emailPass
};

builder.Services.Configure<EmailSetting>(opts =>
{
    opts.SmtpServer = emailSetting.SmtpServer;
    opts.Port = emailSetting.Port;
    opts.SenderName = emailSetting.SenderName;
    opts.SenderEmail = emailSetting.SenderEmail;
    opts.Password = emailSetting.Password;
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
});



// jwt auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                // set response
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
    });

// swagger
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
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });

    option.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

// cors
app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());

// pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// global error handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/json";

        var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var ex = exceptionFeature?.Error;

        int statusCode = ex switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status409Conflict, 
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new
        {
            status = statusCode,
            error = statusCode switch
            {
                404 => "Not Found",
                400 => "Bad Request",
                409 => "Conflict",
                _ => "Internal Server Error"
            },
            message = ex?.Message,
            path = context.Request.Path
        });
    });
});


app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
