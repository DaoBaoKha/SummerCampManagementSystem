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
    /// Hangfire recurring job that preloads face databases at 19:00 UTC (02:00 UTC+7) daily
    /// Preloads camps that start within the next 24 hours
    /// </summary>
    public class PreloadCampFaceDbJob
    {
        private readonly IPythonAiService _pythonAiService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PreloadCampFaceDbJob> _logger;

        public PreloadCampFaceDbJob(
            IPythonAiService pythonAiService,
            IUnitOfWork unitOfWork,
            ILogger<PreloadCampFaceDbJob> logger)
        {
            _pythonAiService = pythonAiService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Executes the preload job - checks for camps starting soon and preloads them
        /// This is called by Hangfire daily at 19:00 UTC (02:00 UTC+7)
        /// </summary>
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("[PreloadCampFaceDbJob] Starting daily preload check at {UtcTime} UTC", DateTime.UtcNow);

            try
            {
                var nowUtc = DateTime.UtcNow;

                // Find camps that start within the next 24 hours (database stores UTC)
                var preloadWindowStart = nowUtc;
                var preloadWindowEnd = nowUtc.AddHours(24);

                var allCamps = await _unitOfWork.Camps.GetAllAsync();
                var campsToPreload = allCamps
                    .Where(c =>
                        c.startDate.HasValue &&
                        c.endDate.HasValue &&
                        c.status == "Active" &&
                        c.startDate.Value >= preloadWindowStart &&
                        c.startDate.Value <= preloadWindowEnd)
                    .ToList();

                if (!campsToPreload.Any())
                {
                    _logger.LogInformation("[PreloadCampFaceDbJob] No camps starting in the next 24 hours");
                    return;
                }

                _logger.LogInformation(
                    "[PreloadCampFaceDbJob] Found {Count} camp(s) starting within 24 hours",
                    campsToPreload.Count);

                // Generate service token once for all preloads
                var serviceToken = _pythonAiService.GenerateJwtToken();

                foreach (var camp in campsToPreload)
                {
                    try
                    {
                        // Check if already loaded to avoid duplicate preloads
                        var isLoaded = await _pythonAiService.CheckCampLoadedAsync(camp.campId, serviceToken);

                        if (isLoaded)
                        {
                            _logger.LogInformation(
                                "[PreloadCampFaceDbJob] Camp {CampId} already loaded, skipping",
                                camp.campId);
                            continue;
                        }

                        _logger.LogInformation(
                            "[PreloadCampFaceDbJob] Preloading camp {CampId} (starts at {StartDate} UTC)",
                            camp.campId,
                            camp.startDate.Value);

                        var result = await _pythonAiService.LoadCampFaceDbAsync(
                            camp.campId,
                            serviceToken,
                            forceReload: false);

                        if (result.Success)
                        {
                            _logger.LogInformation(
                                "[PreloadCampFaceDbJob] ✅ Successfully preloaded camp {CampId} ({FaceCount} faces)",
                                camp.campId,
                                result.FaceCount);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[PreloadCampFaceDbJob] ⚠️ Failed to preload camp {CampId}: {Message}",
                                camp.campId,
                                result.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "[PreloadCampFaceDbJob] Error preloading camp {CampId}",
                            camp.campId);
                        // Continue with next camp even if one fails
                    }
                }

                _logger.LogInformation("[PreloadCampFaceDbJob] Daily preload check completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PreloadCampFaceDbJob] Error during preload check");
                throw; // Re-throw to trigger Hangfire retry
            }
        }
    }
}
