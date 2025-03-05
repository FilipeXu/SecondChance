using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SecondChance.Areas.Identity.Pages.Account;
using SecondChance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationTests
{
    public class LoginTests
    {

        [Fact]
        public void Index_ReturnsViewResult()
        {
            var lm = new LoginModel(new SignInManager<User> signInManager,new ILogger<LoginModel> logger);

            var result = lm.SignIn()

            Assert.IsType<ViewResult>(result);
        }
    }
}
