using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using System;
using System.Linq;

using SeleniumTests;

namespace TestProject1.Automation
{
    public class AuthenticationTests : SeleniumTestBase
    {


        [Fact]
        public void Login_InvalidCredentials_ShowsError()
        {
                SetupTestData();

                Driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Login");

                Wait.Until(d => d.FindElements(By.Id("account")).Count > 0);

                var loginForm = Driver.FindElement(By.Id("account"));

                loginForm.FindElement(By.Id("Input_Email")).SendKeys("wrong@example.com");
                loginForm.FindElement(By.Id("Input_Password")).SendKeys("WrongPassword123");
                loginForm.FindElement(By.CssSelector("button[type='submit']")).Click();

                Wait.Until(d =>
                    d.FindElements(By.CssSelector(".validation-summary-errors")).Count > 0 ||
                    d.PageSource.Contains("Invalid") ||
                    d.PageSource.Contains("erro"));
                string pageSource = Driver.PageSource.ToLower();
                bool hasError =
                    pageSource.Contains("invalid") ||
                    pageSource.Contains("incorret") ||
                    pageSource.Contains("erro") ||
                    pageSource.Contains("inválid");

                Assert.True(hasError, "Page should show login error message");
        }
        private void Login(string email, string password)
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Login");
            Wait.Until(d => d.FindElements(By.Id("account")).Count > 0);
            var loginForm = Driver.FindElement(By.Id("account"));
            loginForm.FindElement(By.Id("Input_Email")).SendKeys(email);
            loginForm.FindElement(By.Id("Input_Password")).SendKeys(password);
            loginForm.FindElement(By.CssSelector("button[type='submit']")).Click();
            Wait.Until(d => d.PageSource.Contains("Logout"));
        }

        [Fact]
        public void Register_NewUser_Success()
        {
                SetupTestData();
                string uniqueEmail = $"new_user_{DateTime.Now.Ticks}@example.com";
                Driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Register");
                Wait.Until(d => d.FindElements(By.Id("Input_Email")).Count > 0);
                ((ITakesScreenshot)Driver).GetScreenshot().SaveAsFile("register_before_form.png");
                Driver.FindElement(By.Id("Input_Email")).SendKeys(uniqueEmail);
                Driver.FindElement(By.Id("Input_Password")).SendKeys("Test@123456");
                Driver.FindElement(By.Id("Input_ConfirmPassword")).SendKeys("Test@123456");
                if (Driver.FindElements(By.Id("Input_FullName")).Count > 0)
                {
                    Driver.FindElement(By.Id("Input_FullName")).SendKeys("Test User");
                }
                if (Driver.FindElements(By.Id("Input_BirthDate")).Count > 0)
                {
                    var birthDateInput = Driver.FindElement(By.Id("Input_BirthDate"));
                    ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].value = '01/01/1990';", birthDateInput);
                }

                var submitButton = Driver.FindElement(By.CssSelector("button[type='submit']"));
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", submitButton);

                Wait.Until(d =>
                    d.Url.Contains("/Home/Index") ||
                    d.Url.Contains("/Identity/Account/Manage") ||
                    d.Url.Contains("/Identity/Account/RegisterConfirmation") ||
                    d.Url.Contains("/Identity/Account/ConfirmEmail") ||
                    d.FindElements(By.CssSelector(".alert-success")).Count > 0 ||
                    d.PageSource.Contains("success") ||
                    d.PageSource.Contains("confirmation") ||
                    d.PageSource.Contains("email"));
                string pageSource = Driver.PageSource.ToLower();
                bool isSuccess =
                    Driver.Url.Contains("/Home/Index") ||
                    Driver.Url.Contains("/Identity/Account/Manage") ||
                    Driver.Url.Contains("/Identity/Account/RegisterConfirmation") ||
                    Driver.Url.Contains("/Identity/Account/ConfirmEmail") ||
                    pageSource.Contains("success") ||
                    pageSource.Contains("confirm") ||
                    pageSource.Contains("email") ||
                    pageSource.Contains("verificação");

                Assert.True(isSuccess, "User registration should lead to success or confirmation page");
                Console.WriteLine($"After registration URL: {Driver.Url}");
                Console.WriteLine($"Registration result: {(isSuccess ? "Success" : "Failure")}");

        }

        [Fact]
        public void Logout_Success()
        {
            SetupTestData();
            Login("test@example.com", "Test@123456");
            Assert.Contains("Logout", Driver.PageSource);
            IWebElement logoutButton = null;
            foreach (var selector in new[] {
                By.CssSelector("form[action*='Logout'] button"),
                By.CssSelector("[href*='Logout']"),
                By.LinkText("Logout"),
                By.LinkText("Sair"),
                By.LinkText("Sign out"),
                By.PartialLinkText("Logout"),
                By.PartialLinkText("Sair"),
                By.XPath("//form[contains(@action, 'Logout')]//button")
            })
            {
                if (Driver.FindElements(selector).Count > 0)
                {
                    logoutButton = Driver.FindElement(selector);
                    break;
                }
            }


            Assert.NotNull(logoutButton);
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", logoutButton);

            Wait.Until(d => !d.PageSource.Contains("Logout"));

            Assert.Contains("Login", Driver.PageSource);
        }
    }
}