using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondChance.Controllers;
using SecondChance.Models;
using SecondChance.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace TestProject1.Integration
{
    public class StatisticsControllerIntegrationTests : IntegrationTestBase
    {
        private readonly StatisticsController _controller;

        public StatisticsControllerIntegrationTests() => 
            _controller = new StatisticsController(_context);

        [Fact]
        public async Task Index_ReturnsViewWithStatistics()
        {
            var user1 = await CreateTestUser("user1@test.com");
            var user2 = await CreateTestUser("user2@test.com");

            var products = new[]
            {
                new Product
                {
                    Name = "Product 1",
                    Description = "Description 1",
                    Category = Category.Eletrônicos,
                    Location = "Location 1",
                    PublishDate = DateTime.UtcNow.AddDays(-5),
                    OwnerId = user1.Id,
                    User = user1,
                    IsDonated = true,
                    DonatedDate = DateTime.UtcNow,
                    Image = "/Images/placeholder.jpg"
                },
                new Product
                {
                    Name = "Product 2",
                    Description = "Description 2",
                    Category = Category.Roupa,
                    Location = "Location 2",
                    PublishDate = DateTime.UtcNow.AddDays(-3),
                    OwnerId = user2.Id,
                    User = user2,
                    Image = "/Images/placeholder.jpg"
                }
            };
            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();

            var result = await _controller.Index() as ViewResult;

            Assert.NotNull(result);
            var viewModel = Assert.IsType<StatisticsViewModel>(result.Model);
            Assert.Equal(2, viewModel.TotalProducts);
            Assert.Equal(1, viewModel.TotalDonatedProducts);
            Assert.Equal(2, viewModel.TotalUsers);
        }

        [Fact]
        public async Task Index_ShowsTopCategories()
        {
            var user = await CreateTestUser();
            var products = new[]
            {
                new Product
                {
                    Name = "Product 1",
                    Category = Category.Eletrônicos,
                    OwnerId = user.Id,
                    User = user,
                    Description = "Test Description",
                    Location = "Test Location",
                    Image = "/Images/placeholder.jpg"
                },
                new Product
                {
                    Name = "Product 2",
                    Category = Category.Eletrônicos,
                    OwnerId = user.Id, 
                    User = user,
                    Description = "Test Description",
                    Location = "Test Location",
                    Image = "/Images/placeholder.jpg"
                },
                new Product
                {
                    Name = "Product 3",
                    Category = Category.Roupa,
                    OwnerId = user.Id,
                    User = user,
                    Description = "Test Description",
                    Location = "Test Location",
                    Image = "/Images/placeholder.jpg"
                }
            };
            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();

            var result = await _controller.Index() as ViewResult;

            Assert.NotNull(result);
            var viewModel = Assert.IsType<StatisticsViewModel>(result.Model);
            var topCategory = viewModel.CategoryStats.FirstOrDefault();
            Assert.NotNull(topCategory);
            Assert.Equal(2, topCategory.Value);
            Assert.Equal("Eletrônicos", topCategory.Key);
            Assert.Equal(3, viewModel.TotalProducts);
        }
    }
}