﻿using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.User
{
    public class RegisterUserRequestDto
    {
        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = null!;

        public string Password { get; set; } = null!;

        public DateOnly? Dob { get; set; }
    }
}
