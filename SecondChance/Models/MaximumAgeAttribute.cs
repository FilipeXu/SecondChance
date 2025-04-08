using System.ComponentModel.DataAnnotations;

namespace SecondChance.Models
{
    public class MaximumAgeAttribute : ValidationAttribute
    {
        private readonly int _maximumAge;

        public MaximumAgeAttribute(int maximumAge)
        {
            _maximumAge = maximumAge;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime dateOfBirth)
            {
                var today = DateTime.Today;
                var age = today.Year - dateOfBirth.Year;
                if (dateOfBirth.Date > today.AddYears(-age))
                {
                    age--;
                }

                if (age <= _maximumAge)
                {
                    return ValidationResult.Success;
                }

                return new ValidationResult($"A idade não pode ser maior que {_maximumAge} anos");
            }

            return new ValidationResult("Data de nascimento inválida");
        }
    }
}