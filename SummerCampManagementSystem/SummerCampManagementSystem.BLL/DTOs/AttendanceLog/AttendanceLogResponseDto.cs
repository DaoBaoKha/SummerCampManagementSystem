using SummerCampManagementSystem.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.AttendanceLog
{
    public class AttendanceLogResponseDto
    {
        public int AttendanceLogId { get; set; }
        public int CamperId { get; set; }
        public string CamperName { get; set; } = "";    
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = "";
        public string CheckInMethod { get; set; } = "";
        public int staffId { get; set; }
        public int activityScheduleId { get; set; }
        public string Note { get; set; } = "";
        public ParticipationStatus participantStatus { get; set; }

    }
}
