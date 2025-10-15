using SummerCampManagementSystem.BLL.DTOs.CamperActivity;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICamperActivityService
    {
        Task<IEnumerable<CamperActivityResponseDto>> GetAllAsync();
        Task<CamperActivityResponseDto?> GetByIdAsync(int id);
        Task<CamperActivityResponseDto> CreateAsync(CamperActivityCreateDto dto);
        Task<bool> UpdateAsync(int id, CamperActivityUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
