using SummerCampManagementSystem.BLL.DTOs.Camper;
using SummerCampManagementSystem.BLL.DTOs.Group;

namespace SummerCampManagementSystem.BLL.DTOs.CamperGroup
{
    public class CamperGroupResponseDto
    {
        public int camperGroupId { get; set; }
        public CamperNameDto camperName { get; set; }
        public GroupNameDto groupName { get; set; }  
        public string? status { get; set; }
    }
}