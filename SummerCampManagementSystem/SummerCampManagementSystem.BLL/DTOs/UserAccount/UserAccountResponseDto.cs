namespace SummerCampManagementSystem.BLL.DTOs.UserAccount
{
    public class UserAccountResponseDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? CreateAt { get; set; }
        public string Avatar { get; set; } = string.Empty;
        public DateOnly? Dob { get; set; }
    }

    public class StaffSummaryDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
    }

    public class SupervisorDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
    }   
}
