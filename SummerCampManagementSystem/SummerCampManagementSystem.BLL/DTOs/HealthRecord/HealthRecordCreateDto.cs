using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.HealthRecord
{
    public class HealthRecordCreateDto
    {
        public string? Condition { get; set; }
        public string? Allergies { get; set; }
        public bool? IsAllergy { get; set; }
        public string? Note { get; set; }
    }

    public class HealthRecordResponseDto : HealthRecordCreateDto
    {
        public DateTime? CreateAt { get; set; }
    }
}
