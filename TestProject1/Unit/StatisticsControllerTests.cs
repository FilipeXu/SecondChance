using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondChance.Controllers;
using SecondChance.Models;
using SecondChance.ViewModels;
using Xunit;

namespace TestProject1.Unit;

public class StatisticsControllerTests : BaseControllerTest
{
    [Fact]
    public async Task Index_ReturnsViewWithStatistics()
    {

        using var context = CreateTestContext();
        var products = new[]
        {
            new Product 
            { 
                Name = "Product 1", 
                PublishDate = DateTime.UtcNow.AddDays(-10),
                IsDonated = true,
                DonatedDate = DateTime.UtcNow,
                Category = Category.Eletrônicos,
                Description = "Description 1",
                Location = "Location 1",
                Image = "/path/to/image1.jpg",
                OwnerId = "user1",
                User = new User
                {
                    Id = "user1",
                    UserName = "user1@test.com",
                    FullName = "User One",
                    BirthDate = DateTime.UtcNow.AddYears(-20),
                    JoinDate = DateTime.UtcNow.AddDays(-30),
                    Location = "Test Location 1",
                    Image = "/path/to/user1.jpg",
                    Description = "Description 1",
                    PhoneNumber = "123456789"
                }
            },
            new Product 
            { 
                Name = "Product 2", 
                PublishDate = DateTime.UtcNow.AddDays(-5),
                IsDonated = false,
                Category = Category.Móveis,
                Description = "Description 2",
                Location = "Location 2",
                Image = "/path/to/image2.jpg",
                OwnerId = "user2",
                User = new User 
                {
                    Id = "user2",
                    UserName = "user2@test.com",
                    FullName = "User Two",
                    BirthDate = DateTime.UtcNow.AddYears(-25),
                    JoinDate = DateTime.UtcNow.AddDays(-20),
                    Location = "Test Location 2",
                    Image = "/path/to/user2.jpg",
                    Description = "Description 2",
                    PhoneNumber = "987654321"
                }
            }
        };
        
        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        var controller = new StatisticsController(context);

        var result = await controller.Index() as ViewResult;
        var model = result?.Model as StatisticsViewModel;

        Assert.NotNull(result);
        Assert.NotNull(model);
        Assert.Equal(2, model.TotalProducts);
        Assert.Equal(2, model.TotalUsers);
        Assert.Equal(1, model.TotalDonatedProducts);        
        Assert.NotNull(model.WeeklyDonationStats);
        Assert.NotNull(model.MonthlyDonationStats);
        Assert.NotNull(model.CategoryStats);
    }

    [Fact]
public async Task Index_DonatedProductsCount_IsCorrect()
{
    using var context = CreateTestContext();
    
    var user = new User
    {
        Id = "test-user",
        UserName = "test@example.com",
        FullName = "Test User",
        BirthDate = DateTime.UtcNow.AddYears(-30),
        JoinDate = DateTime.UtcNow.AddDays(-60),
        Location = "Test Location",
        Image = "/path/to/test-user.jpg",
        Description = "Test user description",
        PhoneNumber = "123-456-7890"
    };
    
    var now = DateTime.UtcNow;
    var products = new List<Product>
    {
        new Product 
        { 
            Name = "Donated Product 1", 
            PublishDate = now.AddDays(-10),
            IsDonated = true, 
            DonatedDate = now.AddDays(-5),
            Category = Category.Eletrônicos,
            Description = "Description for donated product 1",
            Location = "Location 1",
            Image = "/path/to/donated1.jpg",
            OwnerId = user.Id,
            User = user
        },
        new Product 
        { 
            Name = "Donated Product 2", 
            PublishDate = now.AddDays(-8),
            IsDonated = true, 
            DonatedDate = now.AddDays(-3),
            Category = Category.Móveis,
            Description = "Description for donated product 2",
            Location = "Location 2",
            Image = "/path/to/donated2.jpg",
            OwnerId = user.Id,
            User = user
        },
        new Product 
        { 
            Name = "Not Donated Product 1", 
            PublishDate = now.AddDays(-7),
            IsDonated = false,
            Category = Category.Roupa,
            Description = "Description for not donated product 1",
            Location = "Location 3",
            Image = "/path/to/not-donated1.jpg",
            OwnerId = user.Id,
            User = user
        },
        new Product 
        { 
            Name = "Not Donated Product 2", 
            PublishDate = now.AddDays(-5),
            IsDonated = false,
            Category = Category.Livros,
            Description = "Description for not donated product 2",
            Location = "Location 4",
            Image = "/path/to/not-donated2.jpg",
            OwnerId = user.Id,
            User = user
        }
    };
    
    await context.Users.AddAsync(user);
    await context.Products.AddRangeAsync(products);
    await context.SaveChangesAsync();

    var controller = new StatisticsController(context);

    var result = await controller.Index() as ViewResult;
    var model = result?.Model as StatisticsViewModel;

    Assert.NotNull(model);
    Assert.Equal(4, model.TotalProducts);
    Assert.Equal(1, model.TotalUsers);
    Assert.Equal(2, model.TotalDonatedProducts);
}
[Fact]
public async Task Index_CategoryStats_ContainsAllCategories()
{
    using var context = CreateTestContext();
    
    var user = new User
    {
        Id = "category-test-user",
        UserName = "category@test.com",
        FullName = "Category Test User",
        BirthDate = DateTime.UtcNow.AddYears(-25),
        JoinDate = DateTime.UtcNow.AddDays(-45),
        Location = "Category Test Location",
        Image = "/path/to/category-user.jpg",
        Description = "Category test user description",
        PhoneNumber = "123-555-7890"
    };
    
    var now = DateTime.UtcNow;
    var products = new List<Product>
    {
        new Product 
        { 
            Name = "Electronics Product", 
            PublishDate = now.AddDays(-10),
            IsDonated = false,
            Category = Category.Eletrônicos,
            Description = "Description for electronics product",
            Location = "Electronics Location",
            Image = "/path/to/electronics.jpg",
            OwnerId = user.Id,
            User = user
        },
        new Product 
        { 
            Name = "Furniture Product", 
            PublishDate = now.AddDays(-9),
            IsDonated = false,
            Category = Category.Móveis,
            Description = "Description for furniture product",
            Location = "Furniture Location",
            Image = "/path/to/furniture.jpg",
            OwnerId = user.Id,
            User = user
        },
        new Product 
        { 
            Name = "Clothing Product", 
            PublishDate = now.AddDays(-8),
            IsDonated = true,
            DonatedDate = now.AddDays(-2),
            Category = Category.Roupa,
            Description = "Description for clothing product",
            Location = "Clothing Location",
            Image = "/path/to/clothing.jpg",
            OwnerId = user.Id,
            User = user
        },
        new Product 
        { 
            Name = "Books Product", 
            PublishDate = now.AddDays(-7),
            IsDonated = true,
            DonatedDate = now.AddDays(-1),
            Category = Category.Livros,
            Description = "Description for books product",
            Location = "Books Location",
            Image = "/path/to/books.jpg",
            OwnerId = user.Id,
            User = user
        }
    };
    
    await context.Users.AddAsync(user);
    await context.Products.AddRangeAsync(products);
    await context.SaveChangesAsync();

    var controller = new StatisticsController(context);

    var result = await controller.Index() as ViewResult;
    var model = result?.Model as StatisticsViewModel;

    Assert.NotNull(model);
    Assert.Equal(4, model.TotalProducts);
    Assert.NotNull(model.CategoryStats);
    
    var categoryStats = model.CategoryStats.ToDictionary(c => c.Key, c => c.Value);
    Assert.True(categoryStats.ContainsKey(Category.Eletrônicos.ToString()));
    Assert.True(categoryStats.ContainsKey(Category.Móveis.ToString()));
    Assert.True(categoryStats.ContainsKey(Category.Roupa.ToString()));
    Assert.True(categoryStats.ContainsKey(Category.Livros.ToString()));
    Assert.Equal(1, categoryStats[Category.Eletrônicos.ToString()]);
    Assert.Equal(1, categoryStats[Category.Móveis.ToString()]);
    Assert.Equal(1, categoryStats[Category.Roupa.ToString()]);
    Assert.Equal(1, categoryStats[Category.Livros.ToString()]);
}
}