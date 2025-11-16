using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SummerCampManagementSystem.BLL.DTOs.TransportSchedule
{
    public class TransportScheduleRequestDto
    {
        [Required(ErrorMessage = "Route ID is required.")]
        public int RouteId { get; set; }

        [Required(ErrorMessage = "Driver ID is required.")]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "Vehicle ID is required.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        public DateOnly Date { get; set; }

        [Required(ErrorMessage = "Start Time is required.")]
        [JsonConverter(typeof(TimeOnlyConverter))]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "End Time is required.")]
        [JsonConverter(typeof(TimeOnlyConverter))]
        public TimeOnly EndTime { get; set; }

        [JsonConverter(typeof(TimeOnlyConverter))]
        public TimeOnly? ActualStartTime { get; set; }

        [JsonConverter(typeof(TimeOnlyConverter))]
        public TimeOnly? ActualEndTime { get; set; }
    }
}
