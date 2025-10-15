using SummerCampManagementSystem.BLL.DTOs.Requests.Registration;
using SummerCampManagementSystem.BLL.DTOs.Responses.Registration;
using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IRegistrationService
    {
        Task<IEnumerable<RegistrationResponseDto>> GetAllRegistrationsAsync();

        Task<RegistrationResponseDto?> GetRegistrationByIdAsync(int id);
        Task<IEnumerable<RegistrationResponseDto>> GetRegistrationByStatusAsync(RegistrationStatus? status = null);

        Task<RegistrationResponseDto> CreateRegistrationAsync(CreateRegistrationRequestDto request);
        Task<RegistrationResponseDto> ApproveRegistrationAsync(int registrationId);

        Task<RegistrationResponseDto?> UpdateRegistrationAsync(int id, UpdateRegistrationRequestDto request);

        Task<bool> DeleteRegistrationAsync(int id);

        Task<GeneratePaymentLinkResponseDto> GeneratePaymentLinkAsync(int registrationId);

    }
}
