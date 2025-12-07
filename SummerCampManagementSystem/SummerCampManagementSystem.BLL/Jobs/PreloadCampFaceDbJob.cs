using Hangfire;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.Interfaces;
using System;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Jobs
{
    /// <summary>
    /// Hangfire background job that preloads face database into Python AI service
    /// before camp starts (triggered X minutes before StartDate)
    /// </summary>
    public class PreloadCampFaceDbJob
    {
        private readonly IPythonAiService _pythonAiService;
        private readonly ILogger<PreloadCampFaceDbJob> _logger;

        public PreloadCampFaceDbJob(
            IPythonAiService pythonAiService,
            ILogger<PreloadCampFaceDbJob> logger)
        {
            _pythonAiService = pythonAiService;
            _logger = logger;
        }

        /// <summary>
        /// Executes the preload job for a specific camp
        /// This is called by Hangfire before camp starts
        /// </summary>
        /// <param name="campId">The camp ID for which face database should be loaded</param>
        /// <param name="forceReload">Force reload even if already loaded</param>
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
        public async Task ExecuteAsync(int campId, bool forceReload = false)
        {
            _logger.LogInformation("PreloadCampFaceDbJob started for Camp {CampId}", campId);

            try
            {
                // Generate service token for background job (no user context)
                var serviceToken = _pythonAiService.GenerateJwtToken();

                // Call Python AI service to load face database
                var result = await _pythonAiService.LoadCampFaceDbAsync(campId, serviceToken, forceReload);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "PreloadCampFaceDbJob completed successfully for Camp {CampId}. Loaded {FaceCount} faces",
                        campId, result.FaceCount);
                }
                else
                {
                    _logger.LogError(
                        "PreloadCampFaceDbJob failed for Camp {CampId}. Error: {ErrorMessage}",
                        campId, result.Message);
                    throw new Exception($"Failed to preload face database for Camp {campId}: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PreloadCampFaceDbJob encountered an error for Camp {CampId}", campId);
                throw; // Re-throw to trigger Hangfire retry mechanism
            }
        }

        /// <summary>
        /// Static method to schedule a preload job for a specific camp before start time
        /// This should be called when a camp is created or its start date is updated
        /// </summary>
        /// <param name="campId">The camp ID</param>
        /// <param name="startDate">The camp start date/time</param>
        /// <param name="bufferMinutes">Minutes before start time to trigger preload (default: 10)</param>
        /// <returns>The Hangfire job ID or null if job already exists</returns>
        public static string? ScheduleForCamp(int campId, DateTime startDate, int bufferMinutes = 10)
        {
            // Check if job already exists for this camp
            if (IsJobScheduledForCamp(campId))
            {
                return null; // Job already exists
            }

            var preloadTime = startDate.AddMinutes(-bufferMinutes);

            // If preload time is in the past, schedule immediately
            if (preloadTime < DateTime.UtcNow)
            {
                preloadTime = DateTime.UtcNow.AddSeconds(5);
            }

            var jobId = BackgroundJob.Schedule<PreloadCampFaceDbJob>(
                job => job.ExecuteAsync(campId, false),
                preloadTime);

            return jobId;
        }

        private static bool IsJobScheduledForCamp(int campId)
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var scheduledJobs = monitoringApi.ScheduledJobs(0, int.MaxValue);

            return scheduledJobs.Any(j =>
                j.Value?.Job?.Type == typeof(PreloadCampFaceDbJob) &&
                j.Value?.Job?.Args?.Any(a => a?.ToString() == campId.ToString()) == true);
        }

        /// <summary>
        /// Static method to manually trigger preload immediately (for testing or manual intervention)
        /// </summary>
        /// <param name="campId">The camp ID</param>
        /// <param name="forceReload">Force reload even if already loaded</param>
        /// <returns>The Hangfire job ID</returns>
        public static string TriggerImmediately(int campId, bool forceReload = false)
        {
            var jobId = BackgroundJob.Enqueue<PreloadCampFaceDbJob>(
                job => job.ExecuteAsync(campId, forceReload));

            return jobId;
        }

        /// <summary>
        /// Static method to cancel a scheduled preload job
        /// </summary>
        /// <param name="jobId">The Hangfire job ID to cancel</param>
        /// <returns>True if job was cancelled successfully</returns>
        public static bool CancelJob(string jobId)
        {
            return BackgroundJob.Delete(jobId);
        }
    }
}
