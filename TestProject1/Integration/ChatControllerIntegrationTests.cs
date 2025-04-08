using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondChance.Controllers;
using SecondChance.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace TestProject1.Integration
{
    public class ChatControllerIntegrationTests : IntegrationTestBase
    {
        private readonly ChatController _controller;

        public ChatControllerIntegrationTests() => 
            _controller = new ChatController(_context, _userManager);

        private void SetupControllerUser(User user)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            }, "TestAuth");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        [Fact]
        public async Task SendMessage_CreatesNewChatMessage()
        {
            var sender = await CreateTestUser("sender@test.com");
            var receiver = await CreateTestUser("receiver@test.com");
            SetupControllerUser(sender);
            
            var messageContent = "Test message";
            var result = await _controller.SendMessage(receiver.Id, messageContent) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Conversation", result.ActionName);
            
            var message = await _context.ChatMessages.FirstOrDefaultAsync(m => 
                m.SenderId == sender.Id && m.ReceiverId == receiver.Id && m.Content == messageContent);
                
            Assert.NotNull(message);
            Assert.Equal(sender.Id, message.SenderId);
            Assert.Equal(receiver.Id, message.ReceiverId);
            Assert.Equal(messageContent, message.Content);
        }

        [Fact]
        public async Task GetConversationMessages_ReturnsConversationHistory()
        {
            var user1 = await CreateTestUser("user1@test.com");
            var user2 = await CreateTestUser("user2@test.com");
            SetupControllerUser(user1);

            var conversationId = string.Join("_", new[] { user1.Id, user2.Id }.OrderBy(id => id));

            await _context.ChatMessages.AddRangeAsync(new[]
            {
                new ChatMessage
                {
                    SenderId = user1.Id,
                    ReceiverId = user2.Id,
                    Content = "Hello",
                    SentAt = DateTime.UtcNow.AddMinutes(-5),
                    ConversationId = conversationId
                },
                new ChatMessage
                {
                    SenderId = user2.Id,
                    ReceiverId = user1.Id,
                    Content = "Hi there",
                    SentAt = DateTime.UtcNow,
                    ConversationId = conversationId
                }
            });
            await _context.SaveChangesAsync();

            var result = await _controller.Conversation(user2.Id) as ViewResult;

            Assert.NotNull(result);
            var conversation = Assert.IsAssignableFrom<IEnumerable<ChatMessage>>(result.Model);
            Assert.Equal(2, conversation.Count());
            Assert.Contains(conversation, m => m.Content == "Hello");
            Assert.Contains(conversation, m => m.Content == "Hi there");
        }
    }
}