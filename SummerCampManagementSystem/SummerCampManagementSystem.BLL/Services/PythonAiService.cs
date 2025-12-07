using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.DTOs.AI;
using SummerCampManagementSystem.BLL.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace SummerCampManagementSystem.BLL.Services
{
    /// <summary>
    /// Service for communicating with Python Flask AI face recognition API
    /// </summary>
    public class PythonAiService : IPythonAiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PythonAiService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly int _timeoutSeconds;

        // JWT Configuration for Python API authentication
        private readonly string _jwtSecretKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpirationMinutes;

        public PythonAiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<PythonAiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _configuration = configuration;

            _baseUrl = configuration["AIServiceSettings:BaseUrl"] ?? "http://localhost:5000";
            _timeoutSeconds = int.TryParse(configuration["AIServiceSettings:Timeout"], out var timeout) ? timeout : 300;

            // Load JWT settings for Python API authentication (optional - only needed for background jobs)
            // For user requests, tokens are forwarded from the frontend
            _jwtSecretKey = configuration["Jwt:Key"] ?? "";
            _jwtIssuer = configuration["Jwt:Issuer"] ?? "SummerCampBackend";
            _jwtAudience = configuration["Jwt:Audience"] ?? "SummerCampBackend";

            // Try to read ExpirationMinutes, or convert ExpiryDays to minutes, or default to 60 minutes
            if (int.TryParse(configuration["Jwt:ExpirationMinutes"], out var expirationMinutes))
            {
                _jwtExpirationMinutes = expirationMinutes;
            }
            else if (int.TryParse(configuration["Jwt:ExpiryDays"], out var expiryDays))
            {
                _jwtExpirationMinutes = expiryDays * 24 * 60; // Convert days to minutes
            }
            else
            {
                _jwtExpirationMinutes = 60; // Default 1 hour
            }

            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

            _logger.LogInformation("PythonAiService initialized - BaseUrl: {BaseUrl}, Timeout: {Timeout}s, JWT Expiration: {ExpirationMinutes}min",
                _baseUrl, _timeoutSeconds, _jwtExpirationMinutes);
        }

        /// <summary>
        /// Generate JWT token for authenticating with Python API (for background jobs without user context)
        /// </summary>
        public string GenerateJwtToken()
        {
            if (string.IsNullOrEmpty(_jwtSecretKey))
            {
                throw new InvalidOperationException("JWT configuration is not available. Ensure Jwt:Key is configured in appsettings.json");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Only add custom claims - standard claims (iss, aud, iat, nbf, exp) are handled by JwtSecurityToken constructor
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "dotnet-backend"),
                new Claim("service", "SummerCampManagementSystem")
            };

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Log token details for debugging
            _logger.LogInformation("Generated JWT token for Python API");
            _logger.LogInformation("  Secret Key Length: {Length}", _jwtSecretKey.Length);
            _logger.LogInformation("  Issuer: {Issuer}", _jwtIssuer);
            _logger.LogInformation("  Audience: {Audience}", _jwtAudience);
            _logger.LogInformation("  Token (first 100 chars): {TokenPrefix}...", tokenString.Substring(0, Math.Min(100, tokenString.Length)));
            _logger.LogInformation("  Expires in: {Minutes} minutes", _jwtExpirationMinutes);

            return tokenString;
        }

        /// <summary>
        /// Create HTTP request with JWT authentication header using provided token
        /// </summary>
        private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, string authToken)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            _logger.LogDebug("Created authenticated request to {Url} with forwarded user token", url);
            return request;
        }

        /// <summary>
        /// Health check for the Python AI service
        /// </summary>
        public async Task<HealthCheckResponse> HealthCheckAsync()
        {
            try
            {
                _logger.LogInformation("Performing health check on Python AI service at {BaseUrl}", _baseUrl);

                var response = await _httpClient.GetAsync("/health");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var healthData = JsonSerializer.Deserialize<HealthCheckResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation("Python AI service is healthy");
                    return healthData ?? new HealthCheckResponse
                    {
                        IsHealthy = true,
                        Status = "OK",
                        Timestamp = DateTime.UtcNow
                    };
                }
                else
                {
                    _logger.LogWarning("Python AI service health check failed with status code {StatusCode}", response.StatusCode);
                    return new HealthCheckResponse
                    {
                        IsHealthy = false,
                        Status = $"Service returned {response.StatusCode}",
                        Timestamp = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing health check on Python AI service");
                return new HealthCheckResponse
                {
                    IsHealthy = false,
                    Status = $"Error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Load face database for a specific camp into Python AI service memory
        /// </summary>
        public async Task<FaceDbResponse> LoadCampFaceDbAsync(int campId, string authToken, bool forceReload = false)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("[TIMING] Loading face database for Camp {CampId} (ForceReload: {ForceReload})", campId, forceReload);

                var requestBody = new
                {
                    camp_id = campId,
                    force_reload = forceReload
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Use authenticated request with forwarded user token
                var request = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/face-db/load/{campId}", authToken);
                request.Content = httpContent;

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<FaceDbResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null)
                    {
                        stopwatch.Stop();
                        _logger.LogInformation("[TIMING] Successfully loaded {FaceCount} faces for Camp {CampId} in {ElapsedMs}ms ({ElapsedSeconds}s)",
                            result.FaceCount, campId, stopwatch.ElapsedMilliseconds, stopwatch.Elapsed.TotalSeconds);
                        return result;
                    }
                }

                _logger.LogError("Failed to load face database for Camp {CampId}. Status: {StatusCode}, Response: {Response}",
                    campId, response.StatusCode, responseContent);

                return new FaceDbResponse
                {
                    Success = false,
                    Message = $"Failed to load face database: {response.StatusCode}",
                    CampId = campId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout loading face database for Camp {CampId}. Timeout: {Timeout}s", campId, _timeoutSeconds);
                return new FaceDbResponse
                {
                    Success = false,
                    Message = $"Request timeout after {_timeoutSeconds}s. The Python API may be overloaded or the operation is taking too long.",
                    CampId = campId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request cancelled while loading face database for Camp {CampId}", campId);
                return new FaceDbResponse
                {
                    Success = false,
                    Message = $"Request was cancelled or timed out after {_timeoutSeconds}s",
                    CampId = campId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error loading face database for Camp {CampId}. BaseUrl: {BaseUrl}", campId, _baseUrl);
                return new FaceDbResponse
                {
                    Success = false,
                    Message = $"Connection error: {ex.Message}. Check if Python API at {_baseUrl} is accessible.",
                    CampId = campId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading face database for Camp {CampId}. Exception type: {ExceptionType}",
                    campId, ex.GetType().Name);
                return new FaceDbResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    CampId = campId,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Unload face database for a specific camp from Python AI service memory
        /// </summary>
        public async Task<FaceDbResponse> UnloadCampFaceDbAsync(int campId, string authToken)
        {
            try
            {
                _logger.LogInformation("Unloading face database for Camp {CampId}", campId);

                // Use authenticated request with forwarded user token
                var request = CreateAuthenticatedRequest(HttpMethod.Delete, $"/api/face-db/unload/{campId}", authToken);
                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<FaceDbResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null)
                    {
                        _logger.LogInformation("Successfully unloaded face database for Camp {CampId}", campId);
                        return result;
                    }
                }

                _logger.LogError("Failed to unload face database for Camp {CampId}. Status: {StatusCode}, Response: {Response}",
                    campId, response.StatusCode, responseContent);

                return new FaceDbResponse
                {
                    Success = false,
                    Message = $"Failed to unload face database: {response.StatusCode}",
                    CampId = campId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading face database for Camp {CampId}", campId);
                return new FaceDbResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    CampId = campId,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Recognize faces in a photo for a specific activity schedule
        /// </summary>
        public async Task<RecognitionResponse> RecognizeAsync(RecognizeFaceRequest request, string authToken)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("[TIMING] Recognizing faces for ActivitySchedule {ActivityScheduleId}, CampId: {CampId}, GroupId: {GroupId}",
                    request.ActivityScheduleId, request.CampId, request.GroupId);

                using var content = new MultipartFormDataContent();

                // Add photo file
                var fileStream = request.Photo.OpenReadStream();
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(request.Photo.ContentType);
                content.Add(streamContent, "photo", request.Photo.FileName);

                // Use group-specific endpoint if groupId is available (core activities)
                // Otherwise use activity-specific endpoint (optional activities)
                HttpResponseMessage response;
                if (request.GroupId.HasValue)
                {
                    // Core activity - group-specific recognition with forwarded user token
                    var httpRequest = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/recognition/recognize-group/{request.CampId}/{request.GroupId.Value}", authToken);
                    httpRequest.Content = content;
                    response = await _httpClient.SendAsync(httpRequest);
                }
                else
                {
                    // Optional activity - activity-specific recognition with forwarded user token
                    var httpRequest = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/recognition/recognize-activity/{request.CampId}/{request.ActivityScheduleId}", authToken);
                    httpRequest.Content = content;
                    response = await _httpClient.SendAsync(httpRequest);
                }
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<RecognitionResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null)
                    {
                        stopwatch.Stop();
                        _logger.LogInformation("[TIMING] Successfully recognized {MatchedFaces}/{TotalFaces} faces for ActivitySchedule {ActivityScheduleId} in {ElapsedMs}ms ({ElapsedSeconds}s)",
                            result.MatchedFaces, result.TotalFacesDetected, request.ActivityScheduleId, stopwatch.ElapsedMilliseconds, stopwatch.Elapsed.TotalSeconds);
                        return result;
                    }
                }

                _logger.LogError("Failed to recognize faces for ActivitySchedule {ActivityScheduleId}. Status: {StatusCode}, Response: {Response}",
                    request.ActivityScheduleId, response.StatusCode, responseContent);

                return new RecognitionResponse
                {
                    Success = false,
                    Message = $"Failed to recognize faces: {response.StatusCode}",
                    ActivityScheduleId = request.ActivityScheduleId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recognizing faces for ActivitySchedule {ActivityScheduleId}", request.ActivityScheduleId);
                return new RecognitionResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    ActivityScheduleId = request.ActivityScheduleId,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Get statistics about loaded camps in Python AI service
        /// </summary>
        public async Task<Dictionary<int, int>> GetLoadedCampsAsync(string authToken)
        {
            try
            {
                _logger.LogInformation("Getting loaded camps statistics from Python AI service");

                // Use authenticated request with forwarded user token
                var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/face-db/stats", authToken);
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var stats = JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (stats != null && stats.ContainsKey("loaded_camps"))
                    {
                        var loadedCampsJson = stats["loaded_camps"].ToString();
                        var loadedCamps = JsonSerializer.Deserialize<Dictionary<int, int>>(loadedCampsJson ?? "{}", new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        return loadedCamps ?? new Dictionary<int, int>();
                    }
                }

                _logger.LogWarning("Failed to get loaded camps statistics. Status: {StatusCode}", response.StatusCode);
                return new Dictionary<int, int>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting loaded camps statistics");
                return new Dictionary<int, int>();
            }
        }
    }
}
