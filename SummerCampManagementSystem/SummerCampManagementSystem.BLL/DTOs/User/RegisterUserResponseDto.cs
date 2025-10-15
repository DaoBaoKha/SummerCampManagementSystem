namespace SummerCampManagementSystem.BLL.DTOs.User
{
    public class RegisterUserResponseDto
    {
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class UserDetailDto
    {
        public int UserId { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PhoneNumber { get; set; } = null!;

        public string? Avatar { get; set; }

        public DateOnly? Dob { get; set; }

        public bool? IsActive { get; set; }
    }
}
