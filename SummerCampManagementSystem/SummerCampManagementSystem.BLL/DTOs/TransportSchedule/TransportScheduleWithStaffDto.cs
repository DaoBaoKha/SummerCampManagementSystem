using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.DTOs.Driver;
using SummerCampManagementSystem.BLL.DTOs.Route;
using SummerCampManagementSystem.BLL.DTOs.Vehicle;
using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.BLL.DTOs.TransportSchedule
{
    public class TransportScheduleWithStaffDto
    {
        public int TransportScheduleId { get; set; }
        public CampSummaryDto? CampName { get; set; }
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

        public List<StaffInTransportDto> Staff { get; set; } = new List<StaffInTransportDto>();

        public int StaffCount { get; set; }
    }

    // DTO for staff member info in transport
    public class StaffInTransportDto
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
