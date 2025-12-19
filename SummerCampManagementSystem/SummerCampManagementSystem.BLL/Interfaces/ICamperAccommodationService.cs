using SummerCampManagementSystem.BLL.DTOs.CamperAccommodation;
using SummerCampManagementSystem.BLL.DTOs.RegistrationCamper;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICamperAccommodationService
    {
        Task<IEnumerable<CamperAccommodationResponseDto>> GetCamperAccommodationsAsync(CamperAccommodationSearchDto searchDto);
        Task<IEnumerable<RegistrationCamperResponseDto>> GetPendingAssignCampersAsync(int? campId);
        Task<CamperAccommodationResponseDto> CreateCamperAccommodationAsync(CamperAccommodationRequestDto requestDto);
        Task<CamperAccommodationResponseDto> UpdateCamperAccommodationAsync(int id, CamperAccommodationRequestDto requestDto);
        Task<bool> DeleteCamperAccommodationAsync(int id);
    }
}
