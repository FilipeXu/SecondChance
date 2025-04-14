using System;
using System.ComponentModel.DataAnnotations;

namespace SecondChance.Models
{
    /// <summary>
    /// Representa um comentário feito por um utilizador no perfil de outro.
    /// Armazena o conteúdo do comentário e informações relacionadas.
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// Identificador único do comentário
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Conteúdo textual do comentário
        /// </summary>
        [Required(ErrorMessage = "O conteúdo do comentário é obrigatório")]
        public required string Content { get; set; }

        /// <summary>
        /// Data e hora da criação do comentário
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Identificador do utilizador que escreveu o comentário
        /// </summary>
        public required string AuthorId { get; set; }
        
        /// <summary>
        /// Utilizador que escreveu o comentário
        /// </summary>
        public required User Author { get; set; }

        /// <summary>
        /// Identificador do utilizador no perfil do qual o comentário foi escrito
        /// </summary>
        public required string ProfileId { get; set; }
        
        /// <summary>
        /// Utilizador cujo perfil recebeu o comentário
        /// </summary>
        public required User Profile { get; set; }
    }
}