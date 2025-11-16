using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.BLL.DTOs.TransportSchedule
{
    public class TransportScheduleResponseDto
    {
        public int TransportScheduleId { get; set; }
        public int RouteId { get; set; }
        public int DriverId { get; set; }
        public int VehicleId { get; set; }

        public string RouteName { get; set; } = string.Empty; 
        public string DriverFullName { get; set; } = string.Empty;
        public string VehicleLicensePlate { get; set; } = string.Empty; 

        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public TimeOnly? ActualStartTime { get; set; }
        public TimeOnly? ActualEndTime { get; set; }

        public TransportScheduleStatus Status { get; set; } 
        public string? CancelReasons { get; set; }
    }
}
