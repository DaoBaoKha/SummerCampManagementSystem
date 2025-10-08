namespace SummerCampManagementSystem.BLL.DTOs.Requests.CamperGroup
{
    public class CamperGroupRequestDto
    {
        public string GroupName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxSize { get; set; }
        public int SupervisorId { get; set; }
        public int CampId { get; set; }
    }
}
