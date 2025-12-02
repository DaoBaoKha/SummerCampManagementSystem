using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.Jobs;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.HostedServices
{
    /// <summary>
    /// Background service that initializes and reconstructs Hangfire jobs at application startup
    /// Scans all camps and schedules preload/cleanup jobs based on their dates
    /// </summary>
    public class CampBackgroundInitializer : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CampBackgroundInitializer> _logger;
        private readonly int _preloadBufferMinutes;

        public CampBackgroundInitializer(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<CampBackgroundInitializer> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;

            // Get preload buffer from configuration (default: 10 minutes)
            _preloadBufferMinutes = int.TryParse(
                configuration["AttendanceJobSettings:PreDownloadBufferMinutes"],
                out var buffer) ? buffer : 10;
        }

        /// <summary>
        /// Called when the application starts
        /// Reconstructs all Hangfire jobs for camps
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CampBackgroundInitializer started - Rebuilding Hangfire jobs...");

            try
            {
                // Create a scope to resolve scoped services
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // Get all camps from database
                var allCamps = await unitOfWork.Camps.GetAllAsync();
                var now = DateTime.UtcNow;

                _logger.LogInformation("Found {CampCount} camps in database", allCamps.Count());

                int preloadJobsScheduled = 0;
                int cleanupJobsScheduled = 0;
                int attendanceFolderJobsScheduled = 0;

                foreach (var camp in allCamps)
                {
                    try
                    {
                        // Only process camps that have valid dates
                        if (camp.startDate == null || camp.endDate == null)
                        {
                            _logger.LogWarning("Camp {CampId} has null start/end dates, skipping job scheduling", camp.campId);
                            continue;
                        }

                        var startDate = camp.startDate.Value;
                        var endDate = camp.endDate.Value;
                        var registrationEndDate = camp.registrationEndDate ?? startDate.AddDays(-1);

                        // 1. Schedule attendance folder creation job (if registration period not ended)
                        if (registrationEndDate > now)
                        {
                            var folderJobId = AttendanceFolderCreationJob.ScheduleForCamp(camp.campId, registrationEndDate);
                            _logger.LogInformation(
                                "Scheduled AttendanceFolderCreationJob for Camp {CampId} at {RegistrationEndDate}. JobId: {JobId}",
                                camp.campId, registrationEndDate, folderJobId);
                            attendanceFolderJobsScheduled++;
                        }

                        // 2. Schedule preload job (if camp hasn't started yet)
                        var preloadTime = startDate.AddMinutes(-_preloadBufferMinutes);
                        if (preloadTime > now)
                        {
                            var preloadJobId = PreloadCampFaceDbJob.ScheduleForCamp(camp.campId, startDate, _preloadBufferMinutes);
                            _logger.LogInformation(
                                "Scheduled PreloadCampFaceDbJob for Camp {CampId} at {PreloadTime}. JobId: {JobId}",
                                camp.campId, preloadTime, preloadJobId);
                            preloadJobsScheduled++;
                        }
                        else if (startDate > now && endDate > now)
                        {
                            // Camp is starting soon but preload time passed - trigger immediately
                            var immediateJobId = PreloadCampFaceDbJob.TriggerImmediately(camp.campId, forceReload: false);
                            _logger.LogInformation(
                                "Triggered immediate PreloadCampFaceDbJob for Camp {CampId} (preload time passed). JobId: {JobId}",
                                camp.campId, immediateJobId);
                            preloadJobsScheduled++;
                        }

                        // 3. Schedule cleanup job (for all camps, including ongoing ones)
                        if (endDate > now)
                        {
                            var cleanupJobId = CleanupCampFaceDbJob.ScheduleForCamp(camp.campId, endDate);
                            _logger.LogInformation(
                                "Scheduled CleanupCampFaceDbJob for Camp {CampId} at {CleanupTime}. JobId: {JobId}",
                                camp.campId, endDate.Date.AddDays(1), cleanupJobId);
                            cleanupJobsScheduled++;
                        }
                        else
                        {
                            // Camp already ended - check if cleanup is needed
                            _logger.LogDebug(
                                "Camp {CampId} already ended on {EndDate}, cleanup job not scheduled",
                                camp.campId, endDate);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error scheduling jobs for Camp {CampId}", camp.campId);
                        // Continue with next camp
                    }
                }

                _logger.LogInformation(
                    "CampBackgroundInitializer completed. Scheduled: {AttendanceFolderJobs} attendance folder jobs, {PreloadJobs} preload jobs, {CleanupJobs} cleanup jobs",
                    attendanceFolderJobsScheduled, preloadJobsScheduled, cleanupJobsScheduled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in CampBackgroundInitializer");
                // Don't throw - allow application to continue even if job reconstruction fails
            }
        }

        /// <summary>
        /// Called when the application is stopping
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CampBackgroundInitializer stopped");
            return Task.CompletedTask;
        }
    }
}
