using OpenQA.Selenium;
using Xunit;
using System;
using System.Threading;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using SeleniumTests;

namespace TestProject1.Automation
{
    public class ReportTests : SeleniumTestBase
    {
        [Fact]
        public void SubmitReport_FromUserProfile_Success()
        {
            SetupTestData();
            Login("test@example.com", "Test@123456");
            
            Driver.Navigate().GoToUrl($"{BaseUrl}/Products/Details/3");
            
            WaitForElement(By.TagName("body"));
            
            By[] reportSelectors = new[] {
                By.LinkText("Report User"),
                By.LinkText("Reportar Utilizador"),
                By.CssSelector("a[href*='ReportUser']"),
                By.CssSelector(".btn-outline-danger")
            };
            
            IWebElement reportLink = FindFirstAvailableElement(reportSelectors);
            
            ClickElementSafely(reportLink);
            
            var reasonSelect = WaitForElement(By.Id("Reason"));
            var select = new SelectElement(reasonSelect);
            select.SelectByIndex(1);
            
            var detailsField = WaitForElement(By.Id("Details"));
            var reportText = $"Test report {Guid.NewGuid()}";
            detailsField.SendKeys(reportText);
            
            Thread.Sleep(1000);
            var submitButton = WaitForElement(By.CssSelector("button[type='submit']"));
            ClickElementSafely(submitButton);
            
            WebDriverWait wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            bool isSuccess = wait.Until(d => 
                d.FindElements(By.CssSelector(".alert-success")).Count > 0 || 
                d.PageSource.Contains("sucesso") || 
                d.PageSource.Contains("success") ||
                !d.Url.Contains("Report")
            );
            
            Assert.True(isSuccess, "Report should be submitted successfully");
        }
        
        private IWebElement FindFirstAvailableElement(By[] selectors)
        {
            foreach (var selector in selectors)
            {
                var element = TryFindElement(selector);
                if (element != null)
                    return element;
            }
            return null;
        }

        private void ClickElementSafely(IWebElement element)
        {
            ScrollIntoView(element);
            try {
                element.Click();
            } catch {
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", element);
            }
        }
        
        private IWebElement? TryFindElement(By by)
        {
            try
            {
                return Driver.FindElement(by);
            }
            catch
            {
                return null;
            }
        }

        private void ScrollIntoView(IWebElement element)
        {
            if (element != null)
            {
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
            }
        }

        private IWebElement WaitForElement(By by, int timeoutSeconds = 10)
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
            return wait.Until(d => d.FindElement(by));
        }
    }
}