using System.ComponentModel.DataAnnotations;

namespace SecondChance.Models
{
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime dateOfBirth)
            {
                var today = DateTime.Today;
                var age = today.Year - dateOfBirth.Year;
                
                // Subtract a year if the person hasn't had their birthday yet this year
                if (dateOfBirth.Date > today.AddYears(-age))
                {
                    age--;
                }

                if (age >= _minimumAge)
                {
                    return ValidationResult.Success;
                }

                return new ValidationResult($"Deves ter pelo menos {_minimumAge} anos para te registares.");
            }

            return new ValidationResult("Data de nascimento inválida.");
        }
    }
} 