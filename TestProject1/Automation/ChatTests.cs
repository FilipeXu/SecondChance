using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using System;
using SeleniumTests;

namespace TestProject1.Automation
{
    public class ChatTests : SeleniumTestBase
    {
        [Fact]
        public void StartConversation_FromProductPage_Success()
        {
            SetupTestData();
            Login("test@example.com", "Test@123456");
            Driver.Navigate().GoToUrl($"{BaseUrl}/Products/Details/3");
            IWebElement contactButton = Wait.Until(d =>
                d.FindElement(By.CssSelector("a[href*='Chat/StartConversation'], .contact-user-btn, .btn-contact, a.btn-primary[href*='Chat']")));
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", contactButton);
            contactButton.Click();

            Wait.Until(d => d.Url.Contains("/Chat") || d.FindElements(By.CssSelector(".chat-container")).Count > 0);

            IWebElement messageInput = Wait.Until(d => 
                d.FindElement(By.CssSelector(".chat-input .form-control")));

            var messageText = $"Test message {Guid.NewGuid()}";
            messageInput.SendKeys(messageText);
            IWebElement sendButton = Wait.Until(d => 
                d.FindElement(By.CssSelector(".send-button")));

            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", sendButton);
        }

        [Fact]
        public void SendMessage_InExistingConversation_Success()
        {
            SetupTestData();
            Login("test@example.com", "Test@123456");
            Driver.Navigate().GoToUrl($"{BaseUrl}/Chat");

            Wait.Until(d => d.FindElements(By.CssSelector(".conversation-item")).Count > 0);

            Driver.FindElement(By.CssSelector(".conversation-link")).Click();

            Wait.Until(d => d.FindElements(By.CssSelector(".chat-container")).Count > 0);

            var messageText = $"Test message {Guid.NewGuid()}";
            var messageInput = Wait.Until(d => 
                d.FindElement(By.CssSelector(".chat-input .form-control")));
            messageInput.SendKeys(messageText);

            var sendButton = Wait.Until(d => 
                d.FindElement(By.CssSelector(".send-button")));
            sendButton.Click();

            Wait.Until(d => {
                var messages = d.FindElements(By.CssSelector(".message-content p"));
                return messages.Count > 0;
            });

            var messages = Driver.FindElements(By.CssSelector(".message-content p"));
            var lastMessage = messages[messages.Count - 1];
            Assert.Equal(messageText, lastMessage.Text);
        }

        [Fact]
        public void StartConversation_FromUserProfileViaProduct_Success()
        {
            SetupTestData();
            Login("test@example.com", "Test@123456");

            Driver.Navigate().GoToUrl($"{BaseUrl}/Products/Details/3");

            IWebElement sellerProfileLink = Wait.Until(d =>
            {
                try
                {
                    return d.FindElement(By.CssSelector(".donor-actions .btn-outline-secondary"));
                }
                catch
                {
                    try
                    {
                        return d.FindElement(By.CssSelector(".owner-name a"));
                    }
                    catch
                    {
                        return d.FindElement(By.CssSelector(".donor-details a"));
                    }
                }
            });

            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", sellerProfileLink);
            sellerProfileLink.Click();

            Wait.Until(d => d.Url.Contains("/Identity/Account/Manage"));

            IWebElement chatButton = Wait.Until(d =>
                d.FindElement(By.CssSelector("a[href*='Chat/StartConversation']")));

            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", chatButton);
            chatButton.Click();

            Wait.Until(d => d.Url.Contains("/Chat") || d.FindElements(By.CssSelector(".chat-container")).Count > 0);

            IWebElement messageInput = Wait.Until(d => 
                d.FindElement(By.CssSelector(".chat-input .form-control")));

            var messageText = $"Test message {Guid.NewGuid()}";
            messageInput.SendKeys(messageText);

            IWebElement sendButton = Wait.Until(d => 
                d.FindElement(By.CssSelector(".send-button")));

            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", sendButton);
        }
    }
}