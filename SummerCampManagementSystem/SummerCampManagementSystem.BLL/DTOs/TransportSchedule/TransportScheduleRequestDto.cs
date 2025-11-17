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
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "End Time is required.")]
        public TimeOnly EndTime { get; set; }

        public TimeOnly? ActualStartTime { get; set; }

        public TimeOnly? ActualEndTime { get; set; }
    }

    public class TransportScheduleSearchDto
    {
        public int? VehicleId { get; set; }
        public int? DriverId { get; set; }
        public int? RouteId { get; set; }
        public DateOnly? Date { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        public string? Status { get; set; }

        // add paging here
    }
}
