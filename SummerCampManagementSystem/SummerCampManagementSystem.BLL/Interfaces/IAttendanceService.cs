using SummerCampManagementSystem.BLL.DTOs;

namespace SummerCampManagementSystem.BLL.Interfaces;

/// <summary>
/// Service for attendance operations including webhook-based updates
/// </summary>
public interface IAttendanceService
{
    /// <summary>
    /// Match face embeddings to campers in the activity schedule
    /// </summary>
    Task<List<RecognizedCamperDto>> MatchEmbeddingsToCampersAsync(
        int activityScheduleId,
        List<RecognizedFaceDto> recognizedFaces
    );

    /// <summary>
    /// Update attendance logs for recognized campers
    /// </summary>
    Task<AttendanceUpdateResult> UpdateAttendanceLogsAsync(
        int activityScheduleId,
        List<RecognizedCamperDto> recognizedCampers,
        WebhookMetadata metadata
    );
}
