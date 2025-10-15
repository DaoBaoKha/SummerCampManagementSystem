using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Requests.Guardian
{
    public class GuardianUpdateDto : GuardianCreateDto
    {
        public bool IsActive { get; set; }
    }
}
