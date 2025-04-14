using OpenQA.Selenium;
using Xunit;
using System;
using System.Threading;
using OpenQA.Selenium.Support.UI;
using SeleniumTests;

namespace TestProject1.Automation
{
    public class StatisticsTests : SeleniumTestBase
    {
        private void LoginAsAdmin()
        {
            Login("test@example.com", "Test@123456");
        }

        [Fact]
        public void ViewStatistics_AsAdmin_ShowsCharts()
        {
            SetupTestData();
            LoginAsAdmin();
            
            Driver.Navigate().GoToUrl($"{BaseUrl}/Statistics");
            
            Wait.Until(d => d.FindElements(By.CssSelector(".stats-section, .stats-container")).Count > 0);

            bool hasAdminStats =
                Driver.PageSource.Contains("Utilizadores") ||
                Driver.PageSource.Contains("Reports") ||
                Driver.PageSource.Contains("Banidos");

            Assert.True(hasAdminStats, "Statistics page should display chart elements");
        }

        [Fact]
        public void ViewUserMetrics_ShowsUserStats()
        {
            SetupTestData();
            LoginAsAdmin();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Statistics");
            Wait.Until(d => d.FindElements(By.CssSelector(".stats-section, .stats-container")).Count > 0);
            

            bool hasUserStats = 
                Driver.PageSource.Contains("Utilizadores") || 
                Driver.PageSource.Contains("Utilizadores") || 
                Driver.PageSource.Contains("Users");
                
            Assert.True(hasUserStats, "User statistics should be displayed");
        }
    }
}