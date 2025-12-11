using Hangfire;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Jobs
{
    /// <summary>
    /// Hangfire background job that cleans up face database from Python AI service
    /// after camp ends (triggered at EndDate + 1 day at 00:00)
    /// Only runs if no other camps are using the same campers
    /// </summary>
    public class CleanupCampFaceDbJob
    {
        private readonly IPythonAiService _pythonAiService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CleanupCampFaceDbJob> _logger;

        public CleanupCampFaceDbJob(
            IPythonAiService pythonAiService,
            IUnitOfWork unitOfWork,
            ILogger<CleanupCampFaceDbJob> logger)
        {
            _pythonAiService = pythonAiService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Executes the cleanup job for a specific camp
        /// This is called by Hangfire 1 day after camp ends
        /// </summary>
        /// <param name="campId">The camp ID for which face database should be unloaded</param>
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
        public async Task ExecuteAsync(int campId)
        {
            _logger.LogInformation("[CleanupCampFaceDbJob] Starting cleanup for Camp {CampId}", campId);

            try
            {
                // Get the camp to check its end date
                var camp = await _unitOfWork.Camps.GetByIdAsync(campId);

                if (camp == null)
                {
                    _logger.LogWarning("[CleanupCampFaceDbJob] Camp {CampId} not found, skipping cleanup", campId);
                    return;
                }

                // Verify camp has actually ended
                if (camp.endDate > DateTime.UtcNow)
                {
                    _logger.LogWarning(
                        "[CleanupCampFaceDbJob] Camp {CampId} has not ended yet (EndDate: {EndDate} UTC), skipping cleanup",
                        campId, camp.endDate);
                    return;
                }

                _logger.LogInformation(
                    "[CleanupCampFaceDbJob] Camp {CampId} ended at {EndDate} UTC, proceeding with cleanup",
                    campId, camp.endDate);

                // Generate service token for background job (no user context)
                var serviceToken = _pythonAiService.GenerateJwtToken();

                // Call Python AI service to unload face database
                var result = await _pythonAiService.UnloadCampFaceDbAsync(campId, serviceToken);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "[CleanupCampFaceDbJob] ✅ Successfully cleaned up Camp {CampId}",
                        campId);
                }
                else
                {
                    _logger.LogError(
                        "[CleanupCampFaceDbJob] ❌ Failed for Camp {CampId}. Error: {ErrorMessage}",
                        campId, result.Message);
                    throw new Exception($"Failed to cleanup face database for Camp {campId}: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CleanupCampFaceDbJob] Error for Camp {CampId}", campId);
                throw; // Re-throw to trigger Hangfire retry mechanism
            }
        }

        /// <summary>
        /// Static method to schedule a cleanup job for a specific camp after end time
        /// This should be called when a camp is created or its end date is updated
        /// </summary>
        /// <param name="campId">The camp ID</param>
        /// <param name="endDate">The camp end date/time</param>
        /// <returns>The Hangfire job ID or null if job already exists</returns>
        public static string? ScheduleForCamp(int campId, DateTime endDate)
        {
            // Check if job already exists for this camp
            if (IsJobScheduledForCamp(campId))
            {
                return null; // Job already exists
            }

            // Schedule cleanup at midnight of the day after camp ends
            var cleanupTime = endDate.Date.AddDays(1);

            // If cleanup time is in the past, schedule immediately
            if (cleanupTime < DateTime.UtcNow)
            {
                cleanupTime = DateTime.UtcNow.AddSeconds(5);
            }

            var jobId = BackgroundJob.Schedule<CleanupCampFaceDbJob>(
                job => job.ExecuteAsync(campId),
                cleanupTime);

            return jobId;
        }

        private static bool IsJobScheduledForCamp(int campId)
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var scheduledJobs = monitoringApi.ScheduledJobs(0, int.MaxValue);

            return scheduledJobs.Any(j =>
                j.Value?.Job?.Type == typeof(CleanupCampFaceDbJob) &&
                j.Value?.Job?.Args?.Any(a => a?.ToString() == campId.ToString()) == true);
        }

        /// <summary>
        /// Static method to manually trigger cleanup immediately (for testing or manual intervention)
        /// </summary>
        /// <param name="campId">The camp ID</param>
        /// <returns>The Hangfire job ID</returns>
        public static string TriggerImmediately(int campId)
        {
            var jobId = BackgroundJob.Enqueue<CleanupCampFaceDbJob>(
                job => job.ExecuteAsync(campId));

            return jobId;
        }

        /// <summary>
        /// Static method to cancel a scheduled cleanup job
        /// </summary>
        /// <param name="jobId">The Hangfire job ID to cancel</param>
        /// <returns>True if job was cancelled successfully</returns>
        public static bool CancelJob(string jobId)
        {
            return BackgroundJob.Delete(jobId);
        }
    }
}
