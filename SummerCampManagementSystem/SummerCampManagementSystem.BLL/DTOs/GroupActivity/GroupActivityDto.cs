using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.GroupActivity
{
    public class GroupActivityDto
    {
        public int? groupId { get; set; }

        public int? activityScheduleId { get; set; }
    }
}
