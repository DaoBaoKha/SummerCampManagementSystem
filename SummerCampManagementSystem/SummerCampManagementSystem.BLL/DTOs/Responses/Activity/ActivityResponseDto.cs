using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Responses.Activity
{
    public class ActivityResponseDto
    {
        public int ActivityId { get; set; }

        public int CampId { get; set; }

        public int? StaffId { get; set; }

        public string? ActivityType { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string? Location { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Status { get; set; } = null!;

        public bool IsLivestream { get; set; }

        public int? RoomId { get; set; }
    }
}
