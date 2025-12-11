using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    [Authorize(Roles = "Admin,Manager,Staff")]
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
            // Extract user's JWT token from Authorization header
            var authHeader = Request.Headers["Authorization"].ToString();
            var userToken = authHeader.Replace("Bearer ", "").Trim();

            if (string.IsNullOrEmpty(userToken))
            {
                return Unauthorized(new { success = false, message = "No authentication token provided" });
            }

            var loadedCamps = await _pythonAiService.GetLoadedCampsAsync(userToken);
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
        /// Calls Python AI service directly to load face database
        /// </summary>
        /// <param name="campId">The camp ID to preload</param>
        /// <param name="forceReload">Force reload even if already loaded</param>
        /// <response code="200">Preload completed successfully</response>
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

            // Generate service token for this request
            var serviceToken = _pythonAiService.GenerateJwtToken();

            // Call Python AI service directly
            var result = await _pythonAiService.LoadCampFaceDbAsync(campId, serviceToken, forceReload);

            if (result.Success)
            {
                _logger.LogInformation("Manual preload completed for Camp {CampId}. Loaded {FaceCount} faces", campId, result.FaceCount);

                return Ok(new
                {
                    success = true,
                    message = "Preload completed successfully",
                    campId = campId,
                    faceCount = result.FaceCount,
                    forceReload = forceReload
                });
            }
            else
            {
                _logger.LogError("Manual preload failed for Camp {CampId}: {ErrorMessage}", campId, result.Message);

                return StatusCode(500, new
                {
                    success = false,
                    message = $"Preload failed: {result.Message}",
                    campId = campId
                });
            }
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

                        // Note: Preload job now runs as daily recurring job at 19:00 UTC (02:00 UTC+7)
                        // No need to schedule per-camp preload jobs anymore

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
                    "Job rebuild completed. Scheduled: {AttendanceFolderJobs} attendance folder jobs, {CleanupJobs} cleanup jobs (preload jobs run on daily schedule)",
                    attendanceFolderJobsScheduled, cleanupJobsScheduled);

                return Ok(new
                {
                    success = true,
                    message = "All jobs rebuilt successfully",
                    statistics = new
                    {
                        totalCamps = allCamps.Count(),
                        attendanceFolderJobsScheduled = attendanceFolderJobsScheduled,
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
            // Extract user's JWT token from Authorization header
            var authHeader = Request.Headers["Authorization"].ToString();
            var userToken = authHeader.Replace("Bearer ", "").Trim();

            if (string.IsNullOrEmpty(userToken))
            {
                return Unauthorized(new { success = false, message = "No authentication token provided" });
            }

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
            request.CampId = campId;

            // Get groupId from GroupActivity (for core activities)
            var groupActivity = await _unitOfWork.GroupActivities
                .GetQueryable()
                .Where(ga => ga.activityScheduleId == request.ActivityScheduleId)
                .FirstOrDefaultAsync();

            if (groupActivity != null && groupActivity.groupId.HasValue)
            {
                request.GroupId = groupActivity.groupId.Value;
                _logger.LogInformation("Found GroupId {GroupId} for ActivitySchedule {ActivityScheduleId}",
                    request.GroupId, request.ActivityScheduleId);
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Check if face database is already loaded (FAST - just HTTP call, no download)
            var loadedCamps = await _pythonAiService.GetLoadedCampsAsync(userToken);
            if (!loadedCamps.ContainsKey(campId) || loadedCamps[campId] == 0)
            {
                _logger.LogInformation("Face database not loaded for Camp {CampId}, loading now...", campId);
                var loadResult = await _pythonAiService.LoadCampFaceDbAsync(campId, userToken);
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
            var result = await _pythonAiService.RecognizeAsync(request, userToken);
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

            // Update pre-existing attendance logs for recognized campers
            var updatedCount = 0;
            var notFoundCount = 0;
            var notFoundCampers = new List<int>();

            if (result.RecognizedCampers != null && result.RecognizedCampers.Count > 0)
            {
                _logger.LogInformation(
                    "Updating {Count} attendance logs for ActivitySchedule {ActivityScheduleId}",
                    result.RecognizedCampers.Count,
                    request.ActivityScheduleId);

                try
                {
                    // ✅ OPTIMIZATION: Batch query - fetch ALL attendance logs in ONE database call
                    var camperIds = result.RecognizedCampers
                        .Where(rc => rc.CamperId > 0)
                        .Select(rc => rc.CamperId)
                        .ToList();

                    _logger.LogInformation("Fetching attendance logs for {CamperCount} campers...", camperIds.Count);

                    // Fetch all attendance logs for these campers in this activity schedule
                    var existingLogsList = await _unitOfWork.AttendanceLogs
                        .GetQueryable()
                        .Where(al => camperIds.Contains(al.camperId)
                                  && al.activityScheduleId == request.ActivityScheduleId)
                        .ToListAsync();

                    _logger.LogInformation("Fetched {LogCount} existing attendance logs from database", existingLogsList.Count);

                    // Handle duplicates: Group by camperId and take the first log for each camper
                    var existingLogs = existingLogsList
                        .GroupBy(al => al.camperId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.First()
                        );

                    if (existingLogsList.Count != existingLogs.Count)
                    {
                        _logger.LogWarning("⚠️ Found {DuplicateCount} duplicate attendance logs that were deduplicated",
                            existingLogsList.Count - existingLogs.Count);
                    }

                    var currentTime = DateTime.UtcNow;

                    // ✅ OPTIMIZATION: Prepare batch updates in memory
                    foreach (var recognizedCamper in result.RecognizedCampers)
                    {
                        if (recognizedCamper.CamperId <= 0)
                        {
                            _logger.LogWarning("Skipping attendance log for invalid CamperId: {CamperId}", recognizedCamper.CamperId);
                            continue;
                        }

                        // O(1) dictionary lookup instead of database query
                        if (existingLogs.TryGetValue(recognizedCamper.CamperId, out var existingLog))
                        {
                            // UPDATE existing log in memory
                            existingLog.participantStatus = "Present";
                            existingLog.timestamp = currentTime;
                            existingLog.checkInMethod = "FaceRecognition";
                            existingLog.eventType = "CheckIn";
                            existingLog.staffId = staffId;
                            existingLog.note = $"Face detected with {recognizedCamper.Confidence:P2} confidence. Session: {result.SessionId}";

                            await _unitOfWork.AttendanceLogs.UpdateAsync(existingLog);
                            updatedCount++;
                        }
                        else
                        {
                            // ERROR: No pre-existing log found
                            notFoundCount++;
                            notFoundCampers.Add(recognizedCamper.CamperId);
                            _logger.LogWarning("❌ No pre-existing AttendanceLog found for Camper {CamperId} in ActivitySchedule {ActivityScheduleId}",
                                recognizedCamper.CamperId, request.ActivityScheduleId);
                        }
                    }
                }
                catch (Exception dbFetchEx)
                {
                    _logger.LogError(dbFetchEx, "❌ Database query failed while fetching attendance logs");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = $"Face recognition succeeded but database query failed: {dbFetchEx.Message}",
                        recognitionResult = result
                    });
                }

                if (updatedCount > 0)
                {
                    try
                    {
                        // Update activity schedule status
                        activitySchedule.status = "AttendanceChecked";
                        await _unitOfWork.ActivitySchedules.UpdateAsync(activitySchedule);

                        // ✅ OPTIMIZATION: Single transaction commit for all updates
                        await _unitOfWork.CommitAsync();
                        _logger.LogInformation("✅ Batch updated {UpdatedCount} attendance logs in single transaction (NotFound: {NotFoundCount})",
                            updatedCount, notFoundCount);
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogError(dbEx, "❌ Database commit failed after successful face recognition. UpdatedCount: {UpdatedCount}", updatedCount);
                        return StatusCode(500, new
                        {
                            success = false,
                            message = $"Face recognition succeeded but database update failed: {dbEx.Message}",
                            recognitionResult = result,
                            updatedCount = 0,
                            notFoundCount = notFoundCount
                        });
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️  No attendance logs were updated. {NotFoundCount} campers had no pre-existing logs.",
                        notFoundCount);
                }

                // Add error information to response if some campers had no pre-existing logs
                if (notFoundCount > 0)
                {
                    result.Message += $"\n⚠️ Warning: {notFoundCount} recognized camper(s) had no pre-existing attendance log (CamperIDs: {string.Join(", ", notFoundCampers)}). They may not be registered for this activity.";
                }
            }
            else
            {
                _logger.LogInformation("ℹ️  No campers recognized, no attendance logs updated");
            }

            return Ok(result);
        }

    }
}
