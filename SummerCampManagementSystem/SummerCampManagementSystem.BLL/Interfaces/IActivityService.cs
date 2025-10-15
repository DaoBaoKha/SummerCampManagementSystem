using SummerCampManagementSystem.BLL.DTOs.Activity;
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
