using System.Collections.Generic;

namespace SummerCampManagementSystem.BLL.DTOs.CampJob
{
    public class CampJobListDto
    {
        public int CampId { get; set; }
        public string CampName { get; set; }
        public List<CampJobInfoDto> Jobs { get; set; } = new List<CampJobInfoDto>();
    }
}
