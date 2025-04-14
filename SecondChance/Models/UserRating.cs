using System;
using System.ComponentModel.DataAnnotations;

namespace SecondChance.Models
{
    /// <summary>
    /// Representa uma avaliação feita por um utilizador a outro utilizador.
    /// Utilizado para o sistema de reputação na plataforma.
    /// </summary>
    public class UserRating
    {
        /// <summary>
        /// Identificador único da avaliação
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Valor da avaliação numa escala de 1 a 5
        /// </summary>
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        
        /// <summary>
        /// Data e hora em que a avaliação foi criada
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Identificador do utilizador que foi avaliado
        /// </summary>
        [Required]
        public string RatedUserId { get; set; }
        
        /// <summary>
        /// Utilizador que foi avaliado
        /// </summary>
        public User RatedUser { get; set; }

        /// <summary>
        /// Identificador do utilizador que realizou a avaliação
        /// </summary>
        [Required]
        public string RaterUserId { get; set; }
        
        /// <summary>
        /// Utilizador que realizou a avaliação
        /// </summary>
        public User RaterUser { get; set; }
    }
}