using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Livestream
{
    public class LivestreamResponseDto
    {
        public int livestreamId { get; set; }

        public string roomId { get; set; } = string.Empty;

        public string title { get; set; } = string.Empty;

        public int? hostId { get; set; }
    }
}
