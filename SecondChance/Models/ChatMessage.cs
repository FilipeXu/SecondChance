using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondChance.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        [Required]
        public DateTime SentAt { get; set; }
        
        [Required]
        public bool IsRead { get; set; }

        [Required]
        public string SenderId { get; set; }
        
        [ForeignKey("SenderId")]
        public User Sender { get; set; }
        
        [Required]
        public string ReceiverId { get; set; }
        
        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; }

        public string ConversationId { get; set; }
    }
} 