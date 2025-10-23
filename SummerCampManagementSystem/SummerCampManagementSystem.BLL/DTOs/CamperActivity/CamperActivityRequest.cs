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
       
    }

    public class CamperActivityCreateDto
    {
        public int CamperId { get; set; }
        public int ActivityId { get; set; }
    }

    public class CamperActivityUpdateDto : CamperActivityRequest
    {
    }
}
