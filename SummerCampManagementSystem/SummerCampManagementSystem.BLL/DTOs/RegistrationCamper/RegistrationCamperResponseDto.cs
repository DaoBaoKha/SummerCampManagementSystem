using SummerCampManagementSystem.BLL.DTOs.Accommodation;
using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.DTOs.Camper;
using SummerCampManagementSystem.BLL.DTOs.CamperGroup;
using SummerCampManagementSystem.BLL.DTOs.Group;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;

namespace SummerCampManagementSystem.BLL.DTOs.RegistrationCamper
{
    public class RegistrationCamperResponseDto
    {
        public int RegistrationId { get; set; }
        public UserAccountSummaryDto UserAccount { get; set; }
        public CamperNameDto Camper { get; set; }
        public GroupNameDto? GroupName { get; set; }
        public AccommodationSummaryDto? Accommodation { get; set; }
        public string Status { get; set; }
        public bool RequestTransport { get; set; }
        public CampSummaryDto? Camp { get; set; }
        
    }
}
