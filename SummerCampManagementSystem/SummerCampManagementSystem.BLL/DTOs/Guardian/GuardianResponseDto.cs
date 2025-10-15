using SummerCampManagementSystem.BLL.DTOs.Registration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Guardian
{
    public class GuardianResponseDto
    {
        public int GuardianId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateOnly? Dob { get; set; }
        public string? Answer { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }

        public List<CamperSummaryDto>? Campers { get; set; }
    }
}
