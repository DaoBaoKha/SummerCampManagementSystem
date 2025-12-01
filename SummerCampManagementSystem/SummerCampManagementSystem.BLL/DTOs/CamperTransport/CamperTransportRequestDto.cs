using SummerCampManagementSystem.BLL.DTOs.Camper;
using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.CamperTransport
{
    public class CamperTransportRequestDto
    {
        [Required]
        public int TransportScheduleId { get; set; }

        [Required]
        public CamperNameDto Camper { get; set; }

        [Required]
        public int StopLocationId { get; set; }

        public bool IsAbsent { get; set; }
 
        public DateTime CheckInTime { get; set; }

        public DateTime CheckOutTime { get; set; }

        public string Status {  get; set; }

        public string Note { get; set; }
    }

    public class CamperTransportUpdateDto
    {
        public bool? IsAbsent { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string? Status { get; set; }
        public string? Note { get; set; }
    }

    public class CamperTransportAttendanceDto
    {
        [Required]
        public List<int> CamperTransportIds { get; set; } = new List<int>();

        public string? Note { get; set; }
    }
}
