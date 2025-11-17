using SummerCampManagementSystem.BLL.DTOs.RegistrationOptionalActivity;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IRegistrationOptionalActivityService
    {
        Task<RegistrationOptionalActivityResponseDto> GetByIdAsync(int id);

        Task<IEnumerable<RegistrationOptionalActivityResponseDto>> GetAllAsync();

        Task<IEnumerable<RegistrationOptionalActivityResponseDto>> SearchAsync(RegistrationOptionalActivitySearchDto searchDto);

    }
}
