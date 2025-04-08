using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SecondChance.Controllers;
using SecondChance.Models;
using System.Security.Claims;
using Xunit;

namespace TestProject1.Integration
{
    public class ProductWorkflowTests : IntegrationTestBase
    {
        private readonly Mock<IWebHostEnvironment> _webHostEnvironment = new();

        [Fact]
        public async Task ProductLifecycle_Success()
        {
           
            var seller = await CreateTestUser("seller@test.com");
            var controller = new ProductsController(_context, _webHostEnvironment.Object, _userManager);
            AuthenticateUser(controller, seller);

            var product = new Product
            {
                Name = "Test Product",
                Description = "Test Description",
                Category = Category.Roupa,
                Location = "Test Location",
                PublishDate = DateTime.UtcNow,
                Image = "/path/to/image.jpg",
                OwnerId = seller.Id,
                User = seller
            };
            
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            var listResult = await controller.Index(null, null, null, null, null) as ViewResult;
            var products = listResult?.Model as IEnumerable<Product>;
            Assert.Contains(products, p => p.Name == "Test Product");

            var detailResult = await controller.Details(product.Id) as ViewResult;
            var detailProduct = detailResult?.Model as Product;
            Assert.Equal("Test Product", detailProduct.Name);

            var dbProduct = await _context.Products.FindAsync(product.Id);
            dbProduct.IsDonated = true;
            dbProduct.DonatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var updatedProduct = await _context.Products.FindAsync(product.Id);
            Assert.True(updatedProduct.IsDonated);
            Assert.NotNull(updatedProduct.DonatedDate);
        }

        private void AuthenticateUser(Controller controller, User user)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            }, "TestAuth");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }
    }
}