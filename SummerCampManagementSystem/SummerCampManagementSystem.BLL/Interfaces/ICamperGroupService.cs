using SummerCampManagementSystem.BLL.DTOs.CamperGroup;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICamperGroupService
    {
        Task<IEnumerable<CamperGroupResponseDto>> GetCamperGroupsAsync(CamperGroupSearchDto searchDto);

        Task<CamperGroupResponseDto> CreateCamperGroupAsync(CamperGroupRequestDto requestDto);

        Task<CamperGroupResponseDto> UpdateCamperGroupAsync(int id, CamperGroupRequestDto requestDto);

        Task<bool> DeleteCamperGroupAsync(int id);
    }
}
