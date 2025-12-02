namespace SummerCampManagementSystem.BLL.DTOs.Group
{
    public class GroupResponseDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? MaxSize { get; set; }      
        public int? SupervisorId { get; set; } 
        public string SupervisorName { get; set; } = string.Empty;
        public int? CampId { get; set; }       
        public int? MinAge { get; set; }       
        public int? MaxAge { get; set; }      
    }

    public class GroupWithCampDetailsResponseDto 
    {
        public int CampId { get; set; }
        public string CampName { get; set; } = string.Empty;
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int MinAge { get; set; }
        public int MaxAge { get; set; }

    }

    public class GroupNameDto
    {
        public int GroupId { get; set; }

        public string GroupName { get; set; }
    }
}
