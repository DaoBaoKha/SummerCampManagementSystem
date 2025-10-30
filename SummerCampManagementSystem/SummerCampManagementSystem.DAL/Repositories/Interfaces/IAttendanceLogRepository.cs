using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IAttendanceLogRepository : IGenericRepository<AttendanceLog>
    {
        Task<bool> IsScheduleOfCamper(int activityScheduleId, int camperGroupId);
    }
}
