using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.VehicleType
{
    public class VehicleTypeResponseDto
    {
        public int vehicleTypeId { get; set; }

        public string name { get; set; }

        public string description { get; set; }

        public bool? isActive { get; set; }
    }
}
