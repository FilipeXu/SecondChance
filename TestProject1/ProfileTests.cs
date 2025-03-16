//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.Extensions.Logging;
//using Moq;
//using SecondChance.Areas.Identity.Pages.Account.Manage;
//using SecondChance.Models;
//using System.Security.Claims;
//using Microsoft.AspNetCore.Http;
//using System.Threading.Tasks;
//using Xunit;

//namespace TestProject1
//{
//    public class ProfileTests
//    {
//        [Fact]
//        public async Task OnGetAsync_ValidUser_ReturnsPageResult()
//        {
//            // Arrange
//            var userId = "testUserId";
//            var user = new User { 
//                Id = userId, 
//                UserName = "test@example.com",
//                FullName = "Test User",
//                Location = "Test Location",
//                Description = "Test Description",
//                Image = "/Images/testimage.jpg",
//                JoinDate = DateTime.Now.AddMonths(-3),
//                IsActive = true
//            };

//            var userStore = new Mock<IUserStore<User>>();
//            var userManager = MockUserManager<User>();
//            userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
//                .ReturnsAsync(user);
//            userManager.Setup(um => um.GetUserIdAsync(user))
//                .ReturnsAsync(userId);
//            userManager.Setup(um => um.GetPhoneNumberAsync(user))
//                .ReturnsAsync("1234567890");
//            userManager.Setup(um => um.HasPasswordAsync(user))
//                .ReturnsAsync(true);

//            var signInManager = new Mock<SignInManager<User>>(
//                userManager.Object,
//                new Mock<IHttpContextAccessor>().Object,
//                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
//                null, null, null, null)
//                .Object;

//            var pageModel = new IndexModel(userManager.Object, signInManager);

//            // Act
//            var result = await pageModel.OnGetAsync();

//            // Assert
//            var pageResult = Assert.IsType<PageResult>(result);
//            Assert.Equal(user.FullName, pageModel.Input.FullName);
//            Assert.Equal("1234567890", pageModel.Input.PhoneNumber);
//            Assert.Equal(user.Location, pageModel.Input.Location);
//            Assert.Equal(user.Description, pageModel.Input.Description);
//            Assert.Equal(user.Image, pageModel.Input.Image);
//            Assert.True(pageModel.HasPassword);
//        }

//        [Fact]
//        public async Task OnGetAsync_UserNotFound_ReturnsNotFoundResult()
//        {
//            // Arrange
//            var userManager = MockUserManager<User>();
//            userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
//                .ReturnsAsync((User)null);

//            var signInManager = new Mock<SignInManager<User>>(
//                userManager.Object,
//                new Mock<IHttpContextAccessor>().Object,
//                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
//                null, null, null, null)
//                .Object;

//            var pageModel = new IndexModel(userManager.Object, signInManager);

//            // Act
//            var result = await pageModel.OnGetAsync();

//            // Assert
//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task OnPostAsync_WithPasswordChange_UpdatesUserPassword()
//        {
//            // Arrange
//            var user = new User {
//                Id = "testUserId",
//                FullName = "Test User"
//            };

//            var userManager = MockUserManager<User>();
//            userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
//                .ReturnsAsync(user);
//            userManager.Setup(um => um.UpdateAsync(It.IsAny<User>()))
//                .ReturnsAsync(IdentityResult.Success);
//            userManager.Setup(um => um.HasPasswordAsync(user))
//                .ReturnsAsync(true);
//            userManager.Setup(um => um.ChangePasswordAsync(user, "OldPassword", "NewPassword"))
//                .ReturnsAsync(IdentityResult.Success);

//            var signInManager = new Mock<SignInManager<User>>(
//                userManager.Object,
//                new Mock<IHttpContextAccessor>().Object,
//                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
//                null, null, null, null);

//            signInManager.Setup(sm => sm.RefreshSignInAsync(It.IsAny<User>()))
//                .Returns(Task.CompletedTask);

//            var pageModel = new IndexModel(userManager.Object, signInManager.Object)
//            {
//                Input = new IndexModel.InputModel
//                {
//                    FullName = "Test User",
//                    OldPassword = "OldPassword",
//                    NewPassword = "NewPassword",
//                    ConfirmPassword = "NewPassword"
//                }
//            };

//            // Act
//            var result = await pageModel.OnPostAsync();

//            // Assert
//            userManager.Verify(um => um.ChangePasswordAsync(user, "OldPassword", "NewPassword"), Times.Once);
            
//            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
//            Assert.Equal(null, redirectResult.PageName); 
//        }

//        [Fact]
//        public async Task OnPostDeactivateAccountAsync_ValidUser_DeactivatesAccount()
//        {
//            // Arrange
//            var user = new User {
//                Id = "testUserId",
//                IsActive = true
//            };

//            var userManager = MockUserManager<User>();
//            userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
//                .ReturnsAsync(user);
//            userManager.Setup(um => um.UpdateAsync(It.IsAny<User>()))
//                .ReturnsAsync(IdentityResult.Success);

//            var signInManager = new Mock<SignInManager<User>>(
//                userManager.Object,
//                new Mock<IHttpContextAccessor>().Object,
//                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
//                null, null, null, null);

//            signInManager.Setup(sm => sm.SignOutAsync())
//                .Returns(Task.CompletedTask);

//            var pageModel = new IndexModel(userManager.Object, signInManager.Object);

//            // Act
//            var result = await pageModel.OnPostDeactivateAccountAsync();

//            // Assert
//            userManager.Verify(um => um.UpdateAsync(It.Is<User>(u => u.IsActive == false)), Times.Once);
//            signInManager.Verify(sm => sm.SignOutAsync(), Times.Once);
            
//            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
//            Assert.Equal("/Account/Login", redirectResult.PageName);
//        }
//        private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
//        {
//            var store = new Mock<IUserStore<TUser>>();
//            var mgr = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
//            mgr.Object.UserValidators.Add(new UserValidator<TUser>());
//            mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());
//            return mgr;
//        }
//    }
//}