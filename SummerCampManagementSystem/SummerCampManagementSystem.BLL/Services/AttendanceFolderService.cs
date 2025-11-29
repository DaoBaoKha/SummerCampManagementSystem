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
                var camperGroups = await _unitOfWork.Groups
                    .GetByCampIdAsync(campId);

                var firstCamperGroup = camperGroups.FirstOrDefault();
                if (firstCamperGroup == null)
                {
                    _logger.LogWarning("No CamperGroup found for Camp {CampId}. Camp may not have core activities yet.", campId);
                }
                else
                {
                    // Create camper_group folder
                    var camperGroupFolderPath = $"{campFolderPath}/camper_group_{firstCamperGroup.groupId}";
                    if (!await CreateFolderInBucketAsync(camperGroupFolderPath))
                    {
                        _logger.LogError("Failed to create camper group folder for CamperGroup {GroupId}", firstCamperGroup.groupId);
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

                // Step 5: Create campers folder structure
                var campersFolderPath = $"{campFolderPath}/campers";
                if (!await CreateFolderInBucketAsync(campersFolderPath))
                {
                    _logger.LogError("Failed to create campers folder for Camp {CampId}", campId);
                    return false;
                }
                _logger.LogInformation("Created campers folder: {FolderPath}", campersFolderPath);

                // Step 6: Copy confirmed camper photos to attendance-sessions bucket
                await CopyConfirmedCamperPhotosAsync(campId);

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
        /// Copies photos of confirmed campers from camper-photos to attendance-sessions bucket
        /// Photos are organized into camper_group folders and camperactivity folders
        /// </summary>
        private async Task CopyConfirmedCamperPhotosAsync(int campId)
        {
            try
            {
                _logger.LogInformation("Starting to copy confirmed camper photos for Camp {CampId}", campId);

                // Get all camper groups for this camp
                var camperGroups = await _unitOfWork.Groups
                    .GetByCampIdAsync(campId);

                // Get all campers that belong to these groups (with avatars)
                var groupIds = camperGroups.Select(cg => cg.groupId).ToList();
                var campersInGroups = await _unitOfWork.Campers
                     .GetQueryable()
                     .Where(c => c.CamperGroups.Any(cg => groupIds.Contains(cg.groupId)))
                     .Where(c => !string.IsNullOrEmpty(c.avatar))
                     .Include(c => c.CamperGroups) 
                     .ToListAsync();

                _logger.LogInformation("Found {Count} campers with photos in Camp {CampId}", campersInGroups.Count, campId);

                // Get optional activities with registrations
                var optionalActivities = await _unitOfWork.ActivitySchedules
                    .GetQueryable()
                    .Where(asc => asc.isOptional && asc.activity.campId == campId)
                    .Include(asc => asc.CamperActivities)
                    .ToListAsync();

                int successCount = 0;
                int failCount = 0;

                foreach (var camper in campersInGroups)
                {
                    try
                    {
                        var camperGroupLink = camper.CamperGroups?.FirstOrDefault();

                        if (camperGroupLink != null) 
                        {
                            // Copy to camper_group folder
                            var targetFolder = $"camp_{campId}/camper_group_{camperGroupLink.groupId}";
                            await CopyCamperPhotoToAttendanceBucket(camper.camperId, camper.avatar, campId, targetFolder);
                            _logger.LogInformation("✅ Copied photo for Camper {CamperId} ({CamperName}) to {Folder}",
                                camper.camperId, camper.camperName, targetFolder);
                        }

                        // Copy to camperactivity folders for optional activities this camper is registered for
                        foreach (var optionalActivity in optionalActivities)
                        {
                            var isRegistered = optionalActivity.CamperActivities?
                                .Any(ca => ca.camperId == camper.camperId) ?? false;

                            if (isRegistered)
                            {
                                var activityTargetFolder = $"camp_{campId}/camperactivity_{optionalActivity.activityScheduleId}";
                                await CopyCamperPhotoToAttendanceBucket(camper.camperId, camper.avatar, campId, activityTargetFolder);
                                _logger.LogInformation("✅ Copied photo for Camper {CamperId} to camperactivity_{ActivityId}",
                                    camper.camperId, optionalActivity.activityScheduleId);
                            }
                        }

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _logger.LogWarning(ex, "❌ Failed to copy photo for Camper {CamperId}", camper.camperId);
                        // Continue with other campers even if one fails
                    }
                }

                _logger.LogInformation("Completed copying camper photos for Camp {CampId}. Success: {SuccessCount}, Failed: {FailCount}",
                    campId, successCount, failCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying confirmed camper photos for Camp {CampId}", campId);
                // Don't throw - this shouldn't fail the entire folder creation process
            }
        }

        /// <summary>
        /// Copies a single camper photo from camper-photos bucket to attendance-sessions bucket
        /// </summary>
        /// <param name="camperId">The camper ID</param>
        /// <param name="avatarUrl">The avatar URL from camper-photos bucket</param>
        /// <param name="campId">The camp ID</param>
        /// <param name="targetFolder">Optional target folder path (e.g., "camp_12/camper_group_8"). If not provided, uses "camp_{campId}/campers"</param>
        private async Task CopyCamperPhotoToAttendanceBucket(int camperId, string avatarUrl, int campId, string? targetFolder = null)
        {
            try
            {
                // Extract the file path from the avatar URL
                // avatarUrl format: https://...supabase.co/storage/v1/object/public/camper-photos/{camperId}/filename.jpg
                var uri = new Uri(avatarUrl);
                var pathParts = uri.AbsolutePath.Split('/');

                // Find the bucket name and get everything after it as the source path
                var bucketIndex = Array.IndexOf(pathParts, "camper-photos");
                if (bucketIndex == -1)
                {
                    throw new Exception($"Invalid avatar URL format: {avatarUrl}");
                }

                // Reconstruct source path: {camperId}/filename.jpg
                var sourcePath = string.Join("/", pathParts.Skip(bucketIndex + 1));
                var fileName = Path.GetFileName(sourcePath);
                var ext = Path.GetExtension(fileName).ToLower();

                // Determine content type
                var contentType = ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    _ => "image/jpeg"
                };

                _logger.LogDebug("Copying from camper-photos/{SourcePath} to attendance-sessions", sourcePath);

                // Download from camper-photos bucket using Supabase SDK
                var storage = _supabaseClient.Storage;
                var sourceBucket = storage.From("camper-photos");
                var imageBytes = await sourceBucket.Download(sourcePath, null);

                if (imageBytes == null || imageBytes.Length == 0)
                {
                    throw new Exception($"Failed to download photo from camper-photos bucket: {sourcePath}");
                }

                // Determine target path: use provided folder or default to campers folder
                var targetBucket = storage.From(BUCKET_NAME);
                var basePath = string.IsNullOrEmpty(targetFolder) ? $"camp_{campId}/campers" : targetFolder;

                // Rename file to include camper ID for identification: avatar_<camperId>_<original>.jpg
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                var extension = Path.GetExtension(fileName);
                var newFileName = $"avatar_{camperId}_{fileNameWithoutExt}{extension}";
                var targetPath = $"{basePath}/{newFileName}";

                // Upload the actual photo
                _logger.LogDebug("Uploading photo to: {TargetPath}", targetPath);
                await targetBucket.Upload(
                    imageBytes,
                    targetPath,
                    new Supabase.Storage.FileOptions
                    {
                        ContentType = contentType,
                        Upsert = true
                    });

                _logger.LogDebug("Successfully copied photo to {TargetPath}", targetPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying photo for Camper {CamperId} from URL {AvatarUrl}", camperId, avatarUrl);
                throw;
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
                _logger.LogInformation("🪣 CreateFolder: bucket='{BucketName}', path='{FolderPath}'", BUCKET_NAME, folderPath);
                var storage = _supabaseClient.Storage;

                var bucket = storage.From(BUCKET_NAME);
                _logger.LogInformation("✅ Bucket reference created for: {BucketName}", BUCKET_NAME);

                // Create a marker file to represent the folder
                var markerFilePath = $"{folderPath}/{MARKER_FILE_NAME}";
                var markerContent = Encoding.UTF8.GetBytes($"Folder created at {DateTime.UtcNow:O}");

                _logger.LogInformation("📤 Uploading marker to: {MarkerPath}", markerFilePath);

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
