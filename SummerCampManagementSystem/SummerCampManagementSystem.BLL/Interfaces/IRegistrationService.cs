using SummerCampManagementSystem.BLL.DTOs.Requests.Registration;
using SummerCampManagementSystem.BLL.DTOs.Responses.Registration;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IRegistrationService
    {
        Task<IEnumerable<RegistrationResponseDto>> GetAllRegistrationsAsync();

        Task<RegistrationResponseDto?> GetRegistrationByIdAsync(int id);

        Task<CreateRegistrationResponseDto> CreateRegistrationAsync(CreateRegistrationRequestDto request);

        Task<UpdateRegistrationResponseDto?> UpdateRegistrationAsync(int id, UpdateRegistrationRequestDto request);

        Task<bool> DeleteRegistrationAsync(int id);

    }
}
