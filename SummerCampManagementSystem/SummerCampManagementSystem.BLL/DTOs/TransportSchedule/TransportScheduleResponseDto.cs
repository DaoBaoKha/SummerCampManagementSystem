using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.DTOs.Camper;
using SummerCampManagementSystem.BLL.DTOs.Driver;
using SummerCampManagementSystem.BLL.DTOs.Route;
using SummerCampManagementSystem.BLL.DTOs.Vehicle;
using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.BLL.DTOs.TransportSchedule
{
    public class TransportScheduleResponseDto
    {
        public int TransportScheduleId { get; set; }
        public CampSummaryDto? CampName {  get; set; }
        public RouteNameDto? RouteName { get; set; }
        public DriverNameDto? DriverFullName { get; set; }
        public VehicleNameDto? VehicleName { get; set; }

        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public TimeOnly? ActualStartTime { get; set; }
        public TimeOnly? ActualEndTime { get; set; }

        public TransportScheduleStatus Status { get; set; }
        public string? TransportType { get; set; }
        public string? CancelReasons { get; set; }
    }
}
