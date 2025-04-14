using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondChance.Models
{
    /// <summary>
    /// Representa uma mensagem de conversa entre utilizadores.
    /// Armazena o conteúdo da mensagem e metadados relacionados.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Identificador único da mensagem
        /// </summary>
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Conteúdo textual da mensagem
        /// </summary>
        [Required]
        public string Content { get; set; }
        
        /// <summary>
        /// Data e hora do envio da mensagem
        /// </summary>
        [Required]
        public DateTime SentAt { get; set; }
        
        /// <summary>
        /// Indica se a mensagem foi lida pelo destinatário
        /// </summary>
        [Required]
        public bool IsRead { get; set; }

        /// <summary>
        /// Identificador do utilizador que enviou a mensagem
        /// </summary>
        [Required]
        public string SenderId { get; set; }
        
        /// <summary>
        /// Utilizador que enviou a mensagem
        /// </summary>
        [ForeignKey("SenderId")]
        public User Sender { get; set; }
        
        /// <summary>
        /// Identificador do utilizador que recebeu a mensagem
        /// </summary>
        [Required]
        public string ReceiverId { get; set; }
        
        /// <summary>
        /// Utilizador que recebeu a mensagem
        /// </summary>
        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; }

        /// <summary>
        /// Identificador único da conversação a que pertence esta mensagem
        /// </summary>
        public string ConversationId { get; set; }
    }
}