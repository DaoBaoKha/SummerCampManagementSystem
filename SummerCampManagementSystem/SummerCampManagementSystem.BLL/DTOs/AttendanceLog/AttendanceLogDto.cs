using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.AttendanceLog
{
    public class AttendanceLogDto
    {
        public int CamperId { get; set; }
        public string CamperName { get; set; } = "";    
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = "";
        public string CheckInMethod { get; set; } = "";
    }
}
