using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SecondChance.Controllers;
using SecondChance.Models;
using SecondChance.ViewModels;
using System.Security.Claims;
using Xunit;

namespace TestProject1.Unit;

public class ModeratorControllerTests : BaseControllerTest
{
    private User CreateTestUser(string id, bool isAdmin = false, bool isFirstUser = false, bool isActive = true)
    {
        return new User { 
            Id = id, 
            IsAdmin = isAdmin,
            IsFirstUser = isFirstUser,
            IsActive = isActive,
            FullName = $"User {id}",
            BirthDate = DateTime.UtcNow.AddYears(-25),
            JoinDate = DateTime.UtcNow.AddDays(-30),
            Image = "/default.jpg",
            Location = "Test Location",
            Description = "Description"
        };
    }
    
    private Mock<UserManager<User>> SetupUserManager(User currentUser)
    {
        var userManager = new Mock<UserManager<User>>(
            new Mock<IUserStore<User>>().Object,
            null, null, null, null, null, null, null, null);
        
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(currentUser);
            
        return userManager;
    }
    
    private void SetupController(Controller controller)
    {
        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
    }

    [Fact]
    public async Task Index_WhenUserIsNotAdmin_RedirectsToAccessDenied()
    {
        using var context = CreateTestContext();
        var user = CreateTestUser("test-user", isAdmin: false);
        var userManager = SetupUserManager(user);
        var controller = new ModeratorController(userManager.Object, context);

        var result = await controller.Index() as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("AccessDenied", result.ActionName);
        Assert.Equal("Account", result.ControllerName);
    }

    [Fact]
    public async Task Index_WhenUserIsAdmin_ReturnsViewWithStats()
    {
        using var context = CreateTestContext();
        var user = CreateTestUser("admin-user", isAdmin: true);
        var userManager = SetupUserManager(user);
        var controller = new ModeratorController(userManager.Object, context);

        var result = await controller.Index() as ViewResult;
        var model = result?.Model as ModeratorIndexViewModel;

        Assert.NotNull(result);
        Assert.NotNull(model);
    }

    [Fact]
    public async Task Users_ReturnsViewWithUsersList()
    {
        using var context = CreateTestContext();
        var user = CreateTestUser("admin-user", isAdmin: true);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userManager = SetupUserManager(user);
        var controller = new ModeratorController(userManager.Object, context);

        var result = await controller.Users() as ViewResult;
        var model = result?.Model as IEnumerable<User>;

        Assert.NotNull(result);
        Assert.NotNull(model);
        Assert.Single(model);
    }
    
    [Fact]
    public async Task Reports_WhenUserIsAdmin_ReturnsViewWithReportsList()
    {
        using var context = CreateTestContext();
        var adminUser = CreateTestUser("admin-id", isAdmin: true);
        var reporter = CreateTestUser("reporter-id");
        var reported = CreateTestUser("reported-id");
        
        var report = new UserReport {
            Id = 1,
            ReporterUserId = reporter.Id,
            ReporterUser = reporter,
            ReportedUserId = reported.Id,
            ReportedUser = reported,
            Reason = "Inappropriate behavior",
            Details = "User posted offensive content",
            ReportDate = DateTime.UtcNow.AddDays(-1)
        };
        
        context.Users.AddRange(adminUser, reporter, reported);
        context.UserReports.Add(report);
        await context.SaveChangesAsync();

        var userManager = SetupUserManager(adminUser);
        var controller = new ModeratorController(userManager.Object, context);
        
        var result = await controller.Reports() as ViewResult;
        var model = result?.Model as List<UserReport>;

        Assert.NotNull(result);
        Assert.NotNull(model);
        Assert.Single(model);
        Assert.Equal("Inappropriate behavior", model[0].Reason);
    }
    
    [Fact]
    public async Task ReportDetails_WhenReportExists_ReturnsViewWithReport()
    {
        using var context = CreateTestContext();
        var adminUser = CreateTestUser("admin-id", isAdmin: true);
        var reporter = CreateTestUser("reporter-id");
        var reported = CreateTestUser("reported-id");
        
        var report = new UserReport {
            Id = 1,
            ReporterUserId = reporter.Id,
            ReporterUser = reporter,
            ReportedUserId = reported.Id,
            ReportedUser = reported,
            Reason = "Inappropriate behavior",
            Details = "User was using offensive language",
            ReportDate = DateTime.UtcNow.AddDays(-1)
        };
        
        context.Users.AddRange(adminUser, reporter, reported);
        context.UserReports.Add(report);
        await context.SaveChangesAsync();

        var userManager = SetupUserManager(adminUser);
        var controller = new ModeratorController(userManager.Object, context);
        
        var result = await controller.ReportDetails(1) as ViewResult;
        var model = result?.Model as UserReport;

        Assert.NotNull(result);
        Assert.NotNull(model);
        Assert.Equal(1, model.Id);
        Assert.Equal("Inappropriate behavior", model.Reason);
    }
}