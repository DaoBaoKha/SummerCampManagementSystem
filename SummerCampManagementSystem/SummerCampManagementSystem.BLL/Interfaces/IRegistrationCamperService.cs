using SummerCampManagementSystem.BLL.DTOs.RegistrationCamper;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IRegistrationCamperService
    {
        Task<IEnumerable<RegistrationCamperResponseDto>> GetAllRegistrationCampersAsync();
        Task<IEnumerable<RegistrationCamperResponseDto>> SearchRegistrationCampersAsync(RegistrationCamperSearchDto searchDto);
    }
}
