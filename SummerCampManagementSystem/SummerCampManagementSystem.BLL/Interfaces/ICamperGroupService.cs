using SummerCampManagementSystem.BLL.DTOs.CamperGroup;
using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICamperGroupService
    {
        Task<IEnumerable<CamperGroupResponseDto>> GetAllCamperGroupsAsync();
        Task<CamperGroupResponseDto?> GetCamperGroupByIdAsync(int id);
        Task<CamperGroupResponseDto> CreateCamperGroupAsync(CamperGroupRequestDto camperGroup);
        Task<CamperGroupResponseDto?> UpdateCamperGroupAsync(int id, CamperGroupRequestDto camperGroup);
        Task<bool> DeleteCamperGroupAsync(int id);
        Task<CamperGroupResponseDto> AssignStaffToGroup(int camperGroupId, int staffId);
        Task<CamperGroupWithCampDetailsResponseDto?> GetGroupBySupervisorIdAsync(int supervisorId, int campId);
        Task<IEnumerable<CamperGroupResponseDto>> GetGroupsByActivityScheduleId(int activityScheduleId);
    }
}
