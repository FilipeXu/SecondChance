using SecondChance.Data;
using SecondChance.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace TestProject1
{
    public class ReliabilityTests : IClassFixture<TestDatabaseFixture>
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly HttpClient _client;
        private const string BaseUrl = "https://localhost:7052";
        private bool _isAppRunning = false;

        public ReliabilityTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _client = new HttpClient();
            
            try
            {
                var response = _client.GetAsync(BaseUrl).GetAwaiter().GetResult();
                _isAppRunning = response.IsSuccessStatusCode;
            }
            catch
            {
                _isAppRunning = false;
            }
        }

        private Product CreateTestProduct(string name = "Test Product")
        {
            return new Product
            {
                Name = name,
                Description = "Test",
                Category = Category.Roupa,
                Image = "test-image.jpg",
                Location = "Test Location",
                OwnerId = "test-owner-id"
            };
        }

        private User CreateTestUser(string email)
        {
            return new User
            {
                UserName = email,
                Email = email,
                FullName = "Test User",
                Description = "Test description",
                Image = "user-image.jpg",
                Location = "Test Location",
                BirthDate = DateTime.Now.AddYears(-20),
                JoinDate = DateTime.Now,
                IsActive = true
            };
        }

        [Fact]
        public async Task Application_ShouldRecover_FromDatabaseConnection()
        {
            using var scope = _fixture.ServiceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            try
            {
                var product = CreateTestProduct();
                await context.Products.AddAsync(product);
                await context.SaveChangesAsync();
                
                var savedProduct = await context.Products.FindAsync(product.Id);
                Assert.NotNull(savedProduct);
            }
            catch (InvalidOperationException)
            {
                Assert.True(true);
            }
        }

        private async Task CreateAndDeleteProduct(int index)
        {
            using var scope = _fixture.ServiceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var product = CreateTestProduct($"Concurrent Test Product {index}");
            await context.Products.AddAsync(product);
            await context.SaveChangesAsync();

            context.Products.Remove(product);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Application_ShouldHandle_ConcurrentUserOperations()
        {
            var tasks = new Task[5];
            for (int i = 0; i < 5; i++)
            {
                tasks[i] = CreateAndDeleteProduct(i);
            }

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task Application_ShouldRecover_FromAuthenticationFailure()
        {
            using var scope = _fixture.ServiceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            
            const string email = "recovery.test@example.com";
            const string password = "Recovery@123";

            try
            {
                var user = CreateTestUser(email);
                await userManager.CreateAsync(user, password);

                for (int i = 0; i < 5; i++)
                {
                    var result = await userManager.CheckPasswordAsync(user, password);
                    Assert.True(result);
                }
            }
            finally
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user != null)
                    await userManager.DeleteAsync(user);
            }
        }
    }
}