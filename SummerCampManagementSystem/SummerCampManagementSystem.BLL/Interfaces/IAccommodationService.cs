using SummerCampManagementSystem.BLL.DTOs.Accommodation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IAccommodationService
    {
        Task<IEnumerable<AccommodationResponseDto>> GetAllBySupervisorIdAsync(int supervisorId);
    }
}
