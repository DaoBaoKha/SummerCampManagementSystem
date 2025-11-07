using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Accommodation
{
    public class AccommodationResponseDto
    {
        public int CampId { get; set; }
        public string? CampName { get; set; }
        public int AccommodationId { get; set; }
        public string? Name { get; set; }
        public int Capacity { get; set; }
    }
}
