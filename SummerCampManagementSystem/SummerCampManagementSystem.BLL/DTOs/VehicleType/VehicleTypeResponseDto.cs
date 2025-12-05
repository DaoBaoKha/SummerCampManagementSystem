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
        public int VehicleTypeId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool? IsActive { get; set; }
    }

    public class VehicleTypeSimpleDto
    {
        public int VehicleTypeId { get; set; }
        public string Name { get; set; }
    }
}
