using SummerCampManagementSystem.BLL.DTOs.Activity;
using SummerCampManagementSystem.BLL.DTOs.Registration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.CamperActivity
{
    public class CamperActivityResponseDto
    {
        public int CamperActivityId { get; set; }
        public string? ParticipationStatus { get; set; }

        public CamperSummaryDto? Camper { get; set; }
        public ActivitySummaryDto? Activity { get; set; }


    }
}
