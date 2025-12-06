using SummerCampManagementSystem.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.AttendanceLog
{
    public class AttendanceLogRequestDto
    {
        public int CamperId { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = "";
        public int StaffId { get; set; }
        public int ActivityScheduleId { get; set; }
        public string Note { get; set; } = "";
        public ParticipationStatus participantStatus { get; set; } 
    }

    public class AttendanceLogListRequestDto
    {
        public int ActivityScheduleId { get; set; }
        public List<int> CamperIds { get; set; } = new();
        public ParticipationStatus participantStatus { get; set; }
        public string Note { get; set; } = "";
    }

    public class AttendanceLogUpdateRequest
    {
        public int AttendanceLogId { get; set; }
        public ParticipationStatus participantStatus { get; set; }
        public string Note { get; set; } = "";
    }

    public class AttendanceLogUpdateListRequest
    {
        public int ActivityScheduleId { get; set; }
        public List<AttendanceLogUpdateRequest> AttendanceLogs { get; set; } = new();
    }
}
