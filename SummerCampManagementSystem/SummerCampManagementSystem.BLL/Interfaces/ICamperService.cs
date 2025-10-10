using SummerCampManagementSystem.BLL.DTOs.Requests.Camper;
using SummerCampManagementSystem.BLL.DTOs.Responses.Camper;
using SummerCampManagementSystem.BLL.DTOs.Responses.CamperGroup;
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
        Task<CamperResponseDto> CreateCamperAsync(CamperCreateDto camper);
        Task<bool> UpdateCamperAsync(CamperUpdateDto camper);
        Task<bool> DeleteCamperAsync(int id);
    }
}
