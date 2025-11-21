using SummerCampManagementSystem.BLL.DTOs.Accommodation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IAccommodationService
    {
        Task<AccommodationResponseDto?> GetBySupervisorIdAsync(int supervisorId, int campId);

        Task<AccommodationResponseDto> CreateAccommodationAsync(AccommodationRequestDto accommodationRequestDto);

        Task<AccommodationResponseDto> UpdateAccommodationAsync(int accommodationId, AccommodationRequestDto accommodationRequestDto);

        Task<bool> UpdateAccommodationStatusAsync(int accommodationId, bool isActive);

        Task<IEnumerable<AccommodationResponseDto>> GetAccommodationsByCampIdAsync(int campId);

        Task<AccommodationResponseDto?> GetAccommodationByIdAsync(int accommodationId);

        Task<IEnumerable<AccommodationResponseDto>> GetAllAccommodationsAsync();
        Task<IEnumerable<AccommodationResponseDto>> GetActiveAccommodationsAsync();
        Task<bool> DeleteAccommodationAsync(int accommodationId);
    }
}
