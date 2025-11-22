using System;

namespace SummerCampManagementSystem.BLL.DTOs.CampJob
{
    public class CampJobInfoDto
    {
        public string JobId { get; set; }
        public string JobName { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string Status { get; set; }
        public string TargetStatus { get; set; }
        public DateTime? LastExecutionTime { get; set; }
        public string LastExecutionResult { get; set; }
        public string FailureReason { get; set; }
    }
}
