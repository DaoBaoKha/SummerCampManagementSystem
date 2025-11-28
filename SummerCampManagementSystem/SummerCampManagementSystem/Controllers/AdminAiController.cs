using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.DTOs.AI;
using SummerCampManagementSystem.BLL.HostedServices;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Jobs;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/admin/ai")]
    [ApiController]
    [Authorize(Roles = "Admin,Manager")]
    public class AdminAiController : ControllerBase
    {
        private readonly IPythonAiService _pythonAiService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminAiController> _logger;

        public AdminAiController(
            IPythonAiService pythonAiService,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<AdminAiController> logger)
        {
            _pythonAiService = pythonAiService;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Health check for Python AI service
        /// </summary>
        /// <response code="200">Returns health status of Python AI service</response>
        [HttpGet("health")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> HealthCheck()
        {
            var health = await _pythonAiService.HealthCheckAsync();
            return Ok(health);
        }

        /// <summary>
        /// Get statistics about currently loaded camps in Python AI service
        /// </summary>
        /// <response code="200">Returns dictionary of loaded camps with face counts</response>
        [HttpGet("loaded-camps")]
        [ProducesResponseType(typeof(Dictionary<int, int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLoadedCamps()
        {
            var loadedCamps = await _pythonAiService.GetLoadedCampsAsync();
            return Ok(new
            {
                success = true,
                data = loadedCamps,
                totalCamps = loadedCamps.Count,
                totalFaces = loadedCamps.Values.Sum()
            });
        }

        /// <summary>
        /// Manually preload face database for a specific camp
        /// Triggers immediate job execution
        /// </summary>
        /// <param name="campId">The camp ID to preload</param>
        /// <param name="forceReload">Force reload even if already loaded</param>
        /// <response code="200">Preload job triggered successfully</response>
        /// <response code="404">Camp not found</response>
        [HttpPost("preload/{campId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PreloadCamp(int campId, [FromQuery] bool forceReload = false)
        {
            _logger.LogInformation("Manual preload requested for Camp {CampId} (ForceReload: {ForceReload})", campId, forceReload);

            // Verify camp exists
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
            if (camp == null)
            {
                return NotFound(new { success = false, message = $"Camp {campId} not found" });
            }

            // Trigger immediate preload job
            var jobId = PreloadCampFaceDbJob.TriggerImmediately(campId, forceReload);

            _logger.LogInformation("Preload job triggered for Camp {CampId}. JobId: {JobId}", campId, jobId);

            return Ok(new
            {
                success = true,
                message = "Preload job triggered successfully",
                campId = campId,
                jobId = jobId,
                forceReload = forceReload
            });
        }

        /// <summary>
        /// Manually unload face database for a specific camp
        /// Triggers immediate cleanup job execution
        /// </summary>
        /// <param name="campId">The camp ID to unload</param>
        /// <response code="200">Cleanup job triggered successfully</response>
        /// <response code="404">Camp not found</response>
        [HttpDelete("unload/{campId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnloadCamp(int campId)
        {
            _logger.LogInformation("Manual unload requested for Camp {CampId}", campId);

            // Verify camp exists
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
            if (camp == null)
            {
                return NotFound(new { success = false, message = $"Camp {campId} not found" });
            }

            // Trigger immediate cleanup job
            var jobId = CleanupCampFaceDbJob.TriggerImmediately(campId);

            _logger.LogInformation("Cleanup job triggered for Camp {CampId}. JobId: {JobId}", campId, jobId);

            return Ok(new
            {
                success = true,
                message = "Cleanup job triggered successfully",
                campId = campId,
                jobId = jobId
            });
        }

        /// <summary>
        /// Rebuild all Hangfire jobs for all camps
        /// Scans database and reschedules preload/cleanup jobs
        /// </summary>
        /// <response code="200">Jobs rebuilt successfully</response>
        [HttpPost("jobs/rebuild")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> RebuildAllJobs()
        {
            _logger.LogInformation("Manual job rebuild requested");

            try
            {
                var allCamps = await _unitOfWork.Camps.GetAllAsync();
                var now = DateTime.UtcNow;
                var preloadBufferMinutes = int.TryParse(
                    _configuration["AttendanceJobSettings:PreDownloadBufferMinutes"],
                    out var buffer) ? buffer : 10;

                int preloadJobsScheduled = 0;
                int cleanupJobsScheduled = 0;
                int attendanceFolderJobsScheduled = 0;

                foreach (var camp in allCamps)
                {
                    try
                    {
                        if (camp.startDate == null || camp.endDate == null) continue;

                        var startDate = camp.startDate.Value;
                        var endDate = camp.endDate.Value;
                        var registrationEndDate = camp.registrationEndDate ?? startDate.AddDays(-1);

                        // Schedule attendance folder creation job
                        if (registrationEndDate > now)
                        {
                            AttendanceFolderCreationJob.ScheduleForCamp(camp.campId, registrationEndDate);
                            attendanceFolderJobsScheduled++;
                        }

                        // Schedule preload job
                        var preloadTime = startDate.AddMinutes(-preloadBufferMinutes);
                        if (preloadTime > now)
                        {
                            PreloadCampFaceDbJob.ScheduleForCamp(camp.campId, startDate, preloadBufferMinutes);
                            preloadJobsScheduled++;
                        }
                        else if (startDate > now && endDate > now)
                        {
                            PreloadCampFaceDbJob.TriggerImmediately(camp.campId, forceReload: false);
                            preloadJobsScheduled++;
                        }

                        // Schedule cleanup job
                        if (endDate > now)
                        {
                            CleanupCampFaceDbJob.ScheduleForCamp(camp.campId, endDate);
                            cleanupJobsScheduled++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error scheduling jobs for Camp {CampId}", camp.campId);
                    }
                }

                _logger.LogInformation(
                    "Job rebuild completed. Scheduled: {AttendanceFolderJobs} attendance folder jobs, {PreloadJobs} preload jobs, {CleanupJobs} cleanup jobs",
                    attendanceFolderJobsScheduled, preloadJobsScheduled, cleanupJobsScheduled);

                return Ok(new
                {
                    success = true,
                    message = "All jobs rebuilt successfully",
                    statistics = new
                    {
                        totalCamps = allCamps.Count(),
                        attendanceFolderJobsScheduled = attendanceFolderJobsScheduled,
                        preloadJobsScheduled = preloadJobsScheduled,
                        cleanupJobsScheduled = cleanupJobsScheduled
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebuilding jobs");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error rebuilding jobs",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Recognize faces in uploaded photo for a specific activity schedule
        /// </summary>
        /// <param name="request">Recognition request with activity schedule ID and photo</param>
        /// <response code="200">Recognition completed successfully</response>
        /// <response code="400">Invalid request</response>
        [HttpPost("recognize")]
        [ProducesResponseType(typeof(RecognitionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RecognizeFaces([FromForm] RecognizeFaceRequest request)
        {
            if (request.Photo == null || request.Photo.Length == 0)
            {
                return BadRequest(new { success = false, message = "Photo file is required" });
            }

            if (request.ActivityScheduleId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid activity schedule ID is required" });
            }

            _logger.LogInformation(
                "Face recognition requested for ActivitySchedule {ActivityScheduleId}",
                request.ActivityScheduleId);

            // Get activity schedule to find camp ID
            var activitySchedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(request.ActivityScheduleId);
            if (activitySchedule == null)
            {
                return NotFound(new { success = false, message = $"Activity schedule {request.ActivityScheduleId} not found" });
            }

            // Get camp ID from activity schedule
            var activity = await _unitOfWork.Activities.GetByIdAsync(activitySchedule.activityId);
            if (activity == null)
            {
                return NotFound(new { success = false, message = $"Activity {activitySchedule.activityId} not found" });
            }

            if (!activity.campId.HasValue)
            {
                return BadRequest(new { success = false, message = "Activity has no associated camp" });
            }

            int campId = activity.campId.Value;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Check if face database is already loaded (FAST - just HTTP call, no download)
            var loadedCamps = await _pythonAiService.GetLoadedCampsAsync();
            if (!loadedCamps.ContainsKey(campId) || loadedCamps[campId] == 0)
            {
                _logger.LogInformation("Face database not loaded for Camp {CampId}, loading now...", campId);
                var loadResult = await _pythonAiService.LoadCampFaceDbAsync(campId);
                if (!loadResult.Success)
                {
                    _logger.LogError("Failed to load face database for Camp {CampId}: {Message}", campId, loadResult.Message);
                    return StatusCode(500, new
                    {
                        success = false,
                        message = $"Failed to load face database: {loadResult.Message}"
                    });
                }
                _logger.LogInformation("Face database loaded: {FaceCount} faces for Camp {CampId}", loadResult.FaceCount, campId);
            }
            else
            {
                _logger.LogInformation("Face database already loaded for Camp {CampId} ({FaceCount} faces)", campId, loadedCamps[campId]);
            }

            // Perform recognition
            stopwatch.Stop();
            var preprocessingTime = stopwatch.ElapsedMilliseconds;
            _logger.LogInformation("Preprocessing completed in {Time}ms, starting recognition...", preprocessingTime);

            stopwatch.Restart();
            var result = await _pythonAiService.RecognizeAsync(request);
            stopwatch.Stop();

            _logger.LogInformation(
                "Recognition completed in {RecognitionTime}ms (Python: {PythonTime}ms, Total: {TotalTime}ms)",
                stopwatch.ElapsedMilliseconds,
                result.ProcessingTimeMs,
                preprocessingTime + stopwatch.ElapsedMilliseconds);

            if (!result.Success)
            {
                return StatusCode(500, result);
            }

            // Get current user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? staffId = userIdClaim != null && int.TryParse(userIdClaim, out var id) ? id : (int?)null;

            // Save attendance logs for recognized campers
            if (result.RecognizedCampers != null && result.RecognizedCampers.Count > 0)
            {
                _logger.LogInformation(
                    "Saving {Count} attendance logs for ActivitySchedule {ActivityScheduleId}",
                    result.RecognizedCampers.Count,
                    request.ActivityScheduleId);

                foreach (var recognizedCamper in result.RecognizedCampers)
                {
                    if (recognizedCamper.CamperId <= 0)
                    {
                        _logger.LogWarning("Skipping attendance log for invalid CamperId: {CamperId}", recognizedCamper.CamperId);
                        continue;
                    }

                    var attendanceLog = new AttendanceLog
                    {
                        camperId = recognizedCamper.CamperId,
                        staffId = staffId,
                        activityScheduleId = request.ActivityScheduleId,
                        participantStatus = "Present",
                        timestamp = DateTime.UtcNow,
                        checkInMethod = "FaceRecognition",
                        eventType = "CheckIn",
                        note = $"Face detected with {recognizedCamper.Confidence:P2} confidence. Session: {result.SessionId}"
                    };

                    await _unitOfWork.AttendanceLogs.CreateAsync(attendanceLog);
                }

                // Update activity schedule status
                activitySchedule.status = "AttendanceChecked";
                await _unitOfWork.ActivitySchedules.UpdateAsync(activitySchedule);

                await _unitOfWork.CommitAsync();
                _logger.LogInformation("Successfully saved {Count} attendance logs", result.RecognizedCampers.Count);
            }
            else
            {
                _logger.LogInformation("No campers recognized, no attendance logs created");
            }

            return Ok(result);
        }
           
}
    }
}
