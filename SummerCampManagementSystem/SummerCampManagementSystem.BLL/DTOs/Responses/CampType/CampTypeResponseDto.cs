namespace SummerCampManagementSystem.BLL.DTOs.Responses.CampType
{
    public class CampTypeResponseDto
    {
        public int CampTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
