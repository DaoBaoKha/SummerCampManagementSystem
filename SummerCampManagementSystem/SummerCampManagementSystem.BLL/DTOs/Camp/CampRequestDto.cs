using SummerCampManagementSystem.Core.Enums;
using System; 

namespace SummerCampManagementSystem.BLL.DTOs.Camp
{
    public class CampRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int? MinParticipants { get; set; } = 0;
        public int? MaxParticipants { get; set; } = 0;
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }

        public DateOnly? StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public DateOnly? EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        public decimal? Price { get; set; } = 0;
        public string Status { get; set; } = string.Empty;
        public int? CampTypeId { get; set; }
        public string? Image { get; set; } = string.Empty;
        public int? LocationId { get; set; } = null;
        public int? PromotionId { get; set; } = null;

        public DateTime? RegistrationStartDate { get; set; } = DateTime.Now;
        public DateTime? RegistrationEndDate { get; set; } = DateTime.Now;

    }

    public class CampStatusUpdateRequestDto
    {
        public CampStatus Status { get; set; }
    }
}