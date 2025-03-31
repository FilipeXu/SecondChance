using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SecondChance.Models;
using Microsoft.EntityFrameworkCore;
using SecondChance.Data;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;

namespace TestProject1
{
    public class TestDatabaseFixture : IDisposable
    {
        private readonly IServiceScope _serviceScope;
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private const string DefaultPassword = "Test@123456";

        public IServiceProvider ServiceProvider => _serviceScope.ServiceProvider;

        public TestDatabaseFixture()
        {
            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            var dbName = $"TestDatabase_{Guid.NewGuid()}";
            
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName)
                       .EnableSensitiveDataLogging()
                       .EnableDetailedErrors();
            });

            services.AddIdentity<User, IdentityRole>(options => {
                options.SignIn.RequireConfirmedAccount = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;

                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            var serviceProvider = services.BuildServiceProvider();
            _serviceScope = serviceProvider.CreateScope();
            _userManager = _serviceScope.ServiceProvider.GetRequiredService<UserManager<User>>();
            _dbContext = _serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            SetupTestDataAsync().GetAwaiter().GetResult();
        }

        private async Task SetupTestDataAsync()
        {
            try
            {
                var mainUser = new User 
                { 
                    Id = "test-user-id",
                    UserName = "test@example.com", 
                    Email = "test@example.com",
                    FullName = "Test User",
                    EmailConfirmed = true,
                    JoinDate = DateTime.Now.AddMonths(-1),
                    Location = "Test Location",
                    Image = "/Images/default.jpg",
                    Description = "Test Description",
                    PhoneNumber = "1234567890",
                    IsActive = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    BirthDate = new DateTime(1990, 1, 1),
                    NormalizedEmail = "TEST@EXAMPLE.COM",
                    NormalizedUserName = "TEST@EXAMPLE.COM"
                };

                await _dbContext.Users.AddAsync(mainUser);

                var passwordHasher = new PasswordHasher<User>();
                mainUser.PasswordHash = passwordHasher.HashPassword(mainUser, DefaultPassword);

                var secondaryUser = new User 
                { 
                    Id = "secondary-user-id",
                    UserName = "secondary@example.com", 
                    Email = "secondary@example.com",
                    FullName = "Secondary Test User",
                    EmailConfirmed = true,
                    JoinDate = DateTime.Now.AddMonths(-1),
                    Location = "Test Location",
                    Image = "/Images/default.jpg",
                    Description = "Test Description",
                    PhoneNumber = "1234567890",
                    IsActive = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    BirthDate = new DateTime(1990, 1, 1),
                    NormalizedEmail = "SECONDARY@EXAMPLE.COM",
                    NormalizedUserName = "SECONDARY@EXAMPLE.COM"
                };

                await _dbContext.Users.AddAsync(secondaryUser);
                secondaryUser.PasswordHash = passwordHasher.HashPassword(secondaryUser, DefaultPassword);

                var mainUserProducts = new List<Product>
                {
                    new Product
                    {
                        Id = 1,
                        Name = "Test Product 1",
                        Description = "First test product",
                        Category = Category.Eletrônicos,
                        PublishDate = DateTime.Now.AddDays(-10),
                        OwnerId = mainUser.Id,
                        Location = mainUser.Location,
                        Image = "/Images/products/test1.jpg",
                        User = mainUser
                    },
                    new Product
                    {
                        Id = 2,
                        Name = "Test Product 2",
                        Description = "Second test product",
                        Category = Category.Roupa,
                        PublishDate = DateTime.Now.AddDays(-5),
                        OwnerId = mainUser.Id,
                        Location = mainUser.Location,
                        Image = "/Images/products/test2.jpg",
                        User = mainUser
                    }
                };

                await _dbContext.Products.AddRangeAsync(mainUserProducts);

                var secondaryUserProducts = new List<Product>
                {
                    new Product
                    {
                        Id = 3,
                        Name = "Secondary Test Product",
                        Description = "Secondary user test product",
                        Category = Category.Eletrônicos,
                        PublishDate = DateTime.Now.AddDays(-15),
                        OwnerId = secondaryUser.Id,
                        Location = secondaryUser.Location,
                        Image = "/Images/products/test3.jpg",
                        User = secondaryUser
                    }
                };

                await _dbContext.Products.AddRangeAsync(secondaryUserProducts);
                await _dbContext.SaveChangesAsync();

                Console.WriteLine($"Test database initialized successfully with {_dbContext.Products.Count()} products");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during test data setup: {ex}");
                throw;
            }
        }

        public UserManager<User> GetUserManager() => _userManager;
        
        public ApplicationDbContext GetDbContext() => _dbContext;
        
        public void Dispose()
        {
            _serviceScope.Dispose();
        }
    }
}