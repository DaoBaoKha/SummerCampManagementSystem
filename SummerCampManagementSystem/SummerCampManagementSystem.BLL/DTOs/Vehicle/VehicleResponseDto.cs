using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Vehicle
{
    public class VehicleResponseDto
    {
        public int vehicleId { get; set; }

        public int? vehicleType { get; set; }

        public string vehicleName { get; set; }

        public string vehicleNumber { get; set; }

        public int? capacity { get; set; }

        [StringLength(50)]
        public string status { get; set; }
    }
}
