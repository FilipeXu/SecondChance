using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using SecondChance.Controllers;
using SecondChance.Hubs;
using SecondChance.Models;
using SecondChance.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace TestProject1.Integration
{    public class SupportControllerIntegrationTests : IntegrationTestBase
    {
        private readonly SupportController _controller;
        private readonly Mock<IHubContext<ChatHub>> _mockHubContext;

        public SupportControllerIntegrationTests()
        {
            _mockHubContext = CreateMockHubContext();
            _controller = new SupportController(_context, _userManager, _mockHubContext.Object);
        }
          private Mock<IHubContext<ChatHub>> CreateMockHubContext()
        {
            return new Mock<IHubContext<ChatHub>>();
        }

        private void SetupControllerUser(User user, bool isAdmin = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            };
            
            if (isAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task Chat_UserHasExistingChat_LoadsMessages()
        {
            var user = await CreateTestUser("user@test.com");
            var admin = await CreateTestUser("admin@test.com", isAdmin: true);
            SetupControllerUser(user);

            var supportChatId = $"support_{user.Id}";
            var messages = new[]
            {
                new ChatMessage
                {
                    SenderId = user.Id,
                    ReceiverId = admin.Id,
                    Content = "Help needed",
                    SentAt = DateTime.UtcNow.AddMinutes(-5),
                    ConversationId = supportChatId
                },
                new ChatMessage
                {
                    SenderId = admin.Id,
                    ReceiverId = user.Id,
                    Content = "How can I help?",
                    SentAt = DateTime.UtcNow,
                    ConversationId = supportChatId
                }
            };
            await _context.ChatMessages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            var result = await _controller.Chat() as ViewResult;

            Assert.NotNull(result);
            var chatMessages = Assert.IsAssignableFrom<List<ChatMessage>>(result.Model);
            Assert.Equal(2, chatMessages.Count);
            Assert.Equal(user.Id, result.ViewData["CurrentUserId"]);
            Assert.Equal(user.FullName, result.ViewData["CurrentUserName"]);
        }

        [Fact]
        public async Task AdminDashboard_Admin_ShowsAllSupportChats()
        {
            var admin = await CreateTestUser("admin@test.com", isAdmin: true);
            var user1 = await CreateTestUser("user1@test.com");
            var user2 = await CreateTestUser("user2@test.com");
            SetupControllerUser(admin, isAdmin: true);

            var chats = new[]
            {
                new ChatMessage
                {
                    SenderId = user1.Id,
                    ReceiverId = admin.Id,
                    Content = "Help request 1",
                    SentAt = DateTime.UtcNow,
                    ConversationId = $"support_{user1.Id}"
                },
                new ChatMessage
                {
                    SenderId = user2.Id,
                    ReceiverId = admin.Id,
                    Content = "Help request 2",
                    SentAt = DateTime.UtcNow,
                    ConversationId = $"support_{user2.Id}"
                }
            };
            await _context.ChatMessages.AddRangeAsync(chats);
            await _context.SaveChangesAsync();

            var result = await _controller.AdminDashboard() as ViewResult;

            Assert.NotNull(result);
            var viewModel = Assert.IsAssignableFrom<List<SupportChatViewModel>>(result.Model);
            Assert.Equal(2, viewModel.Count);
            Assert.Contains(viewModel, c => c.UserId == user1.Id);
            Assert.Contains(viewModel, c => c.UserId == user2.Id);
        }

        [Fact]
        public async Task RespondToChat_ValidUserId_LoadsChatHistory()
        {
            var admin = await CreateTestUser("admin@test.com", isAdmin: true);
            var user = await CreateTestUser("user@test.com");
            SetupControllerUser(admin, isAdmin: true);

            var supportChatId = $"support_{user.Id}";
            var messages = new[]
            {
                new ChatMessage
                {
                    SenderId = user.Id,
                    ReceiverId = admin.Id,
                    Content = "Need help",
                    SentAt = DateTime.UtcNow,
                    ConversationId = supportChatId
                }
            };
            await _context.ChatMessages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            var result = await _controller.RespondToChat(user.Id) as ViewResult;

            Assert.NotNull(result);
            var chatMessages = Assert.IsAssignableFrom<List<ChatMessage>>(result.Model);
            Assert.Single(chatMessages);
            Assert.Equal(admin.Id, result.ViewData["CurrentAdminId"]);
            Assert.Equal(user.Id, result.ViewData["UserId"]);
        }
    }
}