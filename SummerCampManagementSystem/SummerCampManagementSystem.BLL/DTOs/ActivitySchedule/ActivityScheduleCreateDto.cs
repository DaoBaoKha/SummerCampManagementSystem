using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.ActivitySchedule
{
    public class ActivityScheduleCreateDto 
    {
        public int ActivityId { get; set; }
        public int? StaffId { get; set; }
        public int? LocationId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool? IsOptional { get; set; }    
        public bool? IsLiveStream { get; set; }

    }

    public class OptionalScheduleCreateDto
    {
        public int ActivityId { get; set; }
        public int? StaffId { get; set; }    
        public int? MaxCapacity { get; set; }
        public int? LocationId { get; set; }
        public bool? IsLiveStream { get; set; }

    }
}
