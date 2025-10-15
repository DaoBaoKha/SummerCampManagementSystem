using SummerCampManagementSystem.BLL.DTOs.Responses.CampType;
using SummerCampManagementSystem.BLL.DTOs.Responses.Location;
using SummerCampManagementSystem.BLL.DTOs.Responses.Promotion;

namespace SummerCampManagementSystem.BLL.DTOs.Responses.Camp
{
    public class CampResponseDto
    {
        public int CampId { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int MinParticipants { get; set; } = 0;
        public int MaxParticipants { get; set; } = 0;
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public decimal Price { get; set; } = 0;
        public string Status { get; set; } = string.Empty;
        public string image { get; set; } = string.Empty;
        public int? CreateBy { get; set; } = null;
        public DateTime RegistrationStartDate { get; set; } = DateTime.Now;
        public DateTime RegistrationEndDate { get; set; } = DateTime.Now;
        public CampTypeDto? CampType { get; set; } 
        public LocationDto? Location { get; set; } 
        public PromotionDto? Promotion { get; set; }
    }
}
