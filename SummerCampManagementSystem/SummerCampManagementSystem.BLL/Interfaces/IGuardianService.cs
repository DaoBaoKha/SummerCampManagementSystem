using SummerCampManagementSystem.BLL.DTOs.Requests.Guardian;
using SummerCampManagementSystem.BLL.DTOs.Responses.Guardian;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IGuardianService
    {
        Task<IEnumerable<GuardianResponseDto>> GetAllAsync();
        Task<GuardianResponseDto?> GetByIdAsync(int id);
        Task<GuardianResponseDto> CreateAsync(GuardianCreateDto dto);
        Task<bool> UpdateAsync(int id, GuardianUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
