using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICamperGroupRepository : IGenericRepository<CamperGroup>
    {
        Task<IEnumerable<CamperGroup>> SearchAsync(int? camperId, int? groupId, int? campId, string? camperName);
        Task<CamperGroup?> GetByIdWithDetailsAsync(int id);
        Task<CamperGroup?> GetByCamperAndGroupAsync(int camperId, int groupId);
        Task<CamperGroup?> GetByIdWithGroupAndCampAsync(int id);
        Task<IEnumerable<int>> GetCamperIdsByGroupIdAsync(int groupId);
        Task<IEnumerable<int>> GetGroupIdsByCamperIdAsync(int camperId);
        Task<bool> IsCamperInGroupAsync(int camperId, int groupId);
        Task<IEnumerable<Camper>> GetCampersByGroupIdAsync(int groupId);
    }
}
