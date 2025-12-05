using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require user authentication to get Python API token
    public class PythonTokenController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PythonTokenController> _logger;

        public PythonTokenController(IConfiguration configuration, ILogger<PythonTokenController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Generate a JWT token for authenticating with the Python Face Recognition API
        /// This endpoint is for testing purposes - in production, tokens are generated internally by PythonAiService
        /// </summary>
        /// <returns>JWT token that can be used to authenticate with Python API</returns>
        [HttpGet("generate")]
        public IActionResult GeneratePythonApiToken()
        {
            try
            {
                var secretKey = _configuration["PythonJWT:SecretKey"]
                    ?? throw new InvalidOperationException("PythonJWT:SecretKey is not configured");
                var issuer = _configuration["PythonJWT:Issuer"] ?? "SummerCampBackend";
                var audience = _configuration["PythonJWT:Audience"] ?? "face-recognition-api";
                var expirationMinutes = int.TryParse(_configuration["PythonJWT:ExpirationMinutes"], out var exp) ? exp : 60;

                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, User.Identity?.Name ?? "test-user"),
                    new Claim("service", "SummerCampManagementSystem"),
                    new Claim("userId", User.FindFirst("id")?.Value ?? "test")
                };

                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                    signingCredentials: credentials
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                _logger.LogInformation("Generated Python API token for user {User}", User.Identity?.Name);

                return Ok(new
                {
                    token = tokenString,
                    tokenType = "Bearer",
                    expiresIn = expirationMinutes * 60, // seconds
                    expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                    issuer = issuer,
                    audience = audience,
                    usage = "Use this token in the Authorization header: Bearer <token>"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Python API token");
                return StatusCode(500, new { error = "Failed to generate token", details = ex.Message });
            }
        }

        /// <summary>
        /// Verify if a token is valid for the Python API
        /// </summary>
        [HttpPost("verify")]
        public IActionResult VerifyPythonApiToken([FromBody] TokenVerificationRequest request)
        {
            try
            {
                var secretKey = _configuration["PythonJWT:SecretKey"]
                    ?? throw new InvalidOperationException("PythonJWT:SecretKey is not configured");
                var issuer = _configuration["PythonJWT:Issuer"] ?? "SummerCampBackend";
                var audience = _configuration["PythonJWT:Audience"] ?? "face-recognition-api";

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(request.Token, validationParameters, out var validatedToken);

                return Ok(new
                {
                    valid = true,
                    issuer = validatedToken.Issuer,
                    claims = principal.Claims.Select(c => new { c.Type, c.Value }),
                    expiresAt = validatedToken.ValidTo
                });
            }
            catch (Exception ex)
            {
                return Ok(new { valid = false, error = ex.Message });
            }
        }
    }

    public class TokenVerificationRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
