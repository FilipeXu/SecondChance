using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using SecondChance.Data;
using SecondChance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

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
                    IsAdmin = true,
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
                    IsAdmin = false,
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
          
            if (!await _context.Comments.AnyAsync(c => c.AuthorId == mainUser.Id && c.ProfileId == secondaryUser.Id))
            {
                var comments = new List<Comment>
                {
                    new Comment
                    {
                        Content = "This is a test comment from main user",
                        AuthorId = mainUser.Id,
                        Author = mainUser,
                        ProfileId = secondaryUser.Id,
                        Profile = secondaryUser,
                        CreatedAt = DateTime.Now.AddDays(-1)
                    },
                    new Comment
                    {
                        Content = "This is a test comment from secondary user",
                        AuthorId = secondaryUser.Id,
                        Author = secondaryUser,
                        ProfileId = mainUser.Id,
                        Profile = mainUser,
                        CreatedAt = DateTime.Now.AddDays(-2)
                    }
                };

                await _context.Comments.AddRangeAsync(comments);
                await _context.SaveChangesAsync();
            }
            if (!await _context.UserReports.AnyAsync(r => r.ReporterUserId == mainUser.Id && r.ReportedUserId == secondaryUser.Id))
            {
                var reports = new List<UserReport>
                {
                    new UserReport
                    {
                        ReporterUserId = mainUser.Id,
                        ReporterUser = mainUser,
                        ReportedUserId = secondaryUser.Id,
                        ReportedUser = secondaryUser,
                        Reason = "Test report reason",
                        Details = "Additional details for test report", 
                        ReportDate = DateTime.Now.AddDays(-1),
                        IsResolved = false,
                        Resolution = null,
                        ResolvedDate = null,
                        ResolvedById = null
                    }
                };

                await _context.UserReports.AddRangeAsync(reports);
                await _context.SaveChangesAsync();
            }
            if (!await _context.ChatMessages.AnyAsync(m => m.SenderId == mainUser.Id && m.ReceiverId == secondaryUser.Id))
            {
                var mainToSecondaryConversationId = $"{mainUser.Id}-{secondaryUser.Id}";
                var secondaryToMainConversationId = $"{secondaryUser.Id}-{mainUser.Id}";

                var chatMessages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        SenderId = mainUser.Id,
                        ReceiverId = secondaryUser.Id,
                        Content = "This is a test message from main user",
                        SentAt = DateTime.Now.AddDays(-1),
                        IsRead = true,
                        ConversationId = mainToSecondaryConversationId
                    },
                    new ChatMessage
                    {
                        SenderId = secondaryUser.Id,
                        ReceiverId = mainUser.Id,
                        Content = "This is a test message from secondary user",
                        SentAt = DateTime.Now.AddDays(-2),
                        IsRead = true,
                        ConversationId = secondaryToMainConversationId
                    }
                };

                await _context.ChatMessages.AddRangeAsync(chatMessages);
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