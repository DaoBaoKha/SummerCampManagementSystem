using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.DTOs;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(IUnitOfWork unitOfWork, ILogger<AttendanceService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<RecognizedCamperDto>> MatchEmbeddingsToCampersAsync(
        int activityScheduleId,
        List<RecognizedFaceDto> recognizedFaces)
    {
        try
        {
            // Get activity schedule with camp and groups
            var activitySchedule = await _unitOfWork.ActivitySchedules
                .GetQueryable()
                .Include(a => a.activity)
                    .ThenInclude(a => a.camp)
                    .ThenInclude(c => c.Groups)
                        .ThenInclude(g => g.CamperGroups)
                            .ThenInclude(cg => cg.camper)
                .FirstOrDefaultAsync(a => a.activityScheduleId == activityScheduleId);

            if (activitySchedule == null)
            {
                _logger.LogWarning("Activity schedule {ActivityScheduleId} not found", activityScheduleId);
                return new List<RecognizedCamperDto>();
            }

            // Get all campers from all groups in the camp
            var campers = activitySchedule.activity.camp.Groups
                .SelectMany(g => g.CamperGroups)
                .Select(cg => cg.camper)
                .Distinct()
                .ToList();
            var results = new List<RecognizedCamperDto>();

            // Match each face embedding to campers
            foreach (var face in recognizedFaces)
            {
                // If Python already matched the camper, use that
                if (face.CamperId.HasValue && face.CamperId.Value > 0)
                {
                    var camper = campers.FirstOrDefault(c => c.camperId == face.CamperId.Value);
                    if (camper != null)
                    {
                        results.Add(new RecognizedCamperDto
                        {
                            CamperId = camper.camperId,
                            CamperName = camper.camperName,
                            Confidence = face.Confidence,
                            Distance = 1.0 - face.Confidence,  // Convert confidence back to distance
                            BoundingBox = face.BoundingBox
                        });
                        continue;
                    }
                }

                // If no camper ID from Python, match by embedding
                // Note: This requires campers to have stored embeddings
                // For now, skip if no camper ID (Python should handle matching)
                _logger.LogDebug(
                    "Face with confidence {Confidence} has no camper ID, skipping",
                    face.Confidence
                );
            }

            _logger.LogInformation(
                "Matched {Count} campers for activity schedule {ActivityScheduleId}",
                results.Count, activityScheduleId
            );

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching embeddings to campers");
            throw;
        }
    }

    public async Task<AttendanceUpdateResult> UpdateAttendanceLogsAsync(
        int activityScheduleId,
        List<RecognizedCamperDto> recognizedCampers,
        WebhookMetadata metadata)
    {
        var updatedCount = 0;
        var createdCount = 0;

        try
        {
            var currentTime = DateTime.UtcNow;

            // Process each recognized camper (same pattern as AdminAiController)
            foreach (var recognized in recognizedCampers)
            {
                // Find existing log for this camper and activity schedule
                var existingLog = await _unitOfWork.AttendanceLogs
                    .GetQueryable()
                    .Where(al => al.camperId == recognized.CamperId
                              && al.activityScheduleId == activityScheduleId)
                    .OrderByDescending(al => al.timestamp)
                    .FirstOrDefaultAsync();

                if (existingLog != null)
                {
                    // Update existing log
                    existingLog.participantStatus = "Present";
                    existingLog.timestamp = currentTime;
                    existingLog.checkInMethod = "FaceRecognition";
                    existingLog.eventType = "CheckIn";
                    existingLog.note = $"AI Recognition: {recognized.Confidence:P2}";

                    await _unitOfWork.AttendanceLogs.UpdateAsync(existingLog);
                    updatedCount++;

                    _logger.LogDebug(
                        "Updated attendance log {LogId} for camper {CamperId} (confidence: {Confidence})",
                        existingLog.attendanceLogId, recognized.CamperId, recognized.Confidence
                    );
                }
                else
                {
                    // Create new log
                    var newLog = new AttendanceLog
                    {
                        camperId = recognized.CamperId,
                        activityScheduleId = activityScheduleId,
                        participantStatus = "Present",
                        timestamp = currentTime,
                        eventType = "CheckIn",
                        checkInMethod = "FaceRecognition",
                        note = $"AI Recognition: {recognized.Confidence:P2}"
                    };

                    await _unitOfWork.AttendanceLogs.CreateAsync(newLog);
                    createdCount++;

                    _logger.LogDebug(
                        "Created attendance log for camper {CamperId} (confidence: {Confidence})",
                        recognized.CamperId, recognized.Confidence
                    );
                }

                // Commit after each camper (same as AdminAiController pattern)
                await _unitOfWork.CommitAsync();
            }
            _logger.LogInformation(
                "Attendance update complete: {Updated} updated, {Created} created for activity {ActivityId}",
                updatedCount, createdCount, activityScheduleId
            );

            return new AttendanceUpdateResult
            {
                Success = true,
                UpdatedCount = updatedCount,
                CreatedCount = createdCount,
                RecognizedCampers = recognizedCampers,
                Timestamp = currentTime,
                ProcessedBy = metadata.ProcessedBy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating attendance logs for activity schedule {ActivityScheduleId}",
                activityScheduleId
            );
            throw;
        }
    }
}
