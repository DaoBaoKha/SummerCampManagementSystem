using SummerCampManagementSystem.BLL.DTOs.Requests.CampType;
using SummerCampManagementSystem.BLL.DTOs.Responses.CampType;
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
