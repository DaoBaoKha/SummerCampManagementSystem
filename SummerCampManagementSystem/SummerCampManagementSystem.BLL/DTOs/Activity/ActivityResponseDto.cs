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

        public string? ActivityType { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public int CampId { get; set; }

        public int LocationId { get; set; }
    }

    public class ActivitySummaryDto
    {
        public int ActivityId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
