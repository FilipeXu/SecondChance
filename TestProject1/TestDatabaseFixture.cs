using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SecondChance.Models;
using Microsoft.EntityFrameworkCore;
using SecondChance.Data;
using Microsoft.Extensions.Logging;

namespace AuthenticationTests
{
    public class TestDatabaseFixture : IDisposable
    {
        private readonly IServiceScope _serviceScope;
        private readonly UserManager<User> _userManager;
        
        public TestDatabaseFixture()
        {
            var services = new ServiceCollection();
            

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });
            

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));


            services.AddIdentity<User, IdentityRole>(options => {
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            var serviceProvider = services.BuildServiceProvider();
            _serviceScope = serviceProvider.CreateScope();
            _userManager = _serviceScope.ServiceProvider.GetRequiredService<UserManager<User>>();

            EnsureTestAccountsExist().Wait();
        }
        
        private async Task EnsureTestAccountsExist()
        {

            var mainUser = await _userManager.FindByEmailAsync("test@example.com");
            if (mainUser == null)
            {
                var user = new User 
                { 
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
                    BirthDate = new DateTime(1990, 1, 1)
                };
                
                var result = await _userManager.CreateAsync(user, "Test@123456");
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors)}");
                }


                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to confirm test user email: {string.Join(", ", result.Errors)}");
                }
            }
            var secondaryUser = await _userManager.FindByEmailAsync("secondary@example.com");
            if (secondaryUser == null)
            {
                var user = new User 
                { 
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
                    BirthDate = new DateTime(1990, 1, 1)
                };
                
                var result = await _userManager.CreateAsync(user, "Test@123456");
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create secondary user: {string.Join(", ", result.Errors)}");
                }
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to confirm secondary user email: {string.Join(", ", result.Errors)}");
                }
            }
        }

        public UserManager<User> GetUserManager()
        {
            return _userManager;
        }
        
        public void Dispose()
        {
            _serviceScope.Dispose();
        }
    }
}