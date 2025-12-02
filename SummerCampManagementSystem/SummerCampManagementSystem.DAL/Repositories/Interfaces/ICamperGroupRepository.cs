using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICamperGroupRepository : IGenericRepository<CamperGroup>
    {
        /// <summary>
        /// Get all camper IDs for a specific group
        /// </summary>
        Task<IEnumerable<int>> GetCamperIdsByGroupIdAsync(int groupId);

        /// <summary>
        /// Get all group IDs that a specific camper belongs to
        /// </summary>
        Task<IEnumerable<int>> GetGroupIdsByCamperIdAsync(int camperId);

        /// <summary>
        /// Check if a camper is in a specific group
        /// </summary>
        Task<bool> IsCamperInGroupAsync(int camperId, int groupId);

        /// <summary>
        /// Get all campers with details for a specific group
        /// </summary>
        Task<IEnumerable<Camper>> GetCampersByGroupIdAsync(int groupId);
    }
}
