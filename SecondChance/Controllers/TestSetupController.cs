using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondChance.Data;
using SecondChance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if DEBUG
namespace SecondChance.Controllers
{
    [Route("test-setup")]
    public class TestSetupController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public TestSetupController(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet("setup-test-data")]
        public async Task<IActionResult> SetupTestUsers()
        {
            var mainUser1 = await _userManager.FindByEmailAsync("test@example.com");
            if (mainUser1 == null)
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
                    BirthDate = new DateTime(1990, 1, 1),

                };

                var result = await _userManager.CreateAsync(user, "Test@123456");
                if (!result.Succeeded)
                {
                    return BadRequest($"Failed to create test user: {string.Join(", ", result.Errors)}");
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    return BadRequest($"Failed to confirm test user email: {string.Join(", ", result.Errors)}");
                }
            }
            var secondaryUser2 = await _userManager.FindByEmailAsync("secondary@example.com");
            if (secondaryUser2 == null)
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
                    BirthDate = new DateTime(1990, 1, 1),
                };

                var result = await _userManager.CreateAsync(user, "Test@123456");
                if (!result.Succeeded)
                {
                    return BadRequest($"Failed to create secondary user: {string.Join(", ", result.Errors)}");
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    return BadRequest($"Failed to confirm secondary user email: {string.Join(", ", result.Errors)}");
                }
            }

            var mainUser = await _userManager.FindByEmailAsync("test@example.com");
            var secondaryUser = await _userManager.FindByEmailAsync("secondary@example.com");
            if (!await _context.Products.AnyAsync(p => p.OwnerId == mainUser.Id && p.Name.Contains("Test Product")))
            {
                var products = new List<Product>
                {
                    new Product
                    {
                        Name = "Test Product 1",
                        Description = "First test product",
                        Category = Category.Eletrônicos,
                        PublishDate = DateTime.Now.AddDays(-10),
                        OwnerId = mainUser.Id,
                        Location = mainUser.Location,
                        Image = "/Images/products/test1.jpg"
                    },
                    new Product
                    {
                        Name = "Test Product 2",
                        Description = "Second test product",
                        Category = Category.Roupa,
                        PublishDate = DateTime.Now.AddDays(-5),
                        OwnerId = mainUser.Id,
                        Location = mainUser.Location,
                        Image = "/Images/products/test2.jpg"
                    }
                };

                await _context.Products.AddRangeAsync(products);

                var secondaryProducts = new List<Product>
                {
                    new Product
                    {
                        Name = "Secondary Test Product",
                        Description = "Secondary user test product",
                        Category = Category.Eletrônicos,
                        PublishDate = DateTime.Now.AddDays(-15),
                        OwnerId = secondaryUser.Id,
                        Location = secondaryUser.Location,
                        Image = "/Images/products/test3.jpg"
                    }
                };

                await _context.Products.AddRangeAsync(secondaryProducts);
                await _context.SaveChangesAsync();
            }


            return Ok("Test data created successfully");
        }

        [HttpGet("reset-test-database")]
        public async Task<IActionResult> ResetTestDatabase()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
            return Ok("Banco de dados resetado com sucesso");

        }
    }
}
#endif