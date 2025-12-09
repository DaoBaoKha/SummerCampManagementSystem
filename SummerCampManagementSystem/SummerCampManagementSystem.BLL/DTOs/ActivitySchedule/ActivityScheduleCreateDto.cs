using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.ActivitySchedule
{
    public class ActivityScheduleCreateDto
    {
        public int ActivityId { get; set; }
        public int? StaffId { get; set; }
        public int? LocationId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool? IsOptional { get; set; }
        public bool? IsLiveStream { get; set; }

    }

    public class OptionalScheduleCreateDto
    {
        public int ActivityId { get; set; }
        public int? StaffId { get; set; }
        public int? MaxCapacity { get; set; }
        public int? LocationId { get; set; }
        public bool? IsLiveStream { get; set; }

    }

    public enum RepeatDayOfWeek
    {
        Sunday = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6
    }

    /// <summary>
    /// Request DTO để tạo lịch trình lặp lại theo tuần.
    /// </summary>
    public class ActivityScheduleTemplateDto
    {
        [Required(ErrorMessage = "Activity ID là bắt buộc.")]
        public int ActivityId { get; set; }

        public int? StaffId { get; set; }

        public int? LocationId { get; set; }

        public bool? IsLiveStream { get; set; }

        [Required(ErrorMessage = "Giờ bắt đầu là bắt buộc.")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "Giờ kết thúc là bắt buộc.")]
        public TimeOnly EndTime { get; set; }

        [Required(ErrorMessage = "Danh sách ngày lặp lại là bắt buộc.")]
        public List<RepeatDayOfWeek> RepeatDays { get; set; } = new List<RepeatDayOfWeek>();
    }
}
