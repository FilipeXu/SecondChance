using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using System;

using SeleniumTests;

namespace TestProject1.Automation
{
    public class ModeratorTests : SeleniumTestBase
    {
        private void LoginAsModerator()
        {
            Login("test@example.com", "Test@123456");
            Wait.Until(d => d.FindElements(By.CssSelector("body")).Count > 0);
        }

        [Fact]
        public void ViewReports_ShowsReportsList()
        {
            SetupTestData();
            LoginAsModerator();
            
            Driver.Navigate().GoToUrl($"{BaseUrl}/Moderator/Reports");
            
            Wait.Until(d => 
                d.FindElements(By.TagName("table")).Count > 0 || 
                d.FindElements(By.CssSelector(".alert-info")).Count > 0
            );

            var tables = Driver.FindElements(By.TagName("table"));
            var emptyState = Driver.FindElements(By.CssSelector(".alert-info"));
            Assert.True(tables.Count > 0 || emptyState.Count > 0);
        }

        [Fact]
        public void ViewUserProfile_FromReportDetails_Success()
        {
            SetupTestData();
            LoginAsModerator();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Moderator/Reports");
            
            Wait.Until(d => d.FindElements(By.TagName("table")).Count > 0);
            
            var detailsButton = Driver.FindElement(By.CssSelector(".btn-info"));
            detailsButton.Click();
            
            Wait.Until(d => d.FindElements(By.CssSelector(".card")).Count > 0);
            
            var profileButton = Driver.FindElement(By.CssSelector("a.btn-outline-primary"));
            profileButton.Click();
            
            Wait.Until(d => d.FindElements(By.CssSelector(".profile-card")).Count > 0);
            Assert.Contains("Membro desde", Driver.PageSource);
        }

        [Fact]
        public void AccessModeratorPanel_NonAdmin_ShowsError()
        {
            SetupTestData();
            Login("secondary@example.com", "Test@123456");
            
            Driver.Navigate().GoToUrl($"{BaseUrl}/Moderator");
            
            Wait.Until(d => 
                d.Url.Contains("/Account/AccessDenied") || 
                d.FindElements(By.CssSelector(".access-denied")).Count > 0);
            
            Assert.True(
                Driver.Url.Contains("/Account/AccessDenied") || 
                Driver.FindElements(By.CssSelector(".access-denied")).Count > 0);
        }
    }
}