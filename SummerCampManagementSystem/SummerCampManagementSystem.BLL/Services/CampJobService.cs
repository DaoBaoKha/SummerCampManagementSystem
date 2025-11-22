using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.DTOs.CampJob;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Services
{
    public class CampJobService : ICampJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICampStatusService _campStatusService;
        private readonly ILogger<CampJobService> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public CampJobService(
            IUnitOfWork unitOfWork,
            ICampStatusService campStatusService,
            ILogger<CampJobService> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork;
            _campStatusService = campStatusService;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task ScheduleJobsForCampAsync(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);

            if (camp == null)
            {
                throw new KeyNotFoundException($"Camp with ID {campId} not found.");
            }

            // Validate camp dates
            ValidateCampDates(camp);

            // Delete existing jobs first
            await DeleteAllJobsForCampAsync(campId);

            DateTime utcNow = DateTime.UtcNow;

            // Schedule RegistrationStart job
            if (camp.registrationStartDate.HasValue)
            {
                // Database stores UTC, so treat the value as UTC
                var registrationStartUtc = DateTime.SpecifyKind(camp.registrationStartDate.Value, DateTimeKind.Utc);
                
                if (registrationStartUtc > utcNow)
                {
                    var jobId = BackgroundJob.Schedule(
                        () => ExecuteStatusTransitionJobAsync(campId, CampStatus.OpenForRegistration, GetJobName(campId, "RegistrationStart")),
                        registrationStartUtc - utcNow);

                    _logger.LogInformation($"Scheduled job '{GetJobName(campId, "RegistrationStart")}' for Camp ID {campId} at {registrationStartUtc:yyyy-MM-dd HH:mm:ss} UTC (Vietnam: {registrationStartUtc.ToVietnamTime():yyyy-MM-dd HH:mm:ss}). JobId: {jobId}");
                }
            }

            // Schedule RegistrationEnd job
            if (camp.registrationEndDate.HasValue)
            {
                var registrationEndUtc = DateTime.SpecifyKind(camp.registrationEndDate.Value, DateTimeKind.Utc);
                
                if (registrationEndUtc > utcNow)
                {
                    var jobId = BackgroundJob.Schedule(
                        () => ExecuteStatusTransitionJobAsync(campId, CampStatus.RegistrationClosed, GetJobName(campId, "RegistrationEnd")),
                        registrationEndUtc - utcNow);

                    _logger.LogInformation($"Scheduled job '{GetJobName(campId, "RegistrationEnd")}' for Camp ID {campId} at {registrationEndUtc:yyyy-MM-dd HH:mm:ss} UTC (Vietnam: {registrationEndUtc.ToVietnamTime():yyyy-MM-dd HH:mm:ss}). JobId: {jobId}");
                }
            }

            // Schedule Start job
            if (camp.startDate.HasValue)
            {
                var startDateUtc = DateTime.SpecifyKind(camp.startDate.Value, DateTimeKind.Utc);
                
                if (startDateUtc > utcNow)
                {
                    var jobId = BackgroundJob.Schedule(
                        () => ExecuteStatusTransitionJobAsync(campId, CampStatus.InProgress, GetJobName(campId, "Start")),
                        startDateUtc - utcNow);

                    _logger.LogInformation($"Scheduled job '{GetJobName(campId, "Start")}' for Camp ID {campId} at {startDateUtc:yyyy-MM-dd HH:mm:ss} UTC (Vietnam: {startDateUtc.ToVietnamTime():yyyy-MM-dd HH:mm:ss}). JobId: {jobId}");
                }
            }

            // Schedule End job
            if (camp.endDate.HasValue)
            {
                var endDateUtc = DateTime.SpecifyKind(camp.endDate.Value, DateTimeKind.Utc);
                
                if (endDateUtc > utcNow)
                {
                    var jobId = BackgroundJob.Schedule(
                        () => ExecuteStatusTransitionJobAsync(campId, CampStatus.Completed, GetJobName(campId, "End")),
                        endDateUtc - utcNow);

                    _logger.LogInformation($"Scheduled job '{GetJobName(campId, "End")}' for Camp ID {campId} at {endDateUtc:yyyy-MM-dd HH:mm:ss} UTC (Vietnam: {endDateUtc.ToVietnamTime():yyyy-MM-dd HH:mm:ss}). JobId: {jobId}");
                }
            }
        }

        public async Task DeleteAllJobsForCampAsync(int campId)
        {
            var jobNames = new[]
            {
                GetJobName(campId, "RegistrationStart"),
                GetJobName(campId, "RegistrationEnd"),
                GetJobName(campId, "Start"),
                GetJobName(campId, "End")
            };

            using (var connection = JobStorage.Current.GetConnection())
            {
                var recurringJobs = connection.GetRecurringJobs();
                var scheduledJobs = connection.GetAllItemsFromSet("schedule");

                // Delete recurring jobs matching camp
                foreach (var job in recurringJobs.Where(j => jobNames.Contains(j.Id)))
                {
                    RecurringJob.RemoveIfExists(job.Id);
                    _logger.LogInformation($"Deleted recurring job: {job.Id}");
                }

                // Delete scheduled jobs (we need to search by job data since Hangfire doesn't index by name easily)
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var scheduledJobsList = monitoringApi.ScheduledJobs(0, int.MaxValue);

                foreach (var job in scheduledJobsList)
                {
                    var jobData = connection.GetJobData(job.Key);
                    if (jobData?.Job?.Args != null && jobData.Job.Args.Count >= 3)
                    {
                        // Check if this job belongs to our camp
                        var jobNameArg = jobData.Job.Args[2] as string;
                        if (jobNameArg != null && jobNames.Contains(jobNameArg))
                        {
                            _backgroundJobClient.Delete(job.Key);
                            _logger.LogInformation($"Deleted scheduled job: {job.Key} ({jobNameArg})");
                        }
                    }
                }
            }

            await Task.CompletedTask;
        }

        public async Task<CampJobListDto> GetJobsForCampAsync(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);

            if (camp == null)
            {
                throw new KeyNotFoundException($"Camp with ID {campId} not found.");
            }

            var result = new CampJobListDto
            {
                CampId = campId,
                CampName = camp.name,
                Jobs = new List<CampJobInfoDto>()
            };

            // Convert all database dates to UTC for proper display
            var jobNames = new[]
            {
                (GetJobName(campId, "RegistrationStart"), 
                 camp.registrationStartDate.HasValue ? DateTime.SpecifyKind(camp.registrationStartDate.Value, DateTimeKind.Utc) : (DateTime?)null, 
                 CampStatus.OpenForRegistration),
                (GetJobName(campId, "RegistrationEnd"), 
                 camp.registrationEndDate.HasValue ? DateTime.SpecifyKind(camp.registrationEndDate.Value, DateTimeKind.Utc) : (DateTime?)null, 
                 CampStatus.RegistrationClosed),
                (GetJobName(campId, "Start"), 
                 camp.startDate.HasValue ? DateTime.SpecifyKind(camp.startDate.Value, DateTimeKind.Utc) : (DateTime?)null, 
                 CampStatus.InProgress),
                (GetJobName(campId, "End"), 
                 camp.endDate.HasValue ? DateTime.SpecifyKind(camp.endDate.Value, DateTimeKind.Utc) : (DateTime?)null, 
                 CampStatus.Completed)
            };

            using (var connection = JobStorage.Current.GetConnection())
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();

                foreach (var (jobName, scheduledTime, targetStatus) in jobNames)
                {
                    var jobInfo = new CampJobInfoDto
                    {
                        JobName = jobName,
                        ScheduledTime = scheduledTime ?? DateTime.MinValue,
                        TargetStatus = targetStatus.ToString(),
                        Status = "NotScheduled"
                    };

                    // Search for this job in scheduled jobs
                    var scheduledJobs = monitoringApi.ScheduledJobs(0, int.MaxValue);
                    var matchingJob = scheduledJobs.FirstOrDefault(j =>
                    {
                        var jobData = connection.GetJobData(j.Key);
                        return jobData?.Job?.Args != null &&
                               jobData.Job.Args.Count >= 3 &&
                               jobData.Job.Args[2] as string == jobName;
                    });

                    if (matchingJob.Key != null)
                    {
                        jobInfo.JobId = matchingJob.Key;
                        jobInfo.Status = "Scheduled";
                    }
                    else
                    {
                        // Check if job succeeded
                        var succeededJobs = monitoringApi.SucceededJobs(0, int.MaxValue);
                        var succeededJob = succeededJobs.FirstOrDefault(j =>
                        {
                            var jobData = connection.GetJobData(j.Key);
                            return jobData?.Job?.Args != null &&
                                   jobData.Job.Args.Count >= 3 &&
                                   jobData.Job.Args[2] as string == jobName;
                        });

                        if (succeededJob.Key != null)
                        {
                            jobInfo.JobId = succeededJob.Key;
                            jobInfo.Status = "Succeeded";
                            jobInfo.LastExecutionTime = succeededJob.Value.SucceededAt;
                            jobInfo.LastExecutionResult = "Success";
                        }
                        else
                        {
                            // Check if job failed
                            var failedJobs = monitoringApi.FailedJobs(0, int.MaxValue);
                            var failedJob = failedJobs.FirstOrDefault(j =>
                            {
                                var jobData = connection.GetJobData(j.Key);
                                return jobData?.Job?.Args != null &&
                                       jobData.Job.Args.Count >= 3 &&
                                       jobData.Job.Args[2] as string == jobName;
                            });

                            if (failedJob.Key != null)
                            {
                                jobInfo.JobId = failedJob.Key;
                                jobInfo.Status = "Failed";
                                jobInfo.LastExecutionTime = failedJob.Value.FailedAt;
                                jobInfo.LastExecutionResult = "Failed";
                                jobInfo.FailureReason = failedJob.Value.ExceptionMessage;
                            }
                        }
                    }

                    result.Jobs.Add(jobInfo);
                }
            }

            return result;
        }

        public async Task<JobExecutionResultDto> ForceRunJobAsync(string jobName)
        {
            // Extract campId and job type from job name
            if (!TryParseJobName(jobName, out int campId, out string jobType))
            {
                throw new ArgumentException($"Invalid job name format: {jobName}");
            }

            var targetStatus = jobType switch
            {
                "RegistrationStart" => CampStatus.OpenForRegistration,
                "RegistrationEnd" => CampStatus.RegistrationClosed,
                "Start" => CampStatus.InProgress,
                "End" => CampStatus.Completed,
                _ => throw new ArgumentException($"Unknown job type: {jobType}")
            };

            var result = new JobExecutionResultDto
            {
                JobName = jobName,
                ExecutionTime = DateTime.UtcNow
            };

            try
            {
                var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
                if (camp == null)
                {
                    result.Success = false;
                    result.Message = $"Camp with ID {campId} not found.";
                    return result;
                }

                result.OldStatus = camp.status;

                var success = await _campStatusService.TransitionCampStatusSafeAsync(campId, targetStatus, $"ForceRun:{jobName}");

                result.Success = success;
                result.NewStatus = success ? targetStatus.ToString() : result.OldStatus;
                result.Message = success
                    ? $"Successfully transitioned camp to {targetStatus}"
                    : "Transition failed. Check logs for details.";

                _logger.LogInformation($"Force-run job '{jobName}' completed. Success: {success}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error executing job: {ex.Message}";
                _logger.LogError(ex, $"Error force-running job '{jobName}'");
            }

            return result;
        }

        public async Task RebuildJobsForCampAsync(int campId)
        {
            _logger.LogInformation($"Rebuilding jobs for Camp ID {campId}");

            await DeleteAllJobsForCampAsync(campId);
            await ScheduleJobsForCampAsync(campId);

            _logger.LogInformation($"Successfully rebuilt jobs for Camp ID {campId}");
        }

        public async Task<List<CampJobInfoDto>> GetAllJobsAsync()
        {
            var allJobs = new List<CampJobInfoDto>();

            using (var connection = JobStorage.Current.GetConnection())
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();

                // Get all scheduled jobs
                var scheduledJobs = monitoringApi.ScheduledJobs(0, int.MaxValue);
                foreach (var job in scheduledJobs)
                {
                    var jobData = connection.GetJobData(job.Key);
                    if (jobData?.Job?.Args != null && jobData.Job.Args.Count >= 3)
                    {
                        var jobName = jobData.Job.Args[2] as string;
                        if (jobName != null && jobName.StartsWith("Camp_"))
                        {
                            allJobs.Add(new CampJobInfoDto
                            {
                                JobId = job.Key,
                                JobName = jobName,
                                ScheduledTime = job.Value.EnqueueAt,
                                Status = "Scheduled"
                            });
                        }
                    }
                }

                // Get succeeded jobs
                var succeededJobs = monitoringApi.SucceededJobs(0, 1000);
                foreach (var job in succeededJobs)
                {
                    var jobData = connection.GetJobData(job.Key);
                    if (jobData?.Job?.Args != null && jobData.Job.Args.Count >= 3)
                    {
                        var jobName = jobData.Job.Args[2] as string;
                        if (jobName != null && jobName.StartsWith("Camp_"))
                        {
                            allJobs.Add(new CampJobInfoDto
                            {
                                JobId = job.Key,
                                JobName = jobName,
                                Status = "Succeeded",
                                LastExecutionTime = job.Value.SucceededAt,
                                LastExecutionResult = "Success"
                            });
                        }
                    }
                }

                // Get failed jobs
                var failedJobs = monitoringApi.FailedJobs(0, 1000);
                foreach (var job in failedJobs)
                {
                    var jobData = connection.GetJobData(job.Key);
                    if (jobData?.Job?.Args != null && jobData.Job.Args.Count >= 3)
                    {
                        var jobName = jobData.Job.Args[2] as string;
                        if (jobName != null && jobName.StartsWith("Camp_"))
                        {
                            allJobs.Add(new CampJobInfoDto
                            {
                                JobId = job.Key,
                                JobName = jobName,
                                Status = "Failed",
                                LastExecutionTime = job.Value.FailedAt,
                                LastExecutionResult = "Failed",
                                FailureReason = job.Value.ExceptionMessage
                            });
                        }
                    }
                }
            }

            return await Task.FromResult(allJobs);
        }

        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 600 })]
        public async Task ExecuteStatusTransitionJobAsync(int campId, CampStatus targetStatus, string jobName)
        {
            _logger.LogInformation($"Executing job '{jobName}' for Camp ID {campId}, target status: {targetStatus}");

            try
            {
                var success = await _campStatusService.TransitionCampStatusSafeAsync(campId, targetStatus, $"HangfireJob:{jobName}");

                if (!success)
                {
                    throw new InvalidOperationException($"Failed to transition Camp ID {campId} to {targetStatus}. Check logs for details.");
                }

                _logger.LogInformation($"Job '{jobName}' completed successfully for Camp ID {campId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Job '{jobName}' failed for Camp ID {campId}. Error: {ex.Message}");
                throw;
            }
        }

        #region Private Helper Methods

        private string GetJobName(int campId, string jobType)
        {
            return $"Camp_{campId}_{jobType}";
        }

        private bool TryParseJobName(string jobName, out int campId, out string jobType)
        {
            campId = 0;
            jobType = null;

            if (string.IsNullOrWhiteSpace(jobName) || !jobName.StartsWith("Camp_"))
            {
                return false;
            }

            var parts = jobName.Split('_');
            if (parts.Length != 3)
            {
                return false;
            }

            if (!int.TryParse(parts[1], out campId))
            {
                return false;
            }

            jobType = parts[2];
            return true;
        }

        private void ValidateCampDates(DAL.Models.Camp camp)
        {
            if (!camp.registrationStartDate.HasValue ||
                !camp.registrationEndDate.HasValue ||
                !camp.startDate.HasValue ||
                !camp.endDate.HasValue)
            {
                throw new ArgumentException("Camp must have all required dates set (registrationStartDate, registrationEndDate, startDate, endDate).");
            }

            // Treat all database dates as UTC for comparison
            var registrationStartUtc = DateTime.SpecifyKind(camp.registrationStartDate.Value, DateTimeKind.Utc);
            var registrationEndUtc = DateTime.SpecifyKind(camp.registrationEndDate.Value, DateTimeKind.Utc);
            var startDateUtc = DateTime.SpecifyKind(camp.startDate.Value, DateTimeKind.Utc);
            var endDateUtc = DateTime.SpecifyKind(camp.endDate.Value, DateTimeKind.Utc);

            // Validate: registrationStartDate < registrationEndDate
            if (registrationStartUtc >= registrationEndUtc)
            {
                throw new ArgumentException("Registration start date must be before registration end date.");
            }

            // Validate: registrationEndDate <= startDate
            if (registrationEndUtc > startDateUtc)
            {
                throw new ArgumentException("Registration end date must be before or equal to camp start date.");
            }

            // Validate: startDate < endDate
            if (startDateUtc >= endDateUtc)
            {
                throw new ArgumentException("Camp start date must be before camp end date.");
            }

            // Validate: All dates must be in the future
            DateTime utcNow = DateTime.UtcNow;

            if (registrationStartUtc <= utcNow)
            {
                throw new ArgumentException($"Registration start date must be in the future. Current UTC: {utcNow:yyyy-MM-dd HH:mm:ss}, Provided: {registrationStartUtc:yyyy-MM-dd HH:mm:ss} UTC (Vietnam: {registrationStartUtc.ToVietnamTime():yyyy-MM-dd HH:mm:ss})");
            }

            _logger.LogInformation($"Camp ID {camp.campId} dates validated successfully. Registration starts at {registrationStartUtc:yyyy-MM-dd HH:mm:ss} UTC (Vietnam: {registrationStartUtc.ToVietnamTime():yyyy-MM-dd HH:mm:ss})");
        }

        #endregion
    }
}
