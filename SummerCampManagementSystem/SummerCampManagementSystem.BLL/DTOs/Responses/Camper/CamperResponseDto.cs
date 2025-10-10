using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Responses.Camper
{
    public class CamperResponseDto
    {
        public int CamperId { get; set; }
        public string? CamperName { get; set; }
        public string? Gender { get; set; }
        public DateOnly? Dob { get; set; }
        public int? GroupId { get; set; }

        
    }
}
