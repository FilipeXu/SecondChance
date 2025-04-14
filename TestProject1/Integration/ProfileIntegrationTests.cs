using Microsoft.AspNetCore.Mvc;
using SecondChance.Areas.Identity.Pages.Account.Manage;
using SecondChance.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Xunit;
using Moq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SecondChance.Data;
using System;

namespace TestProject1.Integration
{
    public class ProfileIntegrationTests : IntegrationTestBase
    {
        private readonly IndexModel _pageModel;
        private readonly SignInManager<User> _signInManager;
        private readonly Mock<IProductRepository> _productRepository;

        public ProfileIntegrationTests()
        {
            _productRepository = new Mock<IProductRepository>();
            
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            _signInManager = new Mock<SignInManager<User>>(
                _userManager, 
                contextAccessor.Object,
                userPrincipalFactory.Object,
                null, null, null, null).Object;
                
            _pageModel = new IndexModel(_userManager, _signInManager, _context, _productRepository.Object);
        }

        private void SetupPageModelUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext 
            { 
                User = claimsPrincipal 
            };
            
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            httpContext.Request.Method = "POST";
            
            _pageModel.PageContext = new PageContext
            {
                HttpContext = httpContext
            };
            
            _pageModel.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task OnGet_ReturnsEmptyProductsListIfUserHasNoProducts()
        {
            var user = await CreateTestUser("noproducts@example.com");
            SetupPageModelUser(user);

            _productRepository.Setup(r => r.GetUserProductsAsync(user.Id, It.IsAny<string>()))
                .ReturnsAsync(new List<Product>());

            await _pageModel.OnGetAsync();

            Assert.Equal(user.Id, _pageModel.UserProfile.Id);
            Assert.Empty(_pageModel.UserProducts);
        }

        [Fact]
        public async Task OnPost_UpdatesUserProfile()
        {
            var user = await CreateTestUser();
            
            user.FullName = "Original Name";
            user.Location = "Original Location";
            user.Description = "Original Description";
            await _context.SaveChangesAsync();
            
            SetupPageModelUser(user);

            _pageModel.Input = new IndexModel.InputModel
            {
                FullName = "Updated Name",
                Location = "Updated Location",
                Description = "Updated Description"
            };

            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "Input.FullName", "Updated Name" },
                { "Input.Location", "Updated Location" },
                { "Input.Description", "Updated Description" }
            });

            _pageModel.PageContext.HttpContext.Request.Form = formCollection;

            var result = await _pageModel.OnPostAsync();

            _context.Entry(user).Reload();
            
            Assert.NotNull(user);
            Assert.Equal("Updated Name", user.FullName);
            Assert.Equal("Updated Location", user.Location);
            Assert.Equal("Updated Description", user.Description);
            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnPost_HandlesInvalidModelState()
        {
            var user = await CreateTestUser();
            SetupPageModelUser(user);
            
            _pageModel.ModelState.AddModelError("Input.FullName", "Name is required");

            var result = await _pageModel.OnPostAsync();

            Assert.IsType<PageResult>(result);
            var unchangedUser = await _context.Users.FindAsync(user.Id);
            Assert.NotEqual("Updated Name", unchangedUser.FullName);
        }
    }
}