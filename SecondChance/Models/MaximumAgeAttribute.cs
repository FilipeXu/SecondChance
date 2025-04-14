using System.ComponentModel.DataAnnotations;

namespace SecondChance.Models
{
    /// <summary>
    /// Atributo de validação personalizado que verifica se a idade não excede um valor máximo.
    /// Utilizado para garantir limites de idade em registos de utilizadores.
    /// </summary>
    public class MaximumAgeAttribute : ValidationAttribute
    {
        private readonly int _maximumAge;

        /// <summary>
        /// Construtor do atributo MaximumAge.
        /// </summary>
        /// <param name="maximumAge">Idade máxima permitida em anos</param>
        public MaximumAgeAttribute(int maximumAge)
        {
            _maximumAge = maximumAge;
        }

        /// <summary>
        /// Valida se a data de nascimento fornecida resulta numa idade dentro do limite máximo.
        /// </summary>
        /// <param name="value">Data de nascimento a validar</param>
        /// <param name="validationContext">Contexto de validação</param>
        /// <returns>ValidationResult indicando sucesso ou falha da validação</returns>
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