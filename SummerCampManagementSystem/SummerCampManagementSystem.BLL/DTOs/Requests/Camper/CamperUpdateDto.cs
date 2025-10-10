using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Requests.Camper
{
    public class CamperUpdateDto
    {
        [Required]
        public int CamperId { get; set; }

        [StringLength(255)]
        public string? CamperName { get; set; }

        [StringLength(50)]
        public string? Gender { get; set; }

        public DateOnly? Dob { get; set; }
        public int? GroupId { get; set; }
    }
}
