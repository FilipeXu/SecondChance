using System;

namespace SecondChance.Models
{
    /// <summary>
    /// Representa uma pergunta frequente para o sistema de suporte.
    /// Contém uma pergunta e a sua respetiva resposta para ajudar os utilizadores.
    /// </summary>
    public class SupportFAQ
    {
        /// <summary>
        /// A pergunta frequente
        /// </summary>
        public string Question { get; set; }
        
        /// <summary>
        /// A resposta à pergunta frequente
        /// </summary>
        public string Answer { get; set; }
    }
}