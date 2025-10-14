using SummerCampManagementSystem.BLL.DTOs.Responses.Activity;
using SummerCampManagementSystem.BLL.DTOs.Responses.Registration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Responses.CamperActivity
{
    public class CamperActivityResponseDto
    {
        public int CamperActivityId { get; set; }
        public string? ParticipationStatus { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }

        public CamperSummaryDto? Camper { get; set; }
        public ActivitySummaryDto? Activity { get; set; }


    }
}
