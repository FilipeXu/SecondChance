using System;

namespace SecondChance.ViewModels
{
    /// <summary>
    /// Modelo de vista para gestão das conversas de suporte técnico.
    /// Contém informações resumidas sobre conversas entre utilizadores e equipa de suporte.
    /// </summary>
    public class SupportChatViewModel
    {
        /// <summary>
        /// Identificador único da conversação de suporte
        /// </summary>
        public string ConversationId { get; set; }
        
        /// <summary>
        /// Identificador do utilizador que iniciou a conversação de suporte
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Nome do utilizador que iniciou a conversação de suporte
        /// </summary>
        public string UserName { get; set; }
        
        /// <summary>
        /// Caminho para a imagem de perfil do utilizador
        /// </summary>
        public string UserImage { get; set; }
        
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