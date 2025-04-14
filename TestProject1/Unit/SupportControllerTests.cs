using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using SecondChance.Controllers;
using SecondChance.Hubs;
using SecondChance.Models;
using System.Security.Claims;
using Xunit;

namespace TestProject1.Unit;

public class SupportControllerTests : BaseControllerTest
{    private Mock<IHubContext<ChatHub>> CreateMockHubContext()
    {
        var mockHubContext = new Mock<IHubContext<ChatHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        
        mockClients.Setup(clients => clients.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);
        
        return mockHubContext;
    }

    [Fact]
    public void Index_ReturnsViewWithFAQs()
    {        using var context = CreateTestContext();
        var hubContext = CreateMockHubContext();
        var controller = new SupportController(context, CreateTestUserManager(), hubContext.Object);
        
        var result = controller.Index() as ViewResult;
        var faqs = result?.Model as List<SupportFAQ>;

        Assert.NotNull(result);
        Assert.NotNull(faqs);
        Assert.NotEmpty(faqs);
    }

    [Fact]
    public async Task AdminDashboard_WhenUserIsNotAdmin_RedirectsToAccessDenied()
    {
        using var context = CreateTestContext();
        var user = new User { Id = "test-user-id", IsAdmin = false };
        var userManager = new Mock<UserManager<User>>(
            new Mock<IUserStore<User>>().Object,
            null, null, null, null, null, null, null, null);
          userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
            
        var hubContext = CreateMockHubContext();
        var result = await new SupportController(context, userManager.Object, hubContext.Object).AdminDashboard() as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("AccessDenied", result.ActionName);
        Assert.Equal("Account", result.ControllerName);
    }

    [Fact]
    public async Task SendMessage_WithValidData_CreatesMessage()
    {
        using var context = CreateTestContext();
        var user = new User { 
            Id = "test-user-id", 
            UserName = "test@test.com",
            FullName = "Test User",
            BirthDate = DateTime.UtcNow.AddYears(-20),
            JoinDate = DateTime.UtcNow.AddDays(-30),
            Location = "Test Location",
            Image = "/path/to/image.jpg",
            Description = "Test Description",
            PhoneNumber = "123456789",
            SentMessages = new List<ChatMessage>(),
            ReceivedMessages = new List<ChatMessage>()
        };

        var admin = new User {
            Id = "admin-id",
            UserName = "admin@test.com",
            FullName = "Admin User",
            IsAdmin = true,
            BirthDate = DateTime.UtcNow.AddYears(-30),
            JoinDate = DateTime.UtcNow.AddDays(-60),
            Location = "Admin Location",
            Image = "/path/to/admin.jpg",
            Description = "Admin Description",
            PhoneNumber = "987654321",
            SentMessages = new List<ChatMessage>(),
            ReceivedMessages = new List<ChatMessage>()
        };

        context.Users.AddRange(user, admin);
        await context.SaveChangesAsync();

        var userManager = new Mock<UserManager<User>>(
            new Mock<IUserStore<User>>().Object,
            null, null, null, null, null, null, null, null);
        
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);        userManager.Setup(x => x.GetUsersInRoleAsync("Admin"))
            .ReturnsAsync(new List<User> { admin });

        var hubContext = CreateMockHubContext();
        var controller = new SupportController(context, userManager.Object, hubContext.Object);
        var content = "Test support message";

        var result = await controller.SendMessage(content) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Chat", result.ActionName);
        
        var message = await context.ChatMessages.FirstOrDefaultAsync();
        Assert.NotNull(message);
        Assert.Equal(content, message.Content);
        Assert.Equal(user.Id, message.SenderId);
        Assert.Equal(admin.Id, message.ReceiverId);
        Assert.Equal($"support_{user.Id}", message.ConversationId);
    }
}