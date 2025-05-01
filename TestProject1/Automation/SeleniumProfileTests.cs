using System;
using OpenQA.Selenium;
using Xunit;
using System.Threading;
using OpenQA.Selenium.Support.UI;
using SeleniumTests;

namespace TestProject1.Automation
{
    public class SeleniumProfileTests : SeleniumTestBase
    {
        private const string DefaultEmail = "test@example.com";
        private const string DefaultPassword = "Test@123456";
        private const string ProfileUrl = "/Identity/Account/Manage";

        private void GoToProfile()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}{ProfileUrl}");
            Wait.Until(d => d.FindElement(By.TagName("body")).Displayed);
        }



        private void OpenEditForm()
        {
            var editButton = Driver.FindElement(By.Id("edit-profile-btn"));
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", editButton);
            Wait.Until(d => d.FindElement(By.Id("profile-form")).Displayed);
        }

        [Fact]
        public void ProfilePage_LoadsSuccessfully_WhenLoggedIn()
        {
            SetupTestData();
            Login(DefaultEmail, DefaultPassword);
            GoToProfile();
            
            Assert.Contains(ProfileUrl, Driver.Url);
            var pageSource = Driver.PageSource.ToLower();
            bool hasProfileContent = pageSource.Contains("perfil") || 
                                    pageSource.Contains("conta") ||
                                    pageSource.Contains("utilizador");
            
            Assert.True(hasProfileContent);
        }

        [Fact]
        public void ProfilePage_ShowsUserInformation()
        {
            SetupTestData();
            Login(DefaultEmail, DefaultPassword);
            GoToProfile();
            
            var body = Driver.FindElement(By.TagName("body")).Text;
            bool hasProfileContent = body.Contains("Perfil") || 
                                    body.Contains("Membro desde") || 
                                    body.Contains("Localização") ||
                                    body.Contains("Dados");
            
            Assert.True(hasProfileContent);
        }

        [Fact]
        public void TabNavigation_SwitchesBetweenTabs()
        {
            SetupTestData();
            Login(DefaultEmail, DefaultPassword);
            GoToProfile();
            OpenEditForm();
            var privateTab = Driver.FindElement(By.CssSelector("[data-tab='private-data']"));
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", privateTab);
            
            Wait.Until(d => d.FindElement(By.Id("private-data")).GetAttribute("class").Contains("active"));
            
            var publicTab = Driver.FindElement(By.CssSelector("[data-tab='public-data']"));
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", publicTab);
            
            Wait.Until(d => d.FindElement(By.Id("public-data")).GetAttribute("class").Contains("active"));
        }
    }
}
