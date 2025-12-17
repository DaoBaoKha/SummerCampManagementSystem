namespace SummerCampManagementSystem.BLL.DTOs.Report
{
    public class ReportResponseDto
    {
        public int reportId { get; set; }

        public int? camperId { get; set; }

        public int? transportScheduleId { get; set; }

        public string reportType { get; set; }

        public string note { get; set; }

        public string image { get; set; }

        public DateTime? createAt { get; set; }

        public string status { get; set; }

        public int? reportedBy { get; set; }

        public int? activityScheduleId { get; set; }

        public string level { get; set; }
    }
}
