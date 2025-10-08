namespace SummerCampManagementSystem.BLL.DTOs.Responses.CamperGroup
{
    public class CamperGroupResponseDto
    {
        public int CamperGroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxSize { get; set; }
        public int SupervisorId { get; set; }
        public int CampId { get; set; }
    }
}
