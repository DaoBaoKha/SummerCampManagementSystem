using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Vehicle
{
    public class VehicleRequestDto
    {
        public int? vehicleType { get; set; }

        [StringLength(255)]
        public string? vehicleName { get; set; }

        [StringLength(255)]
        [Unicode(false)]
        public string? vehicleNumber { get; set; }

        public int? capacity { get; set; }

        [StringLength(50)]
        public string? status { get; set; }
    }
}
