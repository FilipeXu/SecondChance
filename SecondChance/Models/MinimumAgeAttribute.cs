using System.ComponentModel.DataAnnotations;

namespace SecondChance.Models
{
    /// <summary>
    /// Atributo de validação personalizado que verifica se a idade atinge um valor mínimo.
    /// Utilizado para garantir a idade mínima necessária para registo na plataforma.
    /// </summary>
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        /// <summary>
        /// Construtor do atributo MinimumAge.
        /// </summary>
        /// <param name="minimumAge">Idade mínima permitida em anos</param>
        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
        }

        /// <summary>
        /// Valida se a data de nascimento fornecida resulta numa idade acima do limite mínimo.
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