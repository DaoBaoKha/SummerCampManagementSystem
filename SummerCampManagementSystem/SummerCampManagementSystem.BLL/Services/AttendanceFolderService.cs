using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Services
{
    /// <summary>
    /// Service for managing attendance session folder structure in Supabase storage
    /// </summary>
    public class AttendanceFolderService : IAttendanceFolderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Client _supabaseClient;
        private readonly ILogger<AttendanceFolderService> _logger;
        private const string BUCKET_NAME = "attendance-sessions";
        private const string MARKER_FILE_NAME = ".folder_marker";

        public AttendanceFolderService(
            IUnitOfWork unitOfWork,
            Client supabaseClient,
            ILogger<AttendanceFolderService> logger)
        {
            _unitOfWork = unitOfWork;
            _supabaseClient = supabaseClient;
            _logger = logger;
        }

        /// <summary>
        /// Creates the complete folder hierarchy for a camp when registration closes
        /// </summary>
        public async Task<bool> CreateAttendanceFoldersForCampAsync(int campId)
        {
            try
            {
                _logger.LogInformation("Starting folder creation for Camp {CampId}", campId);

                // Check if folders already exist (idempotency)
                if (await FoldersExistForCampAsync(campId))
                {
                    _logger.LogInformation("Folders already exist for Camp {CampId}, skipping creation", campId);
                    return true;
                }

                // Get the camp to verify it exists
                var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
                if (camp == null)
                {
                    _logger.LogError("Camp {CampId} not found", campId);
                    return false;
                }

                // Step 1: Create main camp folder
                var campFolderPath = $"camp_{campId}";
                if (!await CreateFolderInBucketAsync(campFolderPath))
                {
                    _logger.LogError("Failed to create camp folder for Camp {CampId}", campId);
                    return false;
                }
                _logger.LogInformation("Created camp folder: {FolderPath}", campFolderPath);

                // Step 2: Get the CamperGroup for this camp (for core activities)
                var camperGroups = await _unitOfWork.CamperGroups
                    .GetByCampIdAsync(campId);

                var firstCamperGroup = camperGroups.FirstOrDefault();
                if (firstCamperGroup == null)
                {
                    _logger.LogWarning("No CamperGroup found for Camp {CampId}. Camp may not have core activities yet.", campId);
                }
                else
                {
                    // Create camper_group folder
                    var camperGroupFolderPath = $"{campFolderPath}/camper_group_{firstCamperGroup.camperGroupId}";
                    if (!await CreateFolderInBucketAsync(camperGroupFolderPath))
                    {
                        _logger.LogError("Failed to create camper group folder for CamperGroup {GroupId}", firstCamperGroup.camperGroupId);
                        return false;
                    }
                    _logger.LogInformation("Created camper group folder: {FolderPath}", camperGroupFolderPath);
                }

                // Step 3: Get all ActivitySchedules for optional activities with registered campers
                // ActivitySchedule has isOptional flag and relates to Activity which has campId
                var allOptionalSchedules = await _unitOfWork.ActivitySchedules
                    .GetOptionalScheduleByCampIdAsync(campId);

                // Load with CamperActivities navigation property
                var optionalActivitySchedules = await _unitOfWork.ActivitySchedules
                    .GetQueryable()
                    .Where(asc => asc.isOptional && asc.activity.campId == campId)
                    .Include(asc => asc.activity)
                    .Include(asc => asc.CamperActivities)
                    .ToListAsync();

                // Step 4: Create folders for optional activities that have registered campers
                foreach (var activitySchedule in optionalActivitySchedules)
                {
                    // Check if this optional activity has any registered campers
                    var registeredCampersCount = activitySchedule.CamperActivities?.Count ?? 0;

                    if (registeredCampersCount > 0)
                    {
                        var camperActivityFolderPath = $"{campFolderPath}/camperactivity_{activitySchedule.activityScheduleId}";
                        if (!await CreateFolderInBucketAsync(camperActivityFolderPath))
                        {
                            _logger.LogError("Failed to create camper activity folder for ActivitySchedule {ScheduleId}",
                                activitySchedule.activityScheduleId);
                            // Continue with other folders even if one fails
                            continue;
                        }
                        _logger.LogInformation("Created camper activity folder: {FolderPath} (Registered: {Count})",
                            camperActivityFolderPath, registeredCampersCount);
                    }
                    else
                    {
                        _logger.LogInformation("Skipping ActivitySchedule {ScheduleId} - no registered campers",
                            activitySchedule.activityScheduleId);
                    }
                }

                _logger.LogInformation("Successfully completed folder creation for Camp {CampId}", campId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating attendance folders for Camp {CampId}", campId);
                return false;
            }
        }

        /// <summary>
        /// Checks if folders already exist for a camp to ensure idempotency
        /// </summary>
        public async Task<bool> FoldersExistForCampAsync(int campId)
        {
            try
            {
                var storage = _supabaseClient.Storage;
                var bucket = storage.From(BUCKET_NAME);

                // Check if the camp folder marker exists
                var campFolderMarkerPath = $"camp_{campId}/{MARKER_FILE_NAME}";

                try
                {
                    // Try to download the marker file
                    var fileBytes = await bucket.Download(campFolderMarkerPath, null);
                    return fileBytes != null && fileBytes.Length > 0;
                }
                catch
                {
                    // File doesn't exist
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if folders exist for Camp {CampId}", campId);
                return false;
            }
        }

        /// <summary>
        /// Creates a folder in the attendance-sessions bucket by uploading a marker file
        /// Supabase doesn't support empty folders, so we upload a small marker file
        /// </summary>
        public async Task<bool> CreateFolderInBucketAsync(string folderPath)
        {
            try
            {
                var storage = _supabaseClient.Storage;
                var bucket = storage.From(BUCKET_NAME);

                // Create a marker file to represent the folder
                var markerFilePath = $"{folderPath}/{MARKER_FILE_NAME}";
                var markerContent = Encoding.UTF8.GetBytes($"Folder created at {DateTime.UtcNow:O}");

                await bucket.Upload(
                    markerContent,
                    markerFilePath,
                    new Supabase.Storage.FileOptions
                    {
                        ContentType = "text/plain",
                        Upsert = true
                    });

                _logger.LogDebug("Created folder marker: {MarkerPath}", markerFilePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder in bucket: {FolderPath}", folderPath);
                return false;
            }
        }
    }
}
