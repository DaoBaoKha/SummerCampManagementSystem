namespace SummerCampManagementSystem.BLL.DTOs.StatisticalReport
{
    public class CampReportOverviewDto
    {
        // camp basic info
        public string CampName { get; set; }
        public string CampType { get; set; }
        public string Location { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }

        // capacity metrics
        public int TotalRegistrations { get; set; }
        public int ConfirmedCampers { get; set; }
        public int MaxParticipants { get; set; }
        public decimal OccupancyRate { get; set; } // percentage

        // financial metrics
        public decimal TotalRevenue { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal NetRevenue { get; set; }

        // performance metrics
        public decimal? AverageRating { get; set; }
        public int TotalFeedbacks { get; set; }
    }
}
