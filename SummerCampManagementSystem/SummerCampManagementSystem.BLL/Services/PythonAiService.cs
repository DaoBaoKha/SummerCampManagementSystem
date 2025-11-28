using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.DTOs.AI;
using SummerCampManagementSystem.BLL.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SummerCampManagementSystem.BLL.Services
{
    /// <summary>
    /// Service for communicating with Python Flask AI face recognition API
    /// </summary>
    public class PythonAiService : IPythonAiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PythonAiService> _logger;
        private readonly string _baseUrl;
        private readonly int _timeoutSeconds;

        public PythonAiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<PythonAiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;

            _baseUrl = configuration["AIServiceSettings:BaseUrl"] ?? "http://localhost:5000";
            _timeoutSeconds = int.TryParse(configuration["AIServiceSettings:Timeout"], out var timeout) ? timeout : 30;

            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
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
        public async Task<FaceDbResponse> LoadCampFaceDbAsync(int campId, bool forceReload = false)
        {
            try
            {
                _logger.LogInformation("Loading face database for Camp {CampId} (ForceReload: {ForceReload})", campId, forceReload);

                var requestBody = new
                {
                    camp_id = campId,
                    force_reload = forceReload
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/api/face-db/load/{campId}", httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<FaceDbResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null)
                    {
                        _logger.LogInformation("Successfully loaded {FaceCount} faces for Camp {CampId}", result.FaceCount, campId);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading face database for Camp {CampId}", campId);
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
        public async Task<FaceDbResponse> UnloadCampFaceDbAsync(int campId)
        {
            try
            {
                _logger.LogInformation("Unloading face database for Camp {CampId}", campId);

                var response = await _httpClient.DeleteAsync($"/api/face-db/unload/{campId}");
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
        public async Task<RecognitionResponse> RecognizeAsync(RecognizeFaceRequest request)
        {
            try
            {
                _logger.LogInformation("Recognizing faces for ActivitySchedule {ActivityScheduleId}", request.ActivityScheduleId);

                using var content = new MultipartFormDataContent();

                // Add activity schedule ID
                content.Add(new StringContent(request.ActivityScheduleId.ToString()), "activity_schedule_id");

           

                // Add photo file
                var fileStream = request.Photo.OpenReadStream();
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(request.Photo.ContentType);
                content.Add(streamContent, "photo", request.Photo.FileName);

                var response = await _httpClient.PostAsync($"/api/recognition/recognize/{request.ActivityScheduleId}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<RecognitionResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null)
                    {
                        _logger.LogInformation("Successfully recognized {MatchedFaces}/{TotalFaces} faces for ActivitySchedule {ActivityScheduleId}",
                            result.MatchedFaces, result.TotalFacesDetected, request.ActivityScheduleId);
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
        public async Task<Dictionary<int, int>> GetLoadedCampsAsync()
        {
            try
            {
                _logger.LogInformation("Getting loaded camps statistics from Python AI service");

                var response = await _httpClient.GetAsync("/api/face-db/stats");

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
