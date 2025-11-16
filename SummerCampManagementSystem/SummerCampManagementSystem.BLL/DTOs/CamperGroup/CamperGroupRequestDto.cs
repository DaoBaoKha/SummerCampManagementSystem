using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.CamperGroup
{
    public class CamperGroupRequestDto
    {
        [Required(ErrorMessage = "Camp ID is required.")]
        public int CampId { get; set; } 

        [Required(ErrorMessage = "Group Name is required.")]
        [StringLength(255, ErrorMessage = "Group Name cannot exceed 255 characters.")]
        public string GroupName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Supervisor ID must be a positive number.")]
        public int? SupervisorId { get; set; } 

        [Range(1, 1000, ErrorMessage = "Max Size must be between 1 and 1000.")] 
        public int? MaxSize { get; set; } 

        [Required, Range(1, 18)]
        public int MinAge { get; set; }

        [Required, Range(1, 18)] 
        public int MaxAge { get; set; }
    }
}
