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
        public ActivitySummaryDto Activity { get; set; }
        public SupervisorDto Staff { get; set; }    
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public bool? IsLivestream { get; set; }
        public LivestreamResponseDto LiveStream { get; set; }
        public LocationDto Location { get; set; }
        public int? CurrentCapacity { get; set; }

    }

    // DTO mới để hứng kết quả trả về
    public class CreateScheduleBatchResult
    {
        public List<ActivityScheduleResponseDto> Successes { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class ActivityScheduleByCamperResponseDto : ActivityScheduleResponseDto
    {
        public List<AttendanceLogNewResponseDto> AttendanceLogs { get; set; }

    }
}