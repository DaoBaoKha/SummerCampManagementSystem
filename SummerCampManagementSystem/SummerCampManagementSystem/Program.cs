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

// add appsettings.json
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();


// Local variables with defaults from appsettings
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
string jwtKey = builder.Configuration["Jwt:Key"];
string jwtIssuer = builder.Configuration["Jwt:Issuer"];
string jwtAudience = builder.Configuration["Jwt:Audience"];
string emailSmtp = builder.Configuration["EmailSetting:SmtpServer"];
int emailPort = int.TryParse(builder.Configuration["EmailSetting:Port"], out var port) ? port : 587;
string emailSenderName = builder.Configuration["EmailSetting:SenderName"];
string emailSenderEmail = builder.Configuration["EmailSetting:SenderEmail"];
string emailPass = builder.Configuration["EmailSetting:Password"];


// Load secrets from GCP if Production
if (builder.Environment.IsProduction())
{
    try
    {
        var client = SecretManagerServiceClient.Create();
        string projectId = "campease-473401";

        // DB
        connectionString = client.AccessSecretVersion(new SecretVersionName(projectId, "db-connection-string", "latest"))
                                 .Payload.Data.ToStringUtf8();

        // JWT
        jwtKey = client.AccessSecretVersion(new SecretVersionName(projectId, "jwt-secret", "latest"))
                       .Payload.Data.ToStringUtf8();
        jwtIssuer = client.AccessSecretVersion(new SecretVersionName(projectId, "jwt-issuer", "latest"))
                          .Payload.Data.ToStringUtf8();
        jwtAudience = client.AccessSecretVersion(new SecretVersionName(projectId, "jwt-audience", "latest"))
                            .Payload.Data.ToStringUtf8();

        // Email
        emailSmtp = client.AccessSecretVersion(new SecretVersionName(projectId, "email-smtpserver", "latest"))
                          .Payload.Data.ToStringUtf8();
        emailPort = int.Parse(client.AccessSecretVersion(new SecretVersionName(projectId, "email-port", "latest"))
                                     .Payload.Data.ToStringUtf8());
        emailSenderName = client.AccessSecretVersion(new SecretVersionName(projectId, "email-sendername", "latest"))
                                .Payload.Data.ToStringUtf8();
        emailSenderEmail = client.AccessSecretVersion(new SecretVersionName(projectId, "email-senderemail", "latest"))
                                 .Payload.Data.ToStringUtf8();
        emailPass = client.AccessSecretVersion(new SecretVersionName(projectId, "email-pass", "latest"))
                          .Payload.Data.ToStringUtf8();

        Console.WriteLine("✓ Secrets loaded from GCP successfully.");
        Console.WriteLine($"   - Connection String: {connectionString?.Substring(0, Math.Min(30, connectionString.Length))}...");
        Console.WriteLine($"   - JWT Key length: {jwtKey?.Length ?? 0}");
        Console.WriteLine($"   - JWT Issuer: {jwtIssuer}");
        Console.WriteLine($"   - JWT Audience: {jwtAudience}");
        Console.WriteLine($"   - Email SMTP: {emailSmtp}");
        Console.WriteLine($"   - Email Port: {emailPort}");
        Console.WriteLine($"   - Email Sender Name: {emailSenderName}");
        Console.WriteLine($"   - Email Sender Email: {emailSenderEmail}");
        Console.WriteLine($"   - Email Password length: {emailPass?.Length ?? 0}");

        // **FIX: Update IConfiguration with GCP secrets**
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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Cannot load secrets from GCP: {ex.Message}");
        Console.WriteLine("Using default settings from appsettings.json");
    }
}
else
{
    Console.WriteLine($"Running in {builder.Environment.EnvironmentName} mode - using appsettings.json");
}


// Apply connection string for DbContext
builder.Services.AddDbContext<CampEaseDatabaseContext>(options =>
    options.UseSqlServer(connectionString)
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));


// Dependency Injection for services & repositories
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


builder.Services.AddMemoryCache();

// Configure EmailSetting - Log values before configuring
Console.WriteLine("Configuring EmailSetting:");
Console.WriteLine($"   - SmtpServer: {emailSmtp ?? "NULL"}");
Console.WriteLine($"   - Port: {emailPort}");
Console.WriteLine($"   - SenderName: {emailSenderName ?? "NULL"}");
Console.WriteLine($"   - SenderEmail: {emailSenderEmail ?? "NULL"}");
Console.WriteLine($"   - Password Length: {emailPass?.Length ?? 0}");

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

// Validate email settings
if (string.IsNullOrEmpty(emailSmtp) || string.IsNullOrEmpty(emailSenderEmail) || string.IsNullOrEmpty(emailPass))
{
    Console.WriteLine("WARNING: Email settings are incomplete!");
}

// Email service
builder.Services.AddScoped<IEmailService, EmailService>();


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


// Controllers & JSON options
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


// Build app & middleware
var app = builder.Build();

// CORS
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// Swagger
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Global error handler with better logging
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var ex = exceptionFeature?.Error;

        // **FIX: Log detailed error to console**
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