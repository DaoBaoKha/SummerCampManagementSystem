using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.UserAccount
{
    public class UserAccountRequestDto
    {
    }

    public class UserProfileUpdateDto
    {
        [StringLength(255)]
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(255)]
        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(255)]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(255)]
        public string Avatar { get; set; } = string.Empty;

        public DateOnly Dob { get; set; }
    }

    public class UserAdminUpdateDto
    {
        [Required(ErrorMessage = "Role is required.")]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
