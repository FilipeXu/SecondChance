using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Moq;
using SecondChance.Areas.Identity.Pages.Account.Manage;
using SecondChance.Data;
using SecondChance.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using System;

namespace TestProject1.Unit
{
    public class ProfileTests
    {
        private readonly string _userId = "test-user-id";
        private readonly string _email = "testuser@test.com";

        private ApplicationDbContext GetContext() =>
            new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private (Mock<UserManager<User>> UserManager, Mock<SignInManager<User>> SignInManager, User User, ClaimsPrincipal Claims)
            SetupTest(bool isActive = true, bool setupSuccess = true)
        {
            var user = new User
            {
                Id = _userId,
                UserName = _email,
                Email = _email,
                FullName = "Test User",
                IsActive = isActive
            };

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, _userId),
                new Claim(ClaimTypes.Name, _email)
            }, "Test"));

            var userManager = new Mock<UserManager<User>>(new Mock<IUserStore<User>>().Object,
                null, null, null, null, null, null, null, null);
            userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            if (setupSuccess)
                userManager.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

            var signInManager = new Mock<SignInManager<User>>(
                userManager.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                null, null, null, null);

            return (userManager, signInManager, user, claims);
        }

        private IndexModel SetupPageModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ClaimsPrincipal claims,
            IndexModel.InputModel input = null)
        {
            var model = new IndexModel(
                userManager,
                signInManager,
                GetContext(),
                new Mock<IProductRepository>().Object)
            {
                PageContext = new PageContext
                {
                    HttpContext = new DefaultHttpContext { User = claims }
                }
            };

            if (input != null)
                model.Input = input;

            return model;
        }

        [Fact]
        public async Task OnGetAsync_ValidUser_ReturnsPageResult()
        {
            var (userManager, signInManager, user, claims) = SetupTest();
            userManager.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(user.Id);

            var productRepoMock = new Mock<IProductRepository>();
            productRepoMock.Setup(x => x.GetUserProductsAsync(user.Id, It.IsAny<string>()))
                .ReturnsAsync(new List<Product>());

            var model = new IndexModel(
                userManager.Object,
                signInManager.Object,
                GetContext(),
                productRepoMock.Object)
            {
                PageContext = new PageContext
                {
                    HttpContext = new DefaultHttpContext { User = claims }
                }
            };

            var result = await model.OnGetAsync();

            Assert.IsType<PageResult>(result);
            Assert.Equal(user.FullName, model.UserProfile.FullName);
            Assert.True(model.IsCurrentUser);
        }

        [Fact]
        public async Task OnGetAsync_UserNotFound_ReturnsNotFoundResult()
        {
            var (userManager, signInManager, _, _) = SetupTest();
            userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((User)null);
            var model = SetupPageModel(userManager.Object, signInManager.Object, new ClaimsPrincipal());
            var result = await model.OnGetAsync();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task OnPostAsync_WithPasswordChange_UpdatesUserPassword()
        {
            var (userManager, signInManager, user, claims) = SetupTest();

            userManager.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
            userManager.Setup(x => x.ChangePasswordAsync(user, "OldPass123!", "NewPass123!"))
                .ReturnsAsync(IdentityResult.Success);

            var model = SetupPageModel(userManager.Object, signInManager.Object, claims,
                new IndexModel.InputModel
                {
                    OldPassword = "OldPass123!",
                    NewPassword = "NewPass123!",
                    ConfirmPassword = "NewPass123!",
                    FullName = user.FullName
                });
            var result = await model.OnPostAsync();
            Assert.IsType<PageResult>(result);
            userManager.Verify(x => x.ChangePasswordAsync(user, "OldPass123!", "NewPass123!"), Times.Once);
            Assert.Contains("sucesso", model.StatusMessage);
        }

        [Fact]
        public async Task OnPostDeactivateAccountAsync_ValidUser_DeactivatesAccount()
        {
            var (userManager, signInManager, user, claims) = SetupTest();
            var model = SetupPageModel(userManager.Object, signInManager.Object, claims);
            var result = await model.OnPostDeactivateAccountAsync();
            Assert.False(user.IsActive);
            userManager.Verify(x => x.UpdateAsync(user), Times.Once);
            signInManager.Verify(x => x.SignOutAsync(), Times.Once);
            Assert.IsType<RedirectToPageResult>(result);
        }
    }
}