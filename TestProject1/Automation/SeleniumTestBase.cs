using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;

namespace SeleniumTests
{
    public abstract class SeleniumTestBase : IDisposable
    {
        protected readonly IWebDriver Driver;
        protected readonly string BaseUrl = "https://localhost:7052";
        protected readonly WebDriverWait Wait;

        protected SeleniumTestBase()
        {
            var options = new ChromeOptions();
            Driver = new ChromeDriver(options);
            Driver.Manage().Window.Maximize();
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
        }

        protected void SetupTestData()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/test-setup/setup-test-data");
        }

        public void Dispose()
        {
            try 
            {
                Driver.Navigate().GoToUrl($"{BaseUrl}/test-setup/reset-test-database");
                Driver.Quit();
                Driver.Dispose();
            }
            catch { }
        }

        protected void GoToLoginPage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Login");
        }

        protected void GoToRegisterPage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Register");
        }

        protected bool IsLoggedIn(string email = null)
        {
            bool hasLogoutLink = Driver.FindElements(By.CssSelector("form[asp-page='/Account/Logout'] button")).Count > 0;
            bool hasManageLink = Driver.FindElements(By.CssSelector("a[href*='/Account/Manage']")).Count > 0;
            bool hasGreeting = email != null && Driver.FindElements(By.PartialLinkText(email)).Count > 0;
            return hasLogoutLink || hasManageLink || hasGreeting;
        }

        protected bool IsLoggedOut()
        {
            return Driver.FindElements(By.CssSelector("a[href*='/Account/Login'], a[href*='/Account/Register']")).Count > 0;
        }

        protected void Login(string email, string password, bool rememberMe = true)
        {
            GoToLoginPage();
            
            Wait.Until(d => d.FindElement(By.Id("Input_Email")).Displayed);
            Driver.FindElement(By.Id("Input_Email")).SendKeys(email);
            Driver.FindElement(By.Id("Input_Password")).SendKeys(password);
            
            if (rememberMe)
            {
                var checkbox = Driver.FindElements(By.Id("Input_RememberMe"));
                if (checkbox.Count > 0 && !checkbox[0].Selected)
                {
                    checkbox[0].Click();
                }
            }
            
            Driver.FindElement(By.CssSelector("button.btn-primary")).Click();
            
            Wait.Until(d => 
                d.Url != $"{BaseUrl}/Identity/Account/Login" || 
                d.FindElements(By.CssSelector(".validation-summary-errors")).Count > 0);
            
            if (Driver.FindElements(By.CssSelector(".validation-summary-errors")).Count > 0)
            {
                throw new Exception($"Login failed: {Driver.FindElement(By.CssSelector(".validation-summary-errors")).Text}");
            }
        }



        protected void Logout()
        {
            var logoutButtons = Driver.FindElements(By.CssSelector("form[asp-page='/Account/Logout'] button"));
            if (logoutButtons.Count > 0)
            {
                logoutButtons[0].Click();
                Wait.Until(d => d.Url.Contains("/Identity/Account/Logout") || d.Url.Contains("/Home/Index"));
            }
        }
    }
}