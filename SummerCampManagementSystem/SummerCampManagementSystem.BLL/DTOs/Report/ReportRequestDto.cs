using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Report
{
    public class ReportRequestDto
    {
        public int camperId { get; set; }

        public string note { get; set; } = string.Empty;

        public IFormFile? image { get; set; }

        public string status { get; set; } = string.Empty;

        public int activityId { get; set; }

        public string level { get; set; } = string.Empty;
    }
}
