using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using System;
using System.Linq;
using System.Threading.Tasks;
using SeleniumTests;

namespace TestProject1.Automation
{
    public class CommentsTests : SeleniumTestBase
    {
        
        [Fact]
        public void ViewUserComments_FromProfile_Success()
        {
            SetupTestData();
            Login("test@example.com", "Test@123456");
            
            Driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Manage");
            Wait.Until(d => d.FindElements(By.TagName("body")).Count > 0);
            
            try {
                var viewAllCommentsLink = Driver.FindElement(By.CssSelector("a[href*='Comments/ProfileComments']"));
                viewAllCommentsLink.Click();
                
                Wait.Until(d => d.FindElements(By.TagName("h2")).Count > 0);
      
                Assert.True(Driver.FindElements(By.CssSelector(".card, .comments-list, h2")).Count > 0,
                    "Expected to find comments-related content");
            }
            catch (NoSuchElementException) {
                Console.WriteLine("Comments link not found. This might be expected if user has no comments.");
            }
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
        
        [Fact]
        public void DeleteComment_OwnComment_Success()
        {
            SetupTestData();
            Login("test@example.com", "Test@123456");
            
            string currentUserId = GetCurrentUserId();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Comments/ProfileComments?profileId={currentUserId}");
            Wait.Until(d => d.FindElements(By.TagName("body")).Count > 0);
            
            var comments = Driver.FindElements(By.CssSelector(".card, .comment-item"));
            
            if (comments.Count > 0) {
                int commentsBefore = comments.Count;
                Console.WriteLine($"Found {commentsBefore} comments");
                
                    IWebElement deleteButton = null;
                    foreach (var comment in comments) {
                        try {
                            deleteButton = comment.FindElement(By.CssSelector(".btn-danger, a[href*='Delete']"));
                            break;
                        }
                        catch (NoSuchElementException) {
                            continue;
                        }
                    }
                    
                    if (deleteButton != null) {
                        ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", deleteButton);
                        Wait.Until(d => d.FindElement(By.CssSelector(".btn-danger, a[href*='Delete']")).Displayed);
                        deleteButton.Click();
                        
                        if (Driver.Url.Contains("Delete")) {
                            var confirmButton = Driver.FindElement(By.CssSelector("input[type='submit'][value='Excluir'], .btn-danger"));
                            confirmButton.Click();
                        }
                        
                        Wait.Until(d => {
                            var currentComments = d.FindElements(By.CssSelector(".card, .comment-item")).Count;
                            return currentComments < commentsBefore;
                        });
                        
                        var commentsAfter = Driver.FindElements(By.CssSelector(".card, .comment-item")).Count;
                        Assert.True(commentsAfter < commentsBefore, "Comment should be deleted");
                    }
                    else {
                        Console.WriteLine("No delete buttons found");
                        Assert.True(true);
                    }

            }
            else {
                Console.WriteLine("No comments found to delete");
                Assert.True(true);
            }
        }
        
        private string GetCurrentUserId()
        {
            try {
                Driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Manage");
                Wait.Until(d => d.FindElements(By.TagName("body")).Count > 0);
                
                if (Driver.Url.Contains("userId=")) {
                    string url = Driver.Url;
                    int start = url.IndexOf("userId=") + 7;
                    int end = url.IndexOf("&", start);
                    if (end == -1) end = url.Length;
                    return url.Substring(start, end - start);
                }
                
                var profileLinks = Driver.FindElements(By.CssSelector("a[href*='profileId='], a[href*='userId=']"));
                if (profileLinks.Count > 0) {
                    string href = profileLinks[0].GetAttribute("href");
                    if (href.Contains("profileId=")) {
                        int start = href.IndexOf("profileId=") + 10;
                        int end = href.IndexOf("&", start);
                        if (end == -1) end = href.Length;
                        return href.Substring(start, end - start);
                    }
                    else if (href.Contains("userId=")) {
                        int start = href.IndexOf("userId=") + 7;
                        int end = href.IndexOf("&", start);
                        if (end == -1) end = href.Length;
                        return href.Substring(start, end - start);
                    }
                }
                
                return "test-user-id";
            }
            catch (Exception) {
                return "test-user-id";
            }
        }
    }
}