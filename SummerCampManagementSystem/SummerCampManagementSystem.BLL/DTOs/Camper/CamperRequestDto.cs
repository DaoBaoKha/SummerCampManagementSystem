using Microsoft.AspNetCore.Http;
using SummerCampManagementSystem.BLL.DTOs.HealthRecord;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Camper
{
    public class CamperCreateDto
    {
        [Required]
        [StringLength(255)]
        public string CamperName { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string Gender { get; set; } = null!;

        [Required]
        public DateOnly Dob { get; set; }

        public HealthRecordCreateDto? HealthRecord { get; set; }

    }

    public class CamperUpdateDto
    {
    
        public string? CamperName { get; set; }

        
        public string? Gender { get; set; }

        public DateOnly? Dob { get; set; }

        public HealthRecordCreateDto? HealthRecord { get; set; }

    }

    public class CampExtensionRequestDto
    {
        [Required(ErrorMessage = "Ngày đóng đăng ký mới là bắt buộc.")]
        public DateTime NewRegistrationEndDate { get; set; }
    }
}
