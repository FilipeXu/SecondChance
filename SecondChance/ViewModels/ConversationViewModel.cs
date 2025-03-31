using System;

namespace SecondChance.ViewModels
{
    public class ConversationViewModel
    {
        public string? ConversationId { get; set; }
        public string? OtherUserId { get; set; }
        public string? OtherUserName { get; set; }
        public string? OtherUserImage { get; set; }
        public string? LastMessageContent { get; set; }
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }
}