using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Livestream
{
    public class LivestreamRequestDto
    {
        public string roomId { get; set; } = string.Empty;

        public string title { get; set; } = string.Empty;
    }
}
