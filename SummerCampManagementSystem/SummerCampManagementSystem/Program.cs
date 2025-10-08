using Google.Cloud.SecretManager.V1;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.Core.Config;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.Repositories.Repository;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System.Text;
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
string emailSmtp = builder.Configuration["EmailSetting:SmtpServer"]?.Trim() ?? "smtp.example.com";
int emailPort = int.TryParse(builder.Configuration["EmailSetting:Port"], out var port) ? port : 587;
string emailSenderName = builder.Configuration["EmailSetting:SenderName"]?.Trim() ?? "CampEase";
string emailSenderEmail = builder.Configuration["EmailSetting:SenderEmail"]?.Trim() ?? "no-reply@campease.com";
string emailPass = builder.Configuration["EmailSetting:Password"]?.Trim() ?? "password123";

// Load GCP secrets if Production
if (builder.Environment.IsProduction())
{
    try
    {
        var client = SecretManagerServiceClient.Create();
        string projectId = "campease-473401";

        connectionString = client.AccessSecretVersion(new SecretVersionName(projectId, "db-connection-string", "latest"))
                                 .Payload.Data.ToStringUtf8().Trim();
        jwtKey = client.AccessSecretVersion(new SecretVersionName(projectId, "jwt-secret", "latest")).Payload.Data.ToStringUtf8().Trim();
        jwtIssuer = client.AccessSecretVersion(new SecretVersionName(projectId, "jwt-issuer", "latest")).Payload.Data.ToStringUtf8().Trim();
        jwtAudience = client.AccessSecretVersion(new SecretVersionName(projectId, "jwt-audience", "latest")).Payload.Data.ToStringUtf8().Trim();
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
            {"EmailSetting:Password", emailPass}
        };
        builder.Configuration.AddInMemoryCollection(inMemorySettings);

        Console.WriteLine("✓ Secrets loaded from GCP successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Cannot load secrets from GCP: {ex.Message}");
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

// Dependency Injection - Services & Repositories
builder.Services.AddScoped<ICamperGroupService, CamperGroupService>();
builder.Services.AddScoped<ICamperGroupRepository, CamperGroupRepository>();
builder.Services.AddScoped<ICampService, CampService>();
builder.Services.AddScoped<ICampRepository, CampRepository>();
builder.Services.AddScoped<ICampTypeService, CampTypeService>();
builder.Services.AddScoped<ICampTypeRepository, CampTypeRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleTypeService, VehicleTypeService>();
builder.Services.AddScoped<IVehicleTypeRepository, VehicleTypeRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IValidationService, ValidationService>();

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

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Controllers & JSON
builder.Services.AddControllers().AddJsonOptions(opt =>
{
    opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.DescribeAllParametersInCamelCase();
    opt.ResolveConflictingActions(c => c.First());
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{}
        }
    });
});

// Build App
var app = builder.Build();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// Global Error Handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>()?.Error;

        Console.WriteLine($"ERROR at {context.Request.Path}: {ex?.Message}");
        Console.WriteLine($"Stack trace: {ex?.StackTrace}");

        int statusCode = ex switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        await context.Response.WriteAsJsonAsync(new
        {
            status = statusCode,
            error = statusCode switch
            {
                404 => "Not Found",
                400 => "Bad Request",
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
