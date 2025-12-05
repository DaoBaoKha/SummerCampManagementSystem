using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SummerCampManagementSystem.API.Controllers
{
    /// <summary>
    /// Controller for managing Hangfire background jobs
    /// </summary>
    [Route("api/hangfire-management")]
    [ApiController]
    [Authorize] // Require authentication
    public class HangfireManagementController : ControllerBase
    {
        private readonly ILogger<HangfireManagementController> _logger;

        public HangfireManagementController(ILogger<HangfireManagementController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get all scheduled jobs with their details
        /// </summary>
        /// <returns>List of scheduled jobs</returns>
        [HttpGet("scheduled-jobs")]
        public IActionResult GetScheduledJobs()
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var scheduledJobs = monitoringApi.ScheduledJobs(0, int.MaxValue);

                var result = scheduledJobs.Select(job => new
                {
                    JobId = job.Key,
                    JobType = job.Value?.Job?.Type?.Name,
                    Method = job.Value?.Job?.Method?.Name,
                    Arguments = job.Value?.Job?.Args,
                    ScheduledAt = job.Value?.ScheduledAt,
                    State = "Scheduled"
                });

                return Ok(new
                {
                    success = true,
                    totalJobs = scheduledJobs.Count,
                    jobs = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scheduled jobs");
                return StatusCode(500, new { success = false, message = "Error retrieving scheduled jobs", error = ex.Message });
            }
        }

        /// <summary>
        /// Get statistics about all Hangfire jobs
        /// </summary>
        /// <returns>Job statistics</returns>
        [HttpGet("stats")]
        public IActionResult GetJobStats()
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();

                var stats = new
                {
                    Scheduled = monitoringApi.ScheduledCount(),
                    Enqueued = monitoringApi.EnqueuedCount("default"),
                    Processing = monitoringApi.ProcessingCount(),
                    Succeeded = monitoringApi.SucceededListCount(),
                    Failed = monitoringApi.FailedCount(),
                    Deleted = monitoringApi.DeletedListCount()
                };

                return Ok(new
                {
                    success = true,
                    stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job statistics");
                return StatusCode(500, new { success = false, message = "Error retrieving job statistics", error = ex.Message });
            }
        }

        /// <summary>
        /// Clear all scheduled jobs (DANGEROUS - use with caution)
        /// </summary>
        /// <returns>Number of jobs deleted</returns>
        [HttpDelete("clear-scheduled")]
        public IActionResult ClearScheduledJobs()
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var scheduledJobs = monitoringApi.ScheduledJobs(0, int.MaxValue);

                int deletedCount = 0;
                foreach (var job in scheduledJobs)
                {
                    if (BackgroundJob.Delete(job.Key))
                    {
                        deletedCount++;
                    }
                }

                _logger.LogWarning("Cleared {Count} scheduled jobs", deletedCount);

                return Ok(new
                {
                    success = true,
                    message = $"Cleared {deletedCount} scheduled jobs",
                    deletedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing scheduled jobs");
                return StatusCode(500, new { success = false, message = "Error clearing scheduled jobs", error = ex.Message });
            }
        }

        /// <summary>
        /// Clear all jobs in all states (VERY DANGEROUS - nuclear option)
        /// </summary>
        /// <returns>Number of jobs deleted by state</returns>
        [HttpDelete("clear-all")]
        public IActionResult ClearAllJobs()
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var results = new Dictionary<string, int>();

                // Clear scheduled jobs
                var scheduledJobs = monitoringApi.ScheduledJobs(0, int.MaxValue);
                int scheduledDeleted = 0;
                foreach (var job in scheduledJobs)
                {
                    if (BackgroundJob.Delete(job.Key))
                    {
                        scheduledDeleted++;
                    }
                }
                results["Scheduled"] = scheduledDeleted;

                // Clear enqueued jobs
                var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, int.MaxValue);
                int enqueuedDeleted = 0;
                foreach (var job in enqueuedJobs)
                {
                    if (BackgroundJob.Delete(job.Key))
                    {
                        enqueuedDeleted++;
                    }
                }
                results["Enqueued"] = enqueuedDeleted;

                // Clear processing jobs
                var processingJobs = monitoringApi.ProcessingJobs(0, int.MaxValue);
                int processingDeleted = 0;
                foreach (var job in processingJobs)
                {
                    if (BackgroundJob.Delete(job.Key))
                    {
                        processingDeleted++;
                    }
                }
                results["Processing"] = processingDeleted;

                // Clear failed jobs
                var failedJobs = monitoringApi.FailedJobs(0, int.MaxValue);
                int failedDeleted = 0;
                foreach (var job in failedJobs)
                {
                    if (BackgroundJob.Delete(job.Key))
                    {
                        failedDeleted++;
                    }
                }
                results["Failed"] = failedDeleted;

                int totalDeleted = results.Values.Sum();

                _logger.LogWarning("CLEARED ALL JOBS: {TotalCount} jobs deleted. Details: {Details}",
                    totalDeleted, string.Join(", ", results.Select(r => $"{r.Key}: {r.Value}")));

                return Ok(new
                {
                    success = true,
                    message = $"Cleared {totalDeleted} jobs across all states",
                    totalDeleted,
                    details = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all jobs");
                return StatusCode(500, new { success = false, message = "Error clearing all jobs", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a specific job by ID
        /// </summary>
        /// <param name="jobId">The Hangfire job ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("job/{jobId}")]
        public IActionResult DeleteJob(string jobId)
        {
            try
            {
                var success = BackgroundJob.Delete(jobId);

                if (success)
                {
                    _logger.LogInformation("Deleted job {JobId}", jobId);
                    return Ok(new { success = true, message = $"Job {jobId} deleted successfully" });
                }
                else
                {
                    return NotFound(new { success = false, message = $"Job {jobId} not found or already deleted" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job {JobId}", jobId);
                return StatusCode(500, new { success = false, message = "Error deleting job", error = ex.Message });
            }
        }

        /// <summary>
        /// Clear jobs for a specific camp
        /// </summary>
        /// <param name="campId">The camp ID</param>
        /// <returns>Number of jobs deleted</returns>
        [HttpDelete("camp/{campId}")]
        public IActionResult ClearJobsForCamp(int campId)
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var scheduledJobs = monitoringApi.ScheduledJobs(0, int.MaxValue);

                int deletedCount = 0;
                foreach (var job in scheduledJobs)
                {
                    // Check if job arguments contain the camp ID
                    var hasCampId = job.Value?.Job?.Args?.Any(a => a?.ToString() == campId.ToString()) == true;

                    if (hasCampId)
                    {
                        if (BackgroundJob.Delete(job.Key))
                        {
                            deletedCount++;
                            _logger.LogInformation("Deleted job {JobId} for Camp {CampId}", job.Key, campId);
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = $"Cleared {deletedCount} jobs for Camp {campId}",
                    campId,
                    deletedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing jobs for Camp {CampId}", campId);
                return StatusCode(500, new { success = false, message = "Error clearing camp jobs", error = ex.Message });
            }
        }
    }
}
