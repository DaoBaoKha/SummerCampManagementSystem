using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.Requests.User
{
    public class RegisterUserRequestDto
    {
        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public string PhoneNumber { get; set; } = null!;

        public string Password { get; set; } = null!;

        public DateOnly? Dob { get; set; }
    }
}
