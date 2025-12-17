namespace SummerCampManagementSystem.BLL.DTOs.Dashboard
{
    public class AdminPriorityActionsDto
    {
        public List<PendingCampDto> PendingCamps { get; set; }
        public List<RecentUserDto> RecentUsers { get; set; }
    }

    public class PendingCampDto
    {
        public int CampId { get; set; }
        public string Name { get; set; }
        public string ManagerName { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string Status { get; set; }
    }

    public class RecentUserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime RegisteredDate { get; set; }
    }
}
