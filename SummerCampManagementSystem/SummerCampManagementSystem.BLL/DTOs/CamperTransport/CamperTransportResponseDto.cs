using SummerCampManagementSystem.BLL.DTOs.Camper;
using SummerCampManagementSystem.BLL.DTOs.Location;

namespace SummerCampManagementSystem.BLL.DTOs.CamperTransport
{
    public class CamperTransportResponseDto
    {
        public int CamperTransportId { get; set; }

        public int TransportScheduleId { get; set; }

        public CamperNameDto? Camper { get; set; } 

        public LocationDto? Location { get; set; }

        public bool IsAbsent { get; set; }

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public string? Status { get; set; }

        public string? Note { get; set; }
    }

    public class CamperInScheduleResponseDto
    {
        public int CamperTransportId { get; set; }

        public int TransportScheduleId { get; set; }

        public CamperNameDto? Camper { get; set; }

        public LocationDto? Location { get; set; }

        public bool IsAbsent { get; set; }

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public string? Status { get; set; }

        public string? Note { get; set; }
    }
}
