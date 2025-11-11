using SummerCampManagementSystem.BLL.DTOs.CampType;
using SummerCampManagementSystem.BLL.DTOs.Location;
using SummerCampManagementSystem.BLL.DTOs.Promotion;
using System;

namespace SummerCampManagementSystem.BLL.DTOs.Camp
{
    public class CampResponseDto
    {
        public int CampId { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int? MinParticipants { get; set; } = 0;
        public int? MaxParticipants { get; set; } = 0;
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Price { get; set; } = 0;
        public string Status { get; set; } = string.Empty;
        public string? Image { get; set; } = string.Empty; 

        public int? CreateBy { get; set; } = null;

        public DateTime? RegistrationStartDate { get; set; }
        public DateTime? RegistrationEndDate { get; set; }
        public CampTypeDto? CampType { get; set; }
        public LocationDto? Location { get; set; }
        public PromotionSummaryForCampDto? Promotion { get; set; }
    }

    public class CampSummaryDto
    {
        public int CampId { get; set; }
        public string Name { get; set; }
    }
}