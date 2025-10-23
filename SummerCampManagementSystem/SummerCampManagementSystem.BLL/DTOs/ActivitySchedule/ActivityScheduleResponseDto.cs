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
        public string ActivityName { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public bool? isLivestream { get; set; }
        public string RoomId { get; set; }
        public int? maxCapacity { get; set; }
        public bool IsOptional { get; set; }
        public int? locationId { get; set; }

    }
}
