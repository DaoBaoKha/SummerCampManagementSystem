using SummerCampManagementSystem.BLL.DTOs.Accommodation;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IAccommodationService
    {
        Task<AccommodationResponseDto?> GetBySupervisorIdAsync(int supervisorId, int campId);

        Task<AccommodationResponseDto> CreateAccommodationAsync(AccommodationRequestDto accommodationRequestDto);

        Task<AccommodationResponseDto> UpdateAccommodationAsync(int accommodationId, AccommodationRequestDto accommodationRequestDto);

        Task<bool> DeactivateAccommodationAsync(int accommodationId);  

        Task<IEnumerable<AccommodationResponseDto>> GetAccommodationsByCampIdAsync(int campId);

        Task<AccommodationResponseDto?> GetAccommodationByIdAsync(int accommodationId);

        Task<IEnumerable<AccommodationResponseDto>> GetAllAccommodationsAsync();
    }
}
