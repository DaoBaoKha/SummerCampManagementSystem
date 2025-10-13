using SummerCampManagementSystem.BLL.DTOs.Requests.Activity;
using SummerCampManagementSystem.BLL.DTOs.Responses.Activity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IActivityService
    {
        Task<IEnumerable<ActivityResponseDto>> GetAllAsync();
        Task<ActivityResponseDto?> GetByIdAsync(int id);
        Task<IEnumerable<ActivityResponseDto>> GetByCampIdAsync(int campId);
        Task<ActivityResponseDto> CreateAsync(ActivityCreateDto dto);
        Task<bool> UpdateAsync(int id, ActivityCreateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
