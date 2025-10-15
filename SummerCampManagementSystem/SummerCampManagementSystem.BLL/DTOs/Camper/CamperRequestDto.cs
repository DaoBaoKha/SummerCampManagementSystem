using SummerCampManagementSystem.BLL.DTOs.HealthRecord;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Camper
{
    public class CamperRequestDto
    {
        [Required]
        [StringLength(255)]
        public string CamperName { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Gender { get; set; } = null!;

        public DateOnly? Dob { get; set; }
        public int? GroupId { get; set; }

        public HealthRecordCreateDto? HealthRecords { get; set; }

    }
}
