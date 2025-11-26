using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    /// <summary>
    /// Service for managing attendance session folder structure in cloud storage
    /// </summary>
    public interface IAttendanceFolderService
    {
        /// <summary>
        /// Creates the folder hierarchy for a camp's attendance sessions
        /// Creates: attendance_sessions/camp_{campId}/camper_group_{groupId}/ and 
        /// attendance_sessions/camp_{campId}/camperactivity_{activityId}/ for each registered optional activity
        /// </summary>
        /// <param name="campId">The camp ID for which folders should be created</param>
        /// <returns>True if folders were created successfully, false otherwise</returns>
        Task<bool> CreateAttendanceFoldersForCampAsync(int campId);

        /// <summary>
        /// Checks if the folder structure already exists for a camp (idempotency check)
        /// </summary>
        /// <param name="campId">The camp ID to check</param>
        /// <returns>True if folders already exist, false otherwise</returns>
        Task<bool> FoldersExistForCampAsync(int campId);

        /// <summary>
        /// Creates a folder in the attendance_sessions bucket
        /// </summary>
        /// <param name="folderPath">The relative path of the folder to create</param>
        /// <returns>True if folder was created successfully, false otherwise</returns>
        Task<bool> CreateFolderInBucketAsync(string folderPath);
    }
}
