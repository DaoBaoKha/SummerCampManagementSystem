using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Requests.Route
{
    public class RouteRequestDto
    {
        public int campId { get; set; }

        public string routeName { get; set; } = string.Empty;
    }
}
