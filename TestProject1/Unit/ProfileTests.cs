using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using SecondChance.Areas.Identity.Pages.Account.Manage;
using SecondChance.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Xunit;
using SecondChance.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;

namespace TestProject1
{
    public class ProfileTests
    {
        private readonly Mock<UserManager<User>> _userManager;
        private readonly Mock<SignInManager<User>> _signInManager;
        private readonly Mock<IProductRepository> _productRepository;
        private readonly Mock<ApplicationDbContext> _context;
        private readonly User _testUser;

        public ProfileTests()
        {
            _testUser = new User 
            { 
                Id = "testUserId", 
                UserName = "test@example.com",
                FullName = "Test User",
                Location = "Test Location",
                Description = "Test Description",
                Image = "/Images/testimage.jpg",
                JoinDate = DateTime.Now.AddMonths(-3),
                IsActive = true
            };

            _userManager = MockUserManager();
            _signInManager = MockSignInManager();
            _productRepository = new Mock<IProductRepository>();
            _context = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
        }

        private Mock<UserManager<User>> MockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            var userManager = new Mock<UserManager<User>>(
                store.Object, null, null, null, null, null, null, null, null);
            return userManager;
        }

        private Mock<SignInManager<User>> MockSignInManager()
        {
            return new Mock<SignInManager<User>>(
                _userManager.Object, 
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                null, null, null, null);
        }

        private IndexModel CreatePageModel()
        {
            return new IndexModel(
                _userManager.Object,
                _signInManager.Object,
                _context.Object,
                _productRepository.Object);
        }

        //[Fact]
        //public async Task OnGetAsync_ValidUser_ReturnsPageResult()
        //{
        //    _userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
        //        .ReturnsAsync(_testUser);
        //    _userManager.Setup(um => um.GetPhoneNumberAsync(_testUser))
        //        .ReturnsAsync("1234567890");
        //    _userManager.Setup(um => um.HasPasswordAsync(_testUser))
        //        .ReturnsAsync(true);

        //    _productRepository.Setup(repo => repo.GetUserProductsAsync(_testUser.Id, It.IsAny<string>()))
        //        .ReturnsAsync(new List<Product> { new Product { Id = 1, Name = "Test Product", OwnerId = _testUser.Id } });

        //    var pageModel = CreatePageModel();
        //    var result = await pageModel.OnGetAsync();

        //    Assert.IsType<PageResult>(result);
        //    Assert.Equal(_testUser.FullName, pageModel.Input.FullName);
        //    Assert.Equal(_testUser.Location, pageModel.Input.Location);
        //    Assert.Equal(_testUser.Description, pageModel.Input.Description);
        //    Assert.Equal(_testUser.Image, pageModel.Input.Image);
        //    Assert.True(pageModel.HasPassword);
        //    Assert.Single(pageModel.UserProducts);
        //}

        [Fact]
        public async Task OnGetAsync_UserNotFound_ReturnsNotFoundResult()
        {
            _userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(value: null!);

            var result = await CreatePageModel().OnGetAsync();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        //[Fact]
        //public async Task OnPostAsync_WithPasswordChange_UpdatesUserPassword()
        //{
        //    _userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
        //        .ReturnsAsync(_testUser);
        //    _userManager.Setup(um => um.UpdateAsync(It.IsAny<User>()))
        //        .ReturnsAsync(IdentityResult.Success);
        //    _userManager.Setup(um => um.HasPasswordAsync(_testUser))
        //        .ReturnsAsync(true);
        //    _userManager.Setup(um => um.ChangePasswordAsync(_testUser, "OldPassword", "NewPassword"))
        //        .ReturnsAsync(IdentityResult.Success);

        //    var pageModel = CreatePageModel();
        //    pageModel.Input = new IndexModel.InputModel
        //    {
        //        FullName = "Test User",
        //        OldPassword = "OldPassword",
        //        NewPassword = "NewPassword",
        //        ConfirmPassword = "NewPassword"
        //    };

        //    var result = await pageModel.OnPostAsync();

        //    _userManager.Verify(um => um.ChangePasswordAsync(_testUser, "OldPassword", "NewPassword"), Times.Once);
        //    var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        //    Assert.Null(redirectResult.PageName);
        //}

        [Fact]
        public async Task OnPostDeactivateAccountAsync_ValidUser_DeactivatesAccount()
        {
            _userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser);
            _userManager.Setup(um => um.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);

            var result = await CreatePageModel().OnPostDeactivateAccountAsync();

            _userManager.Verify(um => um.UpdateAsync(It.Is<User>(u => !u.IsActive)), Times.Once);
            _signInManager.Verify(sm => sm.SignOutAsync(), Times.Once);
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Account/Login", redirectResult.PageName);
        }
    }
}