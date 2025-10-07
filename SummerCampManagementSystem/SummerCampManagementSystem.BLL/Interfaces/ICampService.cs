using SummerCampManagementSystem.BLL.DTOs.Requests.Camp;
using SummerCampManagementSystem.BLL.DTOs.Responses.Camp;
using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICampService
    {
        Task<IEnumerable<Camp>> GetAllCampsAsync();
        Task<Camp?> GetCampByIdAsync(int id);
        Task<IEnumerable<Camp>> GetCampsByTypeAsync(int campTypeId);
        Task<CampResponseDto> CreateCampAsync(CampRequestDto camp);
        Task<CampResponseDto> UpdateCampAsync(int campId, CampRequestDto camp);
        Task<bool> DeleteCampAsync(int id);
    }
}
