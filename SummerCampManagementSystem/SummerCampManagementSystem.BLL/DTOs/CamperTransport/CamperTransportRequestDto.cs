using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.CamperTransport
{
    public class CamperTransportRequestDto
    {
        [Required]
        public int TransportScheduleId { get; set; }

        [Required]
        public int CamperId { get; set; }

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
}
