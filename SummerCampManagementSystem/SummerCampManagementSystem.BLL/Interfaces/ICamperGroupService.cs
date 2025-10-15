using SummerCampManagementSystem.BLL.DTOs.CamperGroup;
using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICamperGroupService
    {
        Task<IEnumerable<CamperGroup>> GetAllCamperGroupsAsync();
        Task<CamperGroupResponseDto?> GetCamperGroupByIdAsync(int id);
        Task<CamperGroupResponseDto> CreateCamperGroupAsync(CamperGroupRequestDto camperGroup);
        Task<CamperGroupResponseDto?> UpdateCamperGroupAsync(int id, CamperGroupRequestDto camperGroup);
        Task<bool> DeleteCamperGroupAsync(int id);
    }
}
