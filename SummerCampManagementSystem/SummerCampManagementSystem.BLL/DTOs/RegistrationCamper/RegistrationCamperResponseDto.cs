using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.DTOs.CamperGroup;

namespace SummerCampManagementSystem.BLL.DTOs.RegistrationCamper
{
    public class RegistrationCamperResponseDto
    {
        public int RegistrationId { get; set; }
        public int CamperId { get; set; }
        public CamperGroupDto CamperGroup { get; set; }
        public string Status { get; set; }
        public bool RequestTransport { get; set; }
        public CampSummaryDto? Camp { get; set; }
    }
}
