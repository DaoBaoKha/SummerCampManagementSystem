using System;

namespace SummerCampManagementSystem.BLL.DTOs.CampJob
{
    public class JobExecutionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string JobName { get; set; }
        public DateTime ExecutionTime { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
    }
}
