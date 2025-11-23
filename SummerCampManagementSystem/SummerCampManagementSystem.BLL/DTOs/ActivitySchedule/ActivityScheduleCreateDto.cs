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
        public int? locationId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool? isOptional { get; set; }      

    }

    public class OptionalScheduleCreateDto
    {
        public int ActivityId { get; set; }
        public int? StaffId { get; set; }    
        public int? maxCapacity { get; set; }
        public int? locationId { get; set; }
    }
}
