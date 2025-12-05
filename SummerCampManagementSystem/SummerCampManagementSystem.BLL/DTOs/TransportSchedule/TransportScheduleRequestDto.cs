using SummerCampManagementSystem.Core.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SummerCampManagementSystem.BLL.DTOs.TransportSchedule
{
    public class TransportScheduleRequestDto
    {
        [Required(ErrorMessage = "Camp ID is required.")]
        public int CampId { get; set; }

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

        [Required]
        //[RegularExpression("PickUp|DropOff", ErrorMessage = "TransportType must be 'PickUp' or 'DropOff'")]
        public string TransportType { get; set; }
    }

    public class TransportScheduleSearchDto
    {
        public int? CampId { get; set; }
        public int? VehicleId { get; set; }
        public int? DriverId { get; set; }
        public int? RouteId { get; set; }
        public DateOnly? Date { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        public string? Status { get; set; }

        // add paging here
    }

    public class TransportScheduleStatusUpdateDto
    {
        public TransportScheduleStatus Status { get; set; }
        public string? CancelReasons { get; set; }
    }
}
