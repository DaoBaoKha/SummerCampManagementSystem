using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Activity
{
    public class ActivityCreateDto
    {
        public int CampId { get; set; }

        public int LocationId { get; set; } 

        public string? ActivityType { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }
    }
}
