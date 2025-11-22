using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.CampJob;
using SummerCampManagementSystem.BLL.Interfaces;
using System;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.API.Controllers
{
    /// <summary>
    /// Controller for managing Hangfire background jobs related to camp status transitions
    /// </summary>
    [Route("api/camp-jobs")]
    [ApiController]
    [Authorize]
    public class CampJobController : ControllerBase
    {
        private readonly ICampJobService _campJobService;
        private readonly ILogger<CampJobController> _logger;

        public CampJobController(ICampJobService campJobService, ILogger<CampJobController> logger)
        {
            _campJobService = campJobService;
            _logger = logger;
        }

        /// <summary>
        /// Get all scheduled jobs for a specific camp
        /// </summary>
        /// <param name="campId">The camp ID</param>
        /// <returns>List of jobs with their status and schedule information</returns>
        [HttpGet("{campId}")]
        public async Task<ActionResult<CampJobListDto>> GetJobsForCamp(int campId)
        {
            try
            {
                var jobs = await _campJobService.GetJobsForCampAsync(campId);
                return Ok(jobs);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving jobs for Camp ID {campId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving camp jobs." });
            }
        }

        /// <summary>
        /// Force-run a specific job manually (admin override)
        /// </summary>
        /// <param name="jobName">The job name (e.g., Camp_1_RegistrationStart)</param>
        /// <returns>Result of the job execution</returns>
        [HttpPost("run/{jobName}")]
        public async Task<ActionResult<JobExecutionResultDto>> ForceRunJob(string jobName)
        {
            try
            {
                _logger.LogInformation($"Force-running job: {jobName}");
                var result = await _campJobService.ForceRunJobAsync(jobName);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error force-running job '{jobName}'");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while executing the job." });
            }
        }

        /// <summary>
        /// Delete all scheduled jobs for a specific camp
        /// </summary>
        /// <param name="campId">The camp ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{campId}")]
        public async Task<IActionResult> DeleteJobsForCamp(int campId)
        {
            try
            {
                await _campJobService.DeleteAllJobsForCampAsync(campId);
                _logger.LogInformation($"Deleted all jobs for Camp ID {campId}");
                return Ok(new { message = $"All jobs for Camp ID {campId} have been deleted." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting jobs for Camp ID {campId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while deleting camp jobs." });
            }
        }

        /// <summary>
        /// Rebuild all jobs for a specific camp (delete old + create new)
        /// </summary>
        /// <param name="campId">The camp ID</param>
        /// <returns>Success message</returns>
        [HttpPost("rebuild/{campId}")]
        public async Task<IActionResult> RebuildJobsForCamp(int campId)
        {
            try
            {
                await _campJobService.RebuildJobsForCampAsync(campId);
                _logger.LogInformation($"Rebuilt all jobs for Camp ID {campId}");
                return Ok(new { message = $"All jobs for Camp ID {campId} have been rebuilt." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rebuilding jobs for Camp ID {campId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while rebuilding camp jobs." });
            }
        }

        /// <summary>
        /// Get all Hangfire jobs in the system (for all camps)
        /// </summary>
        /// <returns>List of all camp-related jobs</returns>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllJobs()
        {
            try
            {
                var jobs = await _campJobService.GetAllJobsAsync();
                return Ok(new 
                { 
                    totalJobs = jobs.Count,
                    jobs 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all camp jobs");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving all jobs." });
            }
        }
    }
}
