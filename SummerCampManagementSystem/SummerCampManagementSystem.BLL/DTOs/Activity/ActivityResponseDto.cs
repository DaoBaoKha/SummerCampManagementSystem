using SummerCampManagementSystem.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Activity
{
    public class ActivityResponseDto
    {
        public int ActivityId { get; set; }

        public ActivityType activityType { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public int CampId { get; set; }
    }

    public class ActivitySummaryDto
    {
        public string Name { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;

    }
}
