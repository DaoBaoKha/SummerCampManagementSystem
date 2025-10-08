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

// add appsettings.json with optional: true
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// google application credential for GCP Secret Manager
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);


// local variables for secrets
string connectionString;
string jwtKey;
string jwtIssuer;
string jwtAudience;
string emailSmtp;
int emailPort;
string emailSenderName;
string emailSenderEmail;
string emailPass;

// if its production, get from GCP Secret Manager
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

        // Apply to configuration runtime
        builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
        builder.Configuration["Jwt:Key"] = jwtKey;
        builder.Configuration["Jwt:Issuer"] = jwtIssuer;
        builder.Configuration["Jwt:Audience"] = jwtAudience;

        builder.Configuration["EmailSetting:SmtpServer"] = emailSmtp;
        builder.Configuration["EmailSetting:Port"] = emailPort.ToString();
        builder.Configuration["EmailSetting:SenderName"] = emailSenderName;
        builder.Configuration["EmailSetting:SenderEmail"] = emailSenderEmail;
        builder.Configuration["EmailSetting:Password"] = emailPass;

        Console.WriteLine("Secrets loaded from GCP.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Cannot load secrets from GCP: {ex.Message}");
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    }
}
else
{
    // local development - get from appsettings.json
    Console.WriteLine("Running in Development mode - using local connection string.");
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

// configure connection string for DbContext
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

// DI
builder.Services.AddScoped<ICamperGroupService, CamperGroupService>();
builder.Services.AddScoped<ICamperGroupRepository, CamperGroupRepository>();
builder.Services.AddScoped<ICampService, CampService>();
builder.Services.AddScoped<ICampRepository, CampRepository>();
builder.Services.AddScoped<ICampTypeService, CampTypeService>();
builder.Services.AddScoped<ICampTypeRepository, CampTypeRepository>();

//builder.Services.AddScoped<IParentCamperRepository, ParentCamperRepository>();
//builder.Services.AddScoped<IParentCamperService, ParentCamperService>();

//builder.Services.AddScoped<ICamperRepository, CamperRepository>();
//builder.Services.AddScoped<ICamperService, CamperService>();

//builder.Services.AddScoped<ICamperGuardianRepository, CamperGuardianService>();
//builder.Services.AddScoped<ICamperGuardianService, CamperGuardianService>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleTypeService, VehicleTypeService>();
builder.Services.AddScoped<IVehicleTypeRepository, VehicleTypeRepository>();

builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddDbContext<CampEaseDatabaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



// Helper
builder.Services.AddScoped<IValidationService, ValidationService>();

// Email service
builder.Services.Configure<EmailSetting>(builder.Configuration.GetSection("EmailSetting"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddMemoryCache();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
});

// database context
builder.Services.AddDbContext<CampEaseDatabaseContext>(options =>
    options.UseSqlServer(connectionString)
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

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
    });

// swagger
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
