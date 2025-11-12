namespace SummerCampManagementSystem.BLL.DTOs.Accommodation
{
    public class AccommodationResponseDto
    {
        public int accommodationId { get; set; }

        public int campId { get; set; }

        public int accommodationTypeId { get; set; }

        public string name { get; set; }

        public int? capacity { get; set; }

        public bool? isActive { get; set; }

        public int? supervisorId { get; set; }
    }
}
