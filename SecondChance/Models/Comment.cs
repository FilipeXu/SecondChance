using System;
using System.ComponentModel.DataAnnotations;

namespace SecondChance.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O conteúdo do comentário é obrigatório")]
        public required string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public required string AuthorId { get; set; }
        public required User Author { get; set; }

        public required string ProfileId { get; set; }
        public required User Profile { get; set; }
    }
}