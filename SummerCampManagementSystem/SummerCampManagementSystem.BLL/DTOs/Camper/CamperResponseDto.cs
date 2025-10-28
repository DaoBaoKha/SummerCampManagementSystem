using SummerCampManagementSystem.BLL.DTOs.HealthRecord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Camper
{
    public class CamperResponseDto
    {
        public int CamperId { get; set; }
        public string? CamperName { get; set; }
        public string? Gender { get; set; }
        public DateOnly? Dob { get; set; }
        public int Age { get; set; }
        public int? GroupId { get; set; }
        public string? avatar { get; set; }



        public HealthRecordResponseDto? HealthRecord { get; set; }


    }
}
