using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Jobs;

namespace SummerCampManagementSystem.Controllers
{
    /// <summary>
    /// API endpoints for managing attendance folder structure for facial recognition
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceFolderController : ControllerBase
    {
        private readonly IAttendanceFolderService _attendanceFolderService;
        private readonly ILogger<AttendanceFolderController> _logger;

        public AttendanceFolderController(
            IAttendanceFolderService attendanceFolderService,
            ILogger<AttendanceFolderController> logger)
        {
            _attendanceFolderService = attendanceFolderService;
            _logger = logger;
        }

        /// <summary>
        /// Manually triggers folder creation for a specific camp (for testing/admin purposes)
        /// </summary>
        /// <param name="campId">The camp ID for which folders should be created</param>
        /// <returns>Success or error message</returns>
        [HttpPost("create-folders/{campId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateFoldersForCamp(int campId)
        {
            try
            {
                _logger.LogInformation("Manual folder creation requested for Camp {CampId}", campId);

                var success = await _attendanceFolderService.CreateAttendanceFoldersForCampAsync(campId);

                if (success)
                {
                    return Ok(new
                    {
                        status = 200,
                        message = $"Successfully created attendance folders for Camp {campId}",
                        campId
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        status = 400,
                        message = $"Failed to create attendance folders for Camp {campId}",
                        campId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folders for Camp {CampId}", campId);
                return StatusCode(500, new
                {
                    status = 500,
                    message = "Internal server error while creating folders",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Checks if attendance folders already exist for a camp (idempotency check)
        /// </summary>
        /// <param name="campId">The camp ID to check</param>
        /// <returns>Boolean indicating whether folders exist</returns>
        [HttpGet("check-folders/{campId}")]
        [Authorize(Roles = "Admin,Manager,Staff")]
        public async Task<IActionResult> CheckFoldersExist(int campId)
        {
            try
            {
                var exists = await _attendanceFolderService.FoldersExistForCampAsync(campId);

                return Ok(new
                {
                    status = 200,
                    campId,
                    foldersExist = exists,
                    message = exists
                        ? $"Folders already exist for Camp {campId}"
                        : $"Folders do not exist for Camp {campId}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking folders for Camp {CampId}", campId);
                return StatusCode(500, new
                {
                    status = 500,
                    message = "Internal server error while checking folders",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Schedules a Hangfire job to create folders immediately (for testing)
        /// </summary>
        /// <param name="campId">The camp ID</param>
        /// <returns>Job ID</returns>
        [HttpPost("schedule-job/{campId}")]
        [Authorize(Roles = "Admin")]
        public IActionResult ScheduleJobImmediately(int campId)
        {
            try
            {
                var jobId = AttendanceFolderCreationJob.TriggerImmediately(campId);

                return Ok(new
                {
                    status = 200,
                    message = $"Scheduled immediate folder creation job for Camp {campId}",
                    campId,
                    jobId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling job for Camp {CampId}", campId);
                return StatusCode(500, new
                {
                    status = 500,
                    message = "Internal server error while scheduling job",
                    error = ex.Message
                });
            }
        }
    }
}
