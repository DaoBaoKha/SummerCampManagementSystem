using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Report
{
    public class ReportResponseDto
    {
        public int reportId { get; set; }

        public int? camperId { get; set; }

        public string note { get; set; }

        public string image { get; set; }

        public DateTime? createAt { get; set; }

        public string status { get; set; }

        public int? reportedBy { get; set; }

        public int? activityId { get; set; }

        public string level { get; set; }
    }
}
