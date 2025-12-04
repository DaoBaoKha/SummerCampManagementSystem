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
        Task<bool> IsCoreScheduleOfCamper(int activityScheduleId, int camperGroupId);
        Task<bool> IsOptionalScheduleOfCamper(int activityScheduleId, int camperId);
        Task<IEnumerable<AttendanceLog>> GetAttendanceLogsByScheduleId(int activityScheduleId);
        Task<IEnumerable<ActivitySchedule>> GetAttendedActivitiesByCamperId(int camperId);
        Task<IEnumerable<Camper>> GetAttendedCampersByActivityScheduleId(int activityScheduleId);

    }
}
