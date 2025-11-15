using SummerCampManagementSystem.BLL.DTOs.UserAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IStaffService
    {
        Task<IEnumerable<StaffSummaryDto>> GetAvailableActivityStaff(int campId, int activityScheduleId);
    }
}
