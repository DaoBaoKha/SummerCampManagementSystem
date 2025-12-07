using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.VehicleType
{
    public class VehicleTypeRequestDto
    {
        public string? name { get; set; }

        public string? description { get; set; }
    }

    public class VehicleTypeUpdateDto : VehicleTypeRequestDto
    {
        public bool? isActive { get; set; }
    }
}
