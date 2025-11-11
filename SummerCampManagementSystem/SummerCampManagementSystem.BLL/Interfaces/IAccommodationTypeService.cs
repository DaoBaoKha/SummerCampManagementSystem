using SummerCampManagementSystem.BLL.DTOs.AccommodationType;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IAccommodationTypeService
    {
        Task<IEnumerable<AccommodationTypeResponseDto>> GetAllAsync();

        Task<AccommodationTypeResponseDto?> GetByIdAsync(int id);

        Task<AccommodationTypeResponseDto> CreateAsync(AccommodationTypeRequestDto accommodationTypeRequestDto);

        Task<AccommodationTypeResponseDto?> UpdateAsync(int id, AccommodationTypeRequestDto accommodationTypeRequestDto);

        Task<bool> DeleteAsync(int id);
    }
}
