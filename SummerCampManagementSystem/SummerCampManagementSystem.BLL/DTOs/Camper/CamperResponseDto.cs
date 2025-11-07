using SummerCampManagementSystem.BLL.DTOs.Guardian;
using SummerCampManagementSystem.BLL.DTOs.HealthRecord;
using SummerCampManagementSystem.DAL.Models;
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

    public class CamperSummaryDto
    {
        public int CamperId { get; set; }
        public string? CamperName { get; set; }
        public string? Gender { get; set; }
        public DateOnly? Dob { get; set; }
        public int? GroupId { get; set; }
        public string? avatar { get; set; }
    }


    public class CamperWithGuardiansResponseDto
    {
        public int CamperId { get; set; }
        public string? CamperName { get; set; }
        public List<GuardianSummaryDto> Guardians { get; set; }
    }
}
