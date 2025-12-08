using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SummerCampManagementSystem.BLL.DTOs;
using SummerCampManagementSystem.BLL.Hubs;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;
using System.Security.Claims;

namespace SummerCampManagementSystem.API.Controllers;

[ApiController]
[Route("api/attendance")]
public class AttendanceWebhookController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IHubContext<AttendanceHub> _hubContext;
    private readonly ILogger<AttendanceWebhookController> _logger;

    public AttendanceWebhookController(
        IAttendanceService attendanceService,
        IIdempotencyService idempotencyService,
        IHubContext<AttendanceHub> hubContext,
        ILogger<AttendanceWebhookController> logger)
    {
        _attendanceService = attendanceService;
        _idempotencyService = idempotencyService;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Webhook endpoint: Python → .NET attendance update
    /// Updates attendance logs and broadcasts SignalR events
    /// </summary>
    [HttpPost("update-from-recognition")]
    [Authorize]
    public async Task<IActionResult> UpdateFromRecognition([FromBody] RecognitionWebhookRequest request)
    {
        var requestId = HttpContext.Request.Headers["X-Request-ID"].FirstOrDefault()
                        ?? request.RequestId
                        ?? Guid.NewGuid().ToString();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // ========== PHASE 1: VALIDATE SERVICE TOKEN ==========
            var issuer = User.FindFirst(ClaimTypes.NameIdentifier)?.Issuer
                         ?? User.FindFirst("iss")?.Value;
            var isService = User.FindFirst("service")?.Value == "True"
                            || User.FindFirst("service")?.Value == "true";

            if (issuer != "PythonAiService" || !isService)
            {
                _logger.LogWarning(
                    "[{RequestId}] ⚠️ Unauthorized webhook attempt from issuer: {Issuer}, service: {IsService}",
                    requestId, issuer, isService
                );
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid service token. Expected issuer: PythonAiService",
                    requestId
                });
            }

            _logger.LogInformation(
                "[{RequestId}] 📥 Received webhook: {Count} faces for activity {ActivityId}",
                requestId, request.RecognizedFaces.Count, request.ActivityScheduleId
            );

            // ========== PHASE 2: CHECK IDEMPOTENCY ==========
            if (await _idempotencyService.IsProcessedAsync(requestId))
            {
                var cached = await _idempotencyService.GetCachedResultAsync<AttendanceUpdateResult>(requestId);

                _logger.LogInformation(
                    "[{RequestId}] ⚡ Duplicate request detected, returning cached result",
                    requestId
                );

                return Ok(cached ?? new AttendanceUpdateResult
                {
                    Success = true,
                    RequestId = requestId,
                    Timestamp = DateTime.UtcNow,
                    UpdatedCount = 0,
                    CreatedCount = 0,
                    RecognizedCampers = new List<RecognizedCamperDto>()
                });
            }

            // ========== PHASE 3: MATCH EMBEDDINGS TO CAMPERS ==========
            var recognizedCampers = await _attendanceService.MatchEmbeddingsToCampersAsync(
                request.ActivityScheduleId,
                request.RecognizedFaces
            );

            _logger.LogInformation(
                "[{RequestId}] ✅ Matched {Count} campers from {FaceCount} faces",
                requestId, recognizedCampers.Count, request.RecognizedFaces.Count
            );

            // ========== PHASE 4: UPDATE ATTENDANCE LOGS ==========
            var result = await _attendanceService.UpdateAttendanceLogsAsync(
                request.ActivityScheduleId,
                recognizedCampers,
                request.Metadata
            );

            result.RequestId = requestId;
            result.Timestamp = DateTime.UtcNow;
            result.Success = true;

            // ========== PHASE 5: CACHE RESULT FOR IDEMPOTENCY ==========
            await _idempotencyService.MarkAsProcessedAsync(
                requestId,
                result,
                TimeSpan.FromHours(1)  // Cache for 1 hour
            );

            stopwatch.Stop();

            _logger.LogInformation(
                "[{RequestId}] ✅ Attendance updated in {Elapsed}ms: {Updated} updated, {Created} created",
                requestId, stopwatch.ElapsedMilliseconds, result.UpdatedCount, result.CreatedCount
            );

            // ========== PHASE 6: BROADCAST SIGNALR EVENT ==========
            try
            {
                await _hubContext.Clients
                    .Group($"attendance/{request.ActivityScheduleId}")
                    .SendAsync("AttendanceUpdated", new
                    {
                        eventType = "AttendanceUpdated",
                        activityScheduleId = request.ActivityScheduleId,
                        campers = recognizedCampers,
                        timestamp = DateTime.UtcNow,
                        requestId,
                        summary = new
                        {
                            updated = result.UpdatedCount,
                            created = result.CreatedCount,
                            total = result.UpdatedCount + result.CreatedCount
                        }
                    });

                result.BroadcastSent = true;

                _logger.LogInformation(
                    "[{RequestId}] 📡 SignalR broadcast sent to attendance/{ActivityId}",
                    requestId, request.ActivityScheduleId
                );
            }
            catch (Exception signalrEx)
            {
                _logger.LogWarning(
                    signalrEx,
                    "[{RequestId}] ⚠️ SignalR broadcast failed (attendance update still succeeded)",
                    requestId
                );
                result.BroadcastSent = false;
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "[{RequestId}] ❌ Webhook processing error after {Elapsed}ms",
                requestId, stopwatch.ElapsedMilliseconds
            );

            return StatusCode(500, new
            {
                success = false,
                message = ex.Message,
                requestId,
                processingTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
    }
}
