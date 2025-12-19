using System;
using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.Attributes
{
    /// <summary>
    /// Validates that the age calculated from a DateOnly field falls within a specified range.
    /// </summary>
    public class AgeRangeAttribute : ValidationAttribute
    {
        public int MinAge { get; }
        public int MaxAge { get; }

        public AgeRangeAttribute(int minAge, int maxAge)
        {
            MinAge = minAge;
            MaxAge = maxAge;
            ErrorMessage = $"Camper phải từ {minAge} đến {maxAge} tuổi.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is DateOnly dob)
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var age = CalculateAge(dob, today);

                if (age < MinAge || age > MaxAge)
                {
                    return new ValidationResult(ErrorMessage ?? $"Camper phải từ {MinAge} đến {MaxAge} tuổi.");
                }

                return ValidationResult.Success;
            }

            return new ValidationResult("Invalid date format.");
        }

        private int CalculateAge(DateOnly dob, DateOnly today)
        {
            var age = today.Year - dob.Year;
            
            // Subtract one year if birthday hasn't occurred yet this year
            if (today < dob.AddYears(age))
            {
                age--;
            }

            return age;
        }
    }
}
