using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SecondChance.Controllers;
using SecondChance.Models;
using System.Security.Claims;
using Xunit;

namespace TestProject1.Unit;

public class CommentsControllerTests : BaseControllerTest
{
    private User CreateTestUser(string id, string userName = null) => new User
    {
        Id = id,
        UserName = userName ?? $"{id}@example.com",
        FullName = $"User {id}",
        BirthDate = DateTime.UtcNow.AddYears(-20),
        JoinDate = DateTime.UtcNow.AddDays(-30),
        Location = "Test Location",
        Image = "/path/to/image.jpg",
        Description = "Test Description",
        PhoneNumber = "123456789"
    };

    private Mock<UserManager<User>> CreateMockUserManager()
    {
        return new Mock<UserManager<User>>(
            new Mock<IUserStore<User>>().Object,
            null, null, null, null, null, null, null, null);
    }

    [Fact]
    public async Task ProfileComments_WithValidId_ReturnsViewWithComments()
    {
        using var context = CreateTestContext();
        var user = CreateTestUser("test-user-id");
        var author = CreateTestUser("author-id");
        
        context.Users.AddRange(user, author);
        await context.SaveChangesAsync();

        context.Comments.Add(new Comment
        {
            Id = 1,
            ProfileId = user.Id,
            Profile = user,
            AuthorId = author.Id,
            Author = author,
            Content = "Test Comment",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var userManager = CreateMockUserManager();
        userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        var controller = new CommentsController(context, userManager.Object);

        var result = await controller.ProfileComments(user.Id) as ViewResult;
        var model = result?.Model as IEnumerable<Comment>;

        Assert.NotNull(result);
        Assert.NotNull(model);
        Assert.Single(model);
        Assert.Equal("Test Comment", model.First().Content);
    }

    [Fact]
    public async Task ProfileComments_WithInvalidId_ReturnsNotFound()
    {
        using var context = CreateTestContext();
        var userManager = CreateMockUserManager();
        userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User)null);
        var controller = new CommentsController(context, userManager.Object);

        var result = await controller.ProfileComments("invalid-id");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_WithValidData_CreatesComment()
    {
        using var context = CreateTestContext();
        var currentUser = CreateTestUser("current-user-id", "current@test.com");
        var profileUser = CreateTestUser("profile-user-id", "profile@test.com");

        context.Users.AddRange(currentUser, profileUser);
        await context.SaveChangesAsync();

        var userManager = CreateMockUserManager();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
        userManager.Setup(x => x.FindByIdAsync(profileUser.Id)).ReturnsAsync(profileUser);

        var controller = new CommentsController(context, userManager.Object);

        var result = await controller.Create(profileUser.Id, "Test Comment") as RedirectToActionResult;

        Assert.NotNull(result);
        var comment = await context.Comments.FirstOrDefaultAsync();
        Assert.NotNull(comment);
        Assert.Equal("Test Comment", comment.Content);
        Assert.Equal(profileUser.Id, comment.ProfileId);
        Assert.Equal(currentUser.Id, comment.AuthorId);
    }

    [Fact]
    public async Task Delete_WithValidId_DeletesComment()
    {
        using var context = CreateTestContext();
        var currentUser = CreateTestUser("current-user-id");
        var profileUser = CreateTestUser("profile-user-id");
        
        context.Users.AddRange(currentUser, profileUser);
        await context.SaveChangesAsync();

        context.Comments.Add(new Comment
        { 
            Id = 1,
            Content = "Test Comment",
            AuthorId = currentUser.Id,
            Author = currentUser,
            ProfileId = profileUser.Id,
            Profile = profileUser,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var userManager = CreateMockUserManager();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);

        var controller = new CommentsController(context, userManager.Object);

        var result = await controller.DeleteConfirmed(1) as RedirectToActionResult;

        Assert.NotNull(result);
        var deletedComment = await context.Comments.FindAsync(1);
        Assert.Null(deletedComment);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        using var context = CreateTestContext();
        var currentUser = CreateTestUser("current-user-id");
        context.Users.Add(currentUser);
        await context.SaveChangesAsync();

        var userManager = CreateMockUserManager();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);

        var controller = new CommentsController(context, userManager.Object);

        var result = await controller.DeleteConfirmed(999);

        Assert.IsType<NotFoundResult>(result);
    }
}