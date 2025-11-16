using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.Core.Enums
{
    public enum ActivityScheduleStatus
    {
        Draft = 1,
        NotYet = 2,
        Rejected = 3,
        Canceled = 4,
        PendingAttendance = 5,
        AttendanceChecked = 6,
        Completed = 7
    }
}
