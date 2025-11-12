using SummerCampManagementSystem.BLL.DTOs.Camper;
using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICamperService
    {
        Task<IEnumerable<CamperResponseDto>> GetAllCampersAsync();
        Task<CamperResponseDto?> GetCamperByIdAsync(int id);
        Task<CamperResponseDto> CreateCamperAsync(CamperRequestDto dto, int parentId);
        Task<bool> UpdateCamperAsync(int id, CamperRequestDto camper);
        Task<bool> DeleteCamperAsync(int id);
        Task<IEnumerable<CamperResponseDto?>> GetCampersByCampId(int campId);
        Task<IEnumerable<CamperWithGuardiansResponseDto>> GetGuardiansByCamperId(int camperId);
        Task<IEnumerable<CamperResponseDto>> GetByParentIdAsync(int parentId);
        Task<IEnumerable<CamperSummaryDto>> GetCampersByOptionalActivitySchedule(int optionalActivityId);
        Task<IEnumerable<CamperSummaryDto>> GetCampersByCoreActivityIdAsync(int coreActivityId, int staffId);
    }
}
