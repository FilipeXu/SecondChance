using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SecondChance.Controllers;
using SecondChance.Data;
using SecondChance.Models;
using System.Security.Claims;


namespace TestProject1;

public class ProductsControllerTests 
{
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;

    public ProductsControllerTests()
    {
        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
    }

    private ApplicationDbContext CreateTestContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options);

    private User CreateTestUser(string id = "test-user-id") => new()
    {
        Id = id,
        UserName = "test@example.com",
        FullName = "Test User",
        BirthDate = DateTime.UtcNow.AddYears(-20),
        JoinDate = DateTime.UtcNow.AddDays(-30),
        Location = "Test Location",
        Image = "/path/to/image.jpg",
        Description = "Test Description",
        PhoneNumber = "123456789"
    };

    private Product CreateTestProduct(User owner) => new()
    { 
        Id = 1,
        Name = "Test Product", 
        Description = "Test Description",
        Category = Category.Roupa,
        Location = "Test Location",
        PublishDate = DateTime.UtcNow,
        Image = "/path/to/image.jpg",
        OwnerId = owner.Id,
        User = owner
    };

    private ProductsController SetupControllerWithUser(ApplicationDbContext context, User user)
    {
        var mockUserManager = new Mock<UserManager<User>>(
            new Mock<IUserStore<User>>().Object,
            null, null, null, null, null, null, null, null);

        mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var controller = new ProductsController(context, _mockWebHostEnvironment.Object, mockUserManager.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName)
        };
        
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { 
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType")) 
            }
        };

        return controller;
    }

    [Fact]
    public async Task Index_ReturnsViewWithProducts()
    {
        using var context = CreateTestContext();
        var user = CreateTestUser();
        var product = CreateTestProduct(user);
        
        context.Users.Add(user);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var controller = SetupControllerWithUser(context, user);
        var result = await controller.Index(null, null, null, null, null) as ViewResult;
        var model = result?.Model as IEnumerable<Product>;

        Assert.NotNull(result);
        Assert.NotNull(model);
        Assert.Single(model);
    }

   

    [Fact]
    public async Task Details_WithValidId_ReturnsViewWithProduct()
    {
        using var context = CreateTestContext();
        var user = CreateTestUser();
        var product = CreateTestProduct(user);
        
        context.Users.Add(user);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var controller = SetupControllerWithUser(context, user);
        var result = await controller.Details(1) as ViewResult;
        var model = result?.Model as Product;

        Assert.NotNull(result);
        Assert.NotNull(model);
        Assert.Equal("Test Product", model.Name);
    }

    [Fact]
    public async Task Details_WithInvalidId_ReturnsNotFound()
    {
        using var context = CreateTestContext();
        var controller = SetupControllerWithUser(context, CreateTestUser());

        var result = await controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ValidId_ReturnsViewWithProduct()
    {
        using var context = CreateTestContext();
        var user = CreateTestUser();
        var product = CreateTestProduct(user);
        
        context.Users.Add(user);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var controller = SetupControllerWithUser(context, user);
        var result = await controller.Edit(1) as ViewResult;
        var model = result?.Model as Product;

        Assert.NotNull(result);
        Assert.NotNull(model);
        Assert.Equal("Test Product", model.Name);
    }
}