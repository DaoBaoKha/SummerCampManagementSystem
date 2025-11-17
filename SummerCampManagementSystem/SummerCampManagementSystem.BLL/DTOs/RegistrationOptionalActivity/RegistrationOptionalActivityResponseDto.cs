namespace SummerCampManagementSystem.BLL.DTOs.RegistrationOptionalActivity
{
    public class RegistrationOptionalActivityResponseDto
    {
        public int registrationOptionalActivityId { get; set; }

        public int registrationId { get; set; }

        public int camperId { get; set; }

        public int activityScheduleId { get; set; }

        public string status { get; set; }

        public DateTime? createdTime { get; set; }
    }

    public class RegistrationOptionalActivitySearchDto
    {
        public int? RegistrationId { get; set; }
        public int? CamperId { get; set; }
        public int? ActivityScheduleId { get; set; }
        public string? Status { get; set; }

        // add paging here
    }
}
