using System;

namespace SecondChance.ViewModels
{
    public class SupportChatViewModel
    {
        public string ConversationId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserImage { get; set; }
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }
}