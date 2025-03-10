using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;

namespace AuthenticationTests
{
    public abstract class SeleniumTestBase : IDisposable
    {
        protected readonly IWebDriver Driver;
        protected readonly string BaseUrl = "https://localhost:7052";
        protected readonly WebDriverWait Wait;
        protected readonly WebDriverWait LongWait; 

        protected SeleniumTestBase()
        {
            var options = new ChromeOptions();
            Driver = new ChromeDriver(options);
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2); 
            Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(2)); 
            LongWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(2)); 

            Driver.Navigate().GoToUrl($"{BaseUrl}/test-setup/setup-test-users");
            LongWait.Until(d => d.FindElement(By.TagName("body")).Text.Contains("Test users created successfully") ||
                           d.FindElement(By.TagName("body")).Text.Contains("already exists"));
        }

        public void Dispose()
        {
            Driver.Quit();
            Driver.Dispose();
        }

        protected void GoToLoginPage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Login");
            Wait.Until(d => d.FindElements(By.Id("account")).Count > 0);
        }

        protected void GoToRegisterPage()
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Register");
        }

        protected bool IsLoggedIn(string? email = null)
        {
            try
            {
                bool hasLogoutLink = Driver.FindElements(By.CssSelector("form[asp-page='/Account/Logout'] button")).Count > 0;
                bool hasManageLink = Driver.FindElements(By.CssSelector("a[href*='/Account/Manage']")).Count > 0;
                bool hasGreeting = email != null ? Driver.FindElements(By.PartialLinkText(email)).Count > 0 : false;
                return hasLogoutLink || hasManageLink || hasGreeting;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected bool IsLoggedOut()
        {
            bool hasLoginLink = Driver.FindElements(By.CssSelector("a[href*='/Account/Login']")).Count > 0;
            bool hasRegisterLink = Driver.FindElements(By.CssSelector("a[href*='/Account/Register']")).Count > 0;
            return hasLoginLink || hasRegisterLink;
        }

        protected void Login(string email, string password, bool rememberMe = true)
        {
            GoToLoginPage();
            Wait.Until(d => {
                try {
                    var inputs = d.FindElements(By.CssSelector("#Input_Email, #Input_Password, button.btn-primary"));
                    return inputs.Count == 3 && inputs.All(i => i.Displayed);
                }
                catch {
                    return false;
                }
            });
            var emailInput = Driver.FindElement(By.Id("Input_Email"));
            emailInput.SendKeys(email);

            var passwordInput = Driver.FindElement(By.Id("Input_Password"));
            passwordInput.SendKeys(password);

            if (rememberMe)
            {
                try
                {
                    var rememberMeCheckbox = Driver.FindElement(By.Id("Input_RememberMe"));
                    if (!rememberMeCheckbox.Selected)
                    {
                        ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", rememberMeCheckbox);
                    }
                }
                catch (NoSuchElementException)
                {
                }
            }

   
            var submitButton = Driver.FindElement(By.CssSelector("button.btn-primary"));
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", submitButton);

         
            try
            {
                Wait.Until(d => d.Url != $"{BaseUrl}/Identity/Account/Login" || 
                                d.FindElements(By.CssSelector(".validation-summary-errors")).Count > 0);
            }
            catch (WebDriverTimeoutException)
            {
           
                if (!IsLoggedIn(email))
                {
                    throw;
                }
            }


            var errors = Driver.FindElements(By.CssSelector(".validation-summary-errors"));
            if (errors.Count > 0)
            {
                throw new Exception($"Login failed: {errors[0].Text}");
            }
        }

        protected void Register(string email, string password, string fullName, DateTime birthDate)
        {
            GoToRegisterPage();
            
            Driver.FindElement(By.Id("Input_Email")).SendKeys(email);
            Driver.FindElement(By.Id("Input_Password")).SendKeys(password);
            Driver.FindElement(By.Id("Input_ConfirmPassword")).SendKeys(password);
            Driver.FindElement(By.Id("Input_FullName")).SendKeys(fullName);
            Driver.FindElement(By.Id("Input_BirthDate")).SendKeys(birthDate.ToString("MM/dd/yyyy"));

            Driver.FindElement(By.CssSelector("button.btn-primary")).Click();

            Wait.Until(d => d.Url.Contains("/Home/Index") || 
                          d.FindElements(By.CssSelector(".validation-summary-errors")).Count > 0);
        }

        protected void Logout()
        {
            try
            {
                var logoutButton = Driver.FindElement(By.CssSelector("form[asp-page='/Account/Logout'] button"));
                logoutButton.Click();
                Wait.Until(d => d.Url.Contains("/Identity/Account/Logout") || d.Url.Contains("/Home/Index"));
            }
            catch (NoSuchElementException)
            {

            }
        }

       
    }
}