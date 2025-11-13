namespace SummerCampManagementSystem.BLL.DTOs.CamperGroup
{
    public class CamperGroupResponseDto
    {
        public int CamperGroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxSize { get; set; }
        public int SupervisorId { get; set; }
        public string SupervisorName { get; set; } = string.Empty;
        public int CampId { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
    }

    public class CamperGroupWithCampDetailsResponseDto 
    {
        public int CampId { get; set; }
        public string CampName { get; set; } = string.Empty;
        public int CamperGroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int MinAge { get; set; }
        public int MaxAge { get; set; }

    }
}
