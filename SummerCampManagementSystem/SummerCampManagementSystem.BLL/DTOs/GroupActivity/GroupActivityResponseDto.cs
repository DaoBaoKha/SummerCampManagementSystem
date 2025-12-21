using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.GroupActivity
{
    public class GroupActivityResponseDto
    {
        public int groupActivityId { get; set; }

        public int? groupId { get; set; }

        public int? activityScheduleId { get; set; }

        [StringLength(50)]
        public string status { get; set; }
    }
}
