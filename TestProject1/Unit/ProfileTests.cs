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
using System.Linq;

namespace TestProject1.Unit
{
    public class ProfileTests
    {
        private ApplicationDbContext GetMockContext() => 
            new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);

        private Mock<UserManager<User>> SetupUserManager() =>
            new Mock<UserManager<User>>(new Mock<IUserStore<User>>().Object, null, null, null, null, null, null, null, null);

        private ClaimsPrincipal SetupClaimsPrincipal(string userId)
        {
            var claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "testuser@test.com")
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        [Fact]
        public async Task OnGetAsync_ValidUser_ReturnsPageResult()
        {
            var userManagerMock = SetupUserManager();
            var signInManagerMock = new Mock<SignInManager<User>>(
                userManagerMock.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                null, null, null, null);

            var testUser = new User {
                Id = "test-user-id",
                UserName = "testuser@test.com",
                Email = "testuser@test.com",
                FullName = "Test User",
                IsActive = true
            };

            var claimsPrincipal = SetupClaimsPrincipal(testUser.Id);
            userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(testUser);
            userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(testUser.Id);

            var context = GetMockContext();
            var productRepoMock = new Mock<IProductRepository>();
            productRepoMock.Setup(x => x.GetUserProductsAsync(testUser.Id, It.IsAny<string>()))
                .ReturnsAsync(new List<Product>());

            var pageModel = new IndexModel(
                userManagerMock.Object,
                signInManagerMock.Object,
                context,
                productRepoMock.Object)
            {
                PageContext = new PageContext {
                    HttpContext = new DefaultHttpContext { User = claimsPrincipal }
                }
            };

            var result = await pageModel.OnGetAsync();

            Assert.IsType<PageResult>(result);
            Assert.Equal(testUser.FullName, pageModel.UserProfile.FullName);
            Assert.True(pageModel.IsCurrentUser);
        }

        [Fact]
        public async Task OnGetAsync_UserNotFound_ReturnsNotFoundResult()
        {
            var userManagerMock = SetupUserManager();
            var signInManagerMock = new Mock<SignInManager<User>>(
                userManagerMock.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                null, null, null, null);

            userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((User)null);

            var pageModel = new IndexModel(
                userManagerMock.Object,
                signInManagerMock.Object,
                GetMockContext(),
                new Mock<IProductRepository>().Object);

            var result = await pageModel.OnGetAsync();

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("Não foi possível carregar o utilizador", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task OnPostAsync_WithPasswordChange_UpdatesUserPassword()
        {
            var userManagerMock = SetupUserManager();
            var signInManagerMock = new Mock<SignInManager<User>>(
                userManagerMock.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                null, null, null, null);

            var testUser = new User {
                Id = "test-user-id",
                UserName = "testuser@test.com",
                Email = "testuser@test.com",
                FullName = "Test User"
            };

            var claimsPrincipal = SetupClaimsPrincipal(testUser.Id);
            userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(testUser);
            userManagerMock.Setup(x => x.HasPasswordAsync(testUser)).ReturnsAsync(true);
            userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
            userManagerMock.Setup(x => x.ChangePasswordAsync(testUser, "OldPass123!", "NewPass123!"))
                .ReturnsAsync(IdentityResult.Success);

            var pageModel = new IndexModel(
                userManagerMock.Object,
                signInManagerMock.Object,
                GetMockContext(),
                new Mock<IProductRepository>().Object) {
                PageContext = new PageContext {
                    HttpContext = new DefaultHttpContext { User = claimsPrincipal }
                },
                Input = new IndexModel.InputModel {
                    OldPassword = "OldPass123!",
                    NewPassword = "NewPass123!",
                    ConfirmPassword = "NewPass123!",
                    FullName = testUser.FullName
                }
            };

            var result = await pageModel.OnPostAsync();

            Assert.IsType<PageResult>(result);
            userManagerMock.Verify(x => x.ChangePasswordAsync(testUser, "OldPass123!", "NewPass123!"), Times.Once);
            userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
            signInManagerMock.Verify(x => x.RefreshSignInAsync(testUser), Times.Once);
            Assert.Contains("Seu perfil foi atualizado com sucesso", pageModel.StatusMessage);
        }

        [Fact]
        public async Task OnPostDeactivateAccountAsync_ValidUser_DeactivatesAccount()
        {
            var userManagerMock = SetupUserManager();
            var signInManagerMock = new Mock<SignInManager<User>>(
                userManagerMock.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                null, null, null, null);

            var testUser = new User {
                Id = "test-user-id",
                UserName = "testuser@test.com",
                Email = "testuser@test.com",
                IsActive = true
            };

            var claimsPrincipal = SetupClaimsPrincipal(testUser.Id);
            userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(testUser);
            userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

            var pageModel = new IndexModel(
                userManagerMock.Object,
                signInManagerMock.Object,
                GetMockContext(),
                new Mock<IProductRepository>().Object) {
                PageContext = new PageContext {
                    HttpContext = new DefaultHttpContext { User = claimsPrincipal }
                }
            };

            var result = await pageModel.OnPostDeactivateAccountAsync();

            Assert.False(testUser.IsActive);
            userManagerMock.Verify(x => x.UpdateAsync(testUser), Times.Once);
            signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Account/Login", redirectResult.PageName);
        }
    }
}