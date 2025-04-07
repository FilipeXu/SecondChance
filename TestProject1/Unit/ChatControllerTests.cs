using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SecondChance.Controllers;
using SecondChance.Models;
using System.Security.Claims;
using Xunit;

namespace TestProject1.Unit;

public class ChatControllerTests : BaseControllerTest
{
    private Mock<UserManager<User>> CreateUserManagerMock(User returnUser = null)
    {
        var userManager = new Mock<UserManager<User>>(
            new Mock<IUserStore<User>>().Object,
            null, null, null, null, null, null, null, null);
        
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(returnUser);
        
        return userManager;
    }
    
    private User CreateTestUser(string id = "test-user-id", string userName = "test@example.com")
    {
        return new User { 
            Id = id, 
            UserName = userName,
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
    }

    [Fact]
    public async Task StartConversation_WithValidUserId_RedirectsToConversation()
    {
        using var context = CreateTestContext();
        var user = CreateTestUser();
        var otherUser = CreateTestUser("other-user-id", "other@example.com");
        otherUser.FullName = "Other User";

        context.Users.AddRange(user, otherUser);
        await context.SaveChangesAsync();
        
        var userManager = CreateUserManagerMock(user);
        userManager.Setup(x => x.FindByIdAsync(otherUser.Id))
            .ReturnsAsync(otherUser);
        
        var controller = new ChatController(context, userManager.Object);

        var result = await controller.StartConversation(otherUser.Id) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Conversation", result.ActionName);
        Assert.Equal(otherUser.Id, result.RouteValues["userId"]);
    }

    [Fact]
    public async Task StartConversation_WhenUserNotAuthenticated_ReturnsChallenge()
    {
        using var context = CreateTestContext();
        var userManager = CreateUserManagerMock();
        var controller = new ChatController(context, userManager.Object);

        var result = await controller.StartConversation("any-user-id");

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task SendMessage_WhenUserNotAuthenticated_ReturnsChallenge()
    {
        using var context = CreateTestContext();
        var userManager = CreateUserManagerMock();
        var controller = new ChatController(context, userManager.Object);

        var result = await controller.SendMessage("any-user-id", "Hello");

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task SendMessage_WithInvalidReceiverId_ReturnsBadRequest()
    {
        using var context = CreateTestContext();
        var user = CreateTestUser();
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        var userManager = CreateUserManagerMock(user);
        userManager.Setup(x => x.FindByIdAsync("invalid-id"))
            .ReturnsAsync((User)null);
        
        var controller = new ChatController(context, userManager.Object);

        var result = await controller.SendMessage("invalid-id", "Hello");

        Assert.IsType<NotFoundResult>(result);
    }
}