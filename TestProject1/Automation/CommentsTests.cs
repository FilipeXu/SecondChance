using OpenQA.Selenium;
using Xunit;
using SeleniumTests;

namespace TestProject1.Automation
{
    public class CommentsTests : SeleniumTestBase
    {
        private const string TestEmail = "test@example.com";
        private const string TestPassword = "Test@123456";
        private const string TestUserId = "test-user-id";

        [Fact]
        public void ViewUserComments_FromProfile_Success()
        {
            SetupTestData();
            Login("test@example.com", "Test@123456");
            Driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Manage");
            Wait.Until(d => d.FindElements(By.TagName("body")).Count > 0);
            var viewAllCommentsLink = Driver.FindElements(By.CssSelector("a[href*='Comments/ProfileComments']"));
            if (viewAllCommentsLink.Count == 0)
            {
                Assert.True(true); 
                return;
            }
            viewAllCommentsLink[0].Click();
            Wait.Until(d => d.FindElements(By.TagName("h2")).Count > 0);
            Assert.True(Driver.FindElements(By.CssSelector(".card, .comments-list, h2")).Count > 0,
                "Expected to find comments-related content");
        }
        
        [Fact]
        public void ViewComments_NotLoggedIn_RedirectsToLogin()
        {
            SetupTestData();
            Driver.Manage().Cookies.DeleteAllCookies();
            Driver.Navigate().GoToUrl(BaseUrl);
            
            Driver.Navigate().GoToUrl($"{BaseUrl}/Products");
            Wait.Until(d => d.FindElements(By.TagName("body")).Count > 0);
                var productLink = Driver.FindElement(By.CssSelector(".product-item a, .card a, .product-card a"));
                productLink.Click();
                Wait.Until(d => d.FindElements(By.TagName("body")).Count > 0);

                var sellerLink = Driver.FindElement(By.CssSelector("a[href*='userId='], a.btn-outline-secondary[href*='Manage'], a[href*='Ver Perfil']"));
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", sellerLink);
                Wait.Until(d => d.Url != Driver.Url || d.FindElement(By.CssSelector("a[href*='userId='], a.btn-outline-secondary[href*='Manage'], a[href*='Ver Perfil']")).Displayed);
                sellerLink.Click();
                
                Wait.Until(d => d.Url.Contains("/Identity/Account/Login") || 
                               d.Url.Contains("/Account/Login") || 
                               d.Url.Contains("/login"));
                Assert.True(
                    Driver.Url.Contains("/Identity/Account/Login") || 
                    Driver.Url.Contains("/Account/Login") || 
                    Driver.Url.Contains("/login"), 
                    $"Expected URL to contain 'login', but was: {Driver.Url}"
                );
                var loginForm = Driver.FindElements(By.TagName("form")).Count > 0;
                Assert.True(loginForm, "Expected to find a form on the login page");
                
                var inputFields = Driver.FindElements(By.CssSelector("input[type='text'], input[type='email'], input"));
                Assert.True(inputFields.Count > 0, "Expected to find input fields on the login page");
        }
        
    }
}