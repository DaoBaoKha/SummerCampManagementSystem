using SummerCampManagementSystem.BLL.DTOs.Activity;
using SummerCampManagementSystem.BLL.DTOs.AttendanceLog;
using SummerCampManagementSystem.BLL.DTOs.Livestream;
using SummerCampManagementSystem.BLL.DTOs.Location;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.ActivitySchedule
{
    public class ActivityScheduleResponseDto
    {
        public int ActivityScheduleId { get; set; }
        public int? CoreActivityId { get; set; } // ADD FOR FILTER LOGIC
        public ActivitySummaryDto Activity { get; set; }
        public SupervisorDto Staff { get; set; }    
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public bool? IsLivestream { get; set; }
        public LivestreamResponseDto LiveStream { get; set; }
        public int? MaxCapacity { get; set; }
        public bool IsOptional { get; set; }
        public LocationDto Location { get; set; }
       // public int? CurrentCapacity { get; set; }

    }

    public class ActivityScheduleByCamperResponseDto : ActivityScheduleResponseDto
    {
        public List<AttendanceLogResponseDto> AttendanceLogs { get; set; }

    }
}