using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.CamperActivity
{
    public class CamperActivityRequest
    {
        public string? ParticipationStatus { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
    }

    public class CamperActivityCreateDto : CamperActivityRequest
    {
        public int CamperId { get; set; }
        public int ActivityId { get; set; }
    }

    public class CamperActivityUpdateDto : CamperActivityRequest
    {
    }
}
