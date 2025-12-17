namespace SummerCampManagementSystem.BLL.DTOs.Dashboard
{
    public class DashboardOperationsDto
    {
        public List<CapacityAlertDto> CapacityAlerts { get; set; }
        public List<RecentRegistrationDto> RecentRegistrations { get; set; }
    }

    public class CapacityAlertDto
    {
        public string Type { get; set; } 
        public string Name { get; set; }
        public int Current { get; set; }
        public int Max { get; set; }
    }

    public class RecentRegistrationDto
    {
        public int RegistrationId { get; set; }
        public string CamperName { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public string Avatar { get; set; }
    }
}
