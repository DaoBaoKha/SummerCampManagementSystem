namespace SummerCampManagementSystem.BLL.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalCampers { get; set; }
        public int PendingApprovals { get; set; }
        public double CancellationRate { get; set; }
        public OccupancyDto Occupancy { get; set; }
    }

    public class OccupancyDto
    {
        public int Current { get; set; }
        public int Max { get; set; }
        public double Percentage { get; set; }
    }
}
