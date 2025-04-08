using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SecondChance.Data;
using SecondChance.Models;
using System;
using System.Threading.Tasks;

namespace TestProject1.Integration
{
    public abstract class IntegrationTestBase : IDisposable
    {
        protected readonly ApplicationDbContext _context;
        protected readonly UserManager<User> _userManager;
        protected readonly Mock<UserManager<User>> _userManagerMock;

        protected IntegrationTestBase()
        {
            var services = new ServiceCollection();
            
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            services.AddIdentity<User, IdentityRole>(options => {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            services.AddLogging(builder => builder.AddDebug());

            var serviceProvider = services.BuildServiceProvider();
            _context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            _userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            if (!roleManager.RoleExistsAsync("Admin").Result)
                roleManager.CreateAsync(new IdentityRole("Admin")).Wait();
        }

        protected async Task<User> CreateTestUser(string email = "test@example.com", bool isAdmin = false)
        {
            var user = new User
            {
                UserName = email,
                Email = email,
                FullName = "Test User",
                BirthDate = DateTime.UtcNow.AddYears(-20),
                JoinDate = DateTime.UtcNow.AddDays(-30),
                Location = "Test Location",
                Image = "/default-profile.jpg",
                Description = "Test user description",
                PhoneNumber = "123456789",
                IsAdmin = isAdmin
            };

            await _userManager.CreateAsync(user, "Test123!");
            
            if (isAdmin)
                await _userManager.AddToRoleAsync(user, "Admin");
            
            return user;
        }

        public void Dispose() => 
            _context.Database.EnsureDeleted();
    }
}