using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SecondChance.Models;
using System;
using System.Threading.Tasks;

#if DEBUG
namespace SecondChance.Controllers
{
    [Route("test-setup")]
    public class TestSetupController : Controller
    {
        private readonly UserManager<User> _userManager;
        public TestSetupController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }
        [HttpGet("setup-test-users")]
        public async Task<IActionResult> SetupTestUsers()
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
                    return BadRequest($"Failed to create test user: {string.Join(", ", result.Errors)}");
                }


                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    return BadRequest($"Failed to confirm test user email: {string.Join(", ", result.Errors)}");
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
                    return BadRequest($"Failed to create secondary user: {string.Join(", ", result.Errors)}");
                }
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    return BadRequest($"Failed to confirm secondary user email: {string.Join(", ", result.Errors)}");
                }
            }

            return Ok("Test users created successfully");
        }
    }
}
#endif