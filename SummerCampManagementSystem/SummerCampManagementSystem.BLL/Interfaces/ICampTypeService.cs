using SummerCampManagementSystem.BLL.DTOs.CampType;
using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICampTypeService
    {
       Task<IEnumerable<CampType>> GetAllCampTypesAsync();

       Task<CampType?> GetCampTypeByIdAsync(int id);

       Task<CampTypeResponseDto> AddCampTypeAsync(CampTypeRequestDto campType);

       Task<CampTypeResponseDto?> UpdateCampTypeAsync(int id, CampTypeRequestDto campType);

       Task<bool> DeleteCampTypeAsync(int id);
    }
}
