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
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
            Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(4));
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

    }
}