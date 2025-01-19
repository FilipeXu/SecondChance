using System.ComponentModel.DataAnnotations;

namespace SecondChance.Models
{
    public sealed class PasswordStrengthAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is string password)
            {
                if (password.Length < 8)
                {
                    return new ValidationResult("The Password must be at least 8 characters long.");
                }
                if (!password.Any(char.IsUpper))
                {
                    return new ValidationResult("The Password must contain at least one uppercase letter.");
                }
                if (!password.Any(char.IsDigit))
                {
                    return new ValidationResult("The Password must contain at least one number.");
                }
                return ValidationResult.Success;
            }
            return new ValidationResult("Invalid Password.");
        }
    }
}