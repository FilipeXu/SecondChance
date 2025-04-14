using System.ComponentModel.DataAnnotations;

namespace SecondChance.Models
{
    /// <summary>
    /// Atributo de validação personalizado que verifica a força da palavra-passe.
    /// Garante que a palavra-passe atende aos requisitos mínimos de segurança.
    /// </summary>
    public sealed class PasswordStrengthAttribute : ValidationAttribute
    {
        /// <summary>
        /// Valida se a palavra-passe atende aos critérios de segurança:
        /// - Mínimo de 8 caracteres
        /// - Pelo menos uma letra maiúscula
        /// - Pelo menos um número
        /// </summary>
        /// <param name="value">Palavra-passe a validar</param>
        /// <param name="validationContext">Contexto de validação</param>
        /// <returns>ValidationResult indicando sucesso ou falha da validação</returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is string password)
            {
                if (password.Length < 8)
                {
                    return new ValidationResult("A palavra-passe deve ter pelo menos 8 caracteres.");
                }
                if (!password.Any(char.IsUpper))
                {
                    return new ValidationResult("A palavra-passe deve conter pelo menos uma letra maiúscula.");
                }
                if (!password.Any(char.IsDigit))
                {
                    return new ValidationResult("A palavra-passe deve conter pelo menos um número.");
                }
                return ValidationResult.Success;
            }
            return new ValidationResult("Palavra-passe inválida.");
        }
    }
}