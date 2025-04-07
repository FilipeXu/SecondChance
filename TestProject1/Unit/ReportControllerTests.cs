using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SecondChance.Controllers;
using SecondChance.Models;
using SecondChance.ViewModels;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace TestProject1.Unit;

public class ReportControllerTests : BaseControllerTest
{
    private User CreateTestUser(string id, string username, string fullName) => new()
    {
        Id = id,
        UserName = username,
        FullName = fullName,
        EmailConfirmed = true,
        Location = "Test Location",
        Image = "/Images/default.jpg",
        Description = "Test Description",
        PhoneNumber = "1234567890",
        IsActive = true,
        BirthDate = DateTime.UtcNow.AddYears(-20),
        JoinDate = DateTime.UtcNow.AddDays(-30)
    };

    private Mock<UserManager<User>> SetupUserManager(User currentUser = null, User reportedUser = null)
    {
        var userManager = new Mock<UserManager<User>>(new Mock<IUserStore<User>>().Object, null, null, null, null, null, null, null, null);
        
        if (currentUser != null)
            userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
        
        if (reportedUser != null)
            userManager.Setup(x => x.FindByIdAsync(reportedUser.Id)).ReturnsAsync(reportedUser);
        
        return userManager;
    }

    private ReportController SetupController(UserManager<User> userManager, SecondChance.Data.ApplicationDbContext context, User currentUser = null)
    {
        var controller = new ReportController(userManager, context);
        
        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new DefaultHttpContext(), 
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

        if (currentUser != null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, currentUser.Id),
                new Claim(ClaimTypes.Name, currentUser.UserName)
            }, "TestAuthType"));
            
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }
        
        return controller;
    }

    [Fact]
    public async Task ReportUser_Get_WithValidId_ReturnsView()
    {
        using var context = CreateTestContext();
        var reportedUser = CreateTestUser("reported-user-id", "reported@test.com", "Reported User");
        var currentUser = CreateTestUser("current-user-id", "current@test.com", "Current User");
        
        var userManager = SetupUserManager(currentUser, reportedUser);
        var controller = SetupController(userManager.Object, context);

        var result = await controller.ReportUser(reportedUser.Id) as ViewResult;
        var model = result?.Model as ReportUserViewModel;

        Assert.NotNull(result);
        Assert.NotNull(model);
        Assert.Equal(reportedUser.Id, model.ReportedUserId);
    }

    [Fact]
    public async Task ReportUser_Post_WithValidModel_CreatesReport()
    {
        using var context = CreateTestContext();
        var reportedUser = CreateTestUser("reported-user-id", "reported@test.com", "Reported User");
        var currentUser = CreateTestUser("current-user-id", "current@test.com", "Current User");

        context.Users.AddRange(reportedUser, currentUser);
        await context.SaveChangesAsync();

        var userManager = SetupUserManager(currentUser, reportedUser);
        var controller = SetupController(userManager.Object, context, currentUser);

        var viewModel = new ReportUserViewModel
        {
            ReportedUserId = reportedUser.Id,
            ReportedUserName = reportedUser.FullName,
            Reason = "Test reason",
            Details = "Test details"
        };

        var result = await controller.ReportUser(viewModel) as RedirectToPageResult;

        Assert.NotNull(result);
        
        var report = await context.UserReports.FirstOrDefaultAsync();
        Assert.NotNull(report);
        Assert.Equal(reportedUser.Id, report.ReportedUserId);
        Assert.Equal(currentUser.Id, report.ReporterUserId);
        Assert.Equal("Test reason", report.Reason);
        
        Assert.Equal("/Account/Manage/Index", result.PageName);
        Assert.Equal(reportedUser.Id, result.RouteValues["userId"]);
    }

    [Fact]
    public async Task ReportUser_Post_WithInvalidModel_ReturnsViewWithModel()
    {
        using var context = CreateTestContext();
        var reportedUser = CreateTestUser("reported-user-id", "reported@test.com", "Reported User");
        
        var userManager = SetupUserManager(reportedUser: reportedUser);
        var controller = SetupController(userManager.Object, context);
        
        controller.ModelState.AddModelError("Reason", "Reason is required");

        var viewModel = new ReportUserViewModel
        {
            ReportedUserId = reportedUser.Id,
            ReportedUserName = reportedUser.FullName,
            Reason = "",
            Details = "Test details"
        };

        var result = await controller.ReportUser(viewModel) as ViewResult;
        var model = result?.Model as ReportUserViewModel;

        Assert.NotNull(result);
        Assert.NotNull(model);
        Assert.Equal(reportedUser.Id, model.ReportedUserId);
        Assert.False(controller.ModelState.IsValid);
    }
}