namespace SummerCampManagementSystem.BLL.DTOs.StatisticalReport
{
    public class CamperRosterItemDto
    {
        public int RowNumber { get; set; }
        public string CamperName { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string GuardianName { get; set; }
        public string GuardianPhone { get; set; }
        public string GroupName { get; set; }
        public string MedicalNotes { get; set; }
        public string TransportInfo { get; set; }
        public string AccommodationInfo { get; set; }
    }
}
