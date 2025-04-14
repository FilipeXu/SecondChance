using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SecondChance.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string userId, string message, string receiverId)
        {
            await Clients.User(receiverId).SendAsync("ReceiveMessage", user, userId, message);
            await Clients.Caller.SendAsync("ReceiveMessage", user, userId, message);
        }

        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task SendMessageToGroup(string user, string userId, string message, string conversationId)
        {
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", user, userId, message);
        }

        public async Task JoinSupportChat(string supportChatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, supportChatId);
        }

        public async Task SendSupportMessage(string user, string userId, string message, string supportChatId)
        {
            await Clients.Group(supportChatId).SendAsync("ReceiveSupportMessage", user, userId, message);
        }
    }
}