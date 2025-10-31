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

        //public int? VehicleId { get; set; }
        //public int? TransportScheduleId { get; set; }

        public int StaffId { get; set; }
        public int ActivityScheduleId { get; set; }
        public string Note { get; set; } = "";
    }
}
