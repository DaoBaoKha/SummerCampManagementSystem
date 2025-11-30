using SummerCampManagementSystem.BLL.DTOs.Camper;
using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.CamperGroup
{
    public class CamperGroupRequestDto
    {
        [Required]
        public int? camperId { get; set; }

        [Required]
        public int? groupId { get; set; }

    }

    public class CamperGroupSearchDto
    {
        public int? CamperId { get; set; }
        public int? GroupId { get; set; }
        public int? CampId { get; set; } 
        public string? CamperName { get; set; } 
    }
}
