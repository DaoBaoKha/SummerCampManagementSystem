using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.BLL.DTOs.RegistrationCamper
{
    public class RegistrationCamperRequestDto
    {
    }

    public class RegistrationCamperSearchDto
    {
        public int? CamperId { get; set; }
        public int? CampId { get; set; }
        public RegistrationCamperStatus? Status { get; set; }
        public bool? RequestTransport { get; set; }
    }
}
