using Hangfire;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.Interfaces;
using System;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Jobs
{
    /// <summary>
    /// Hangfire background job that creates folder structure for facial recognition
    /// when camp registration closes
    /// </summary>
    public class AttendanceFolderCreationJob
    {
        private readonly IAttendanceFolderService _attendanceFolderService;
        private readonly ILogger<AttendanceFolderCreationJob> _logger;

        public AttendanceFolderCreationJob(
            IAttendanceFolderService attendanceFolderService,
            ILogger<AttendanceFolderCreationJob> logger)
        {
            _attendanceFolderService = attendanceFolderService;
            _logger = logger;
        }

        /// <summary>
        /// Executes the folder creation job for a specific camp
        /// This is called by Hangfire when registration closes
        /// </summary>
        /// <param name="campId">The camp ID for which folders should be created</param>
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
        public async Task ExecuteAsync(int campId)
        {
            _logger.LogInformation("AttendanceFolderCreationJob started for Camp {CampId}", campId);

            try
            {
                var success = await _attendanceFolderService.CreateAttendanceFoldersForCampAsync(campId);

                if (success)
                {
                    _logger.LogInformation("AttendanceFolderCreationJob completed successfully for Camp {CampId}", campId);
                }
                else
                {
                    _logger.LogError("AttendanceFolderCreationJob failed for Camp {CampId}", campId);
                    throw new Exception($"Failed to create attendance folders for Camp {campId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AttendanceFolderCreationJob encountered an error for Camp {CampId}", campId);
                throw; // Re-throw to trigger Hangfire retry mechanism
            }
        }

        /// <summary>
        /// Static method to schedule a job for a specific camp at registration end time
        /// This should be called when a camp is created or its registration end date is updated
        /// </summary>
        /// <param name="campId">The camp ID</param>
        /// <param name="registrationEndDate">The registration end date/time</param>
        /// <returns>The Hangfire job ID</returns>
        public static string ScheduleForCamp(int campId, DateTime registrationEndDate)
        {
            var jobId = BackgroundJob.Schedule<AttendanceFolderCreationJob>(
                job => job.ExecuteAsync(campId),
                registrationEndDate);

            return jobId;
        }

        /// <summary>
        /// Static method to manually trigger folder creation immediately (for testing or manual intervention)
        /// </summary>
        /// <param name="campId">The camp ID</param>
        /// <returns>The Hangfire job ID</returns>
        public static string TriggerImmediately(int campId)
        {
            var jobId = BackgroundJob.Enqueue<AttendanceFolderCreationJob>(
                job => job.ExecuteAsync(campId));

            return jobId;
        }
    }
}
