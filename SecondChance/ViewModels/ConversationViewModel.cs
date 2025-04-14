using System;

namespace SecondChance.ViewModels
{
    /// <summary>
    /// Modelo de vista para apresentação de conversações entre utilizadores.
    /// Contém informações resumidas sobre uma conversa para listagens.
    /// </summary>
    public class ConversationViewModel
    {
        /// <summary>
        /// Identificador único da conversação
        /// </summary>
        public string? ConversationId { get; set; }
        
        /// <summary>
        /// Identificador do outro utilizador na conversação
        /// </summary>
        public string? OtherUserId { get; set; }
        
        /// <summary>
        /// Nome completo do outro utilizador na conversação
        /// </summary>
        public string? OtherUserName { get; set; }
        
        /// <summary>
        /// Caminho para a imagem de perfil do outro utilizador
        /// </summary>
        public string? OtherUserImage { get; set; }
        
        /// <summary>
        /// Conteúdo da última mensagem na conversação
        /// </summary>
        public string? LastMessageContent { get; set; }
        
        /// <summary>
        /// Data e hora da última mensagem na conversação
        /// </summary>
        public DateTime LastMessageTime { get; set; }
        
        /// <summary>
        /// Número de mensagens não lidas na conversação
        /// </summary>
        public int UnreadCount { get; set; }
    }
}