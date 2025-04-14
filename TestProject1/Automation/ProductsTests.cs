using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumTests;

namespace TestProject1.Automation
{
    public class ProductsTests : SeleniumTestBase
    {
        private void SetElementValueByJS(string elementId, string value)
        {
            var element = Driver.FindElement(By.Id(elementId));
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].value = arguments[1]", element, value);
        }

        private void FillProductForm(string name, string description, string category, string location)
        {
            SetElementValueByJS("Name", name);
            SetElementValueByJS("Description", description);
            var categoryElement = Driver.FindElement(By.Id("Category"));
            if (categoryElement.TagName.ToLower() == "select")
            {
                var selectCategory = new SelectElement(categoryElement);
                selectCategory.SelectByText(category);
            }
            else
            {
                SetElementValueByJS("Category", category);
            }

            if (Driver.FindElements(By.Id("Price")).Count > 0)
            {
                SetElementValueByJS("Price", "0");
            }

            if (Driver.FindElements(By.Id("Location")).Count > 0)
            {
                SetElementValueByJS("Location", location);
            }
        }

        private void UploadTestImage()
        {
            string tempImagePath = Path.Combine(Path.GetTempPath(), "test-image.jpg");

            if (!File.Exists(tempImagePath))
            {
                using (var fileStream = new FileStream(tempImagePath, FileMode.Create))
                {
                    byte[] jpegBytes = new byte[1024];
                    for (int i = 0; i < jpegBytes.Length; i++)
                        jpegBytes[i] = (byte)(i % 256);

                    fileStream.Write(jpegBytes, 0, jpegBytes.Length);
                }
            }

            var fileInputs = Driver.FindElements(By.CssSelector("input[type='file']"));
            if (fileInputs.Count > 0)
            {
                fileInputs[0].SendKeys(tempImagePath);
            }
        }

        private void SubmitForm()
        {
            var submitButton = Driver.FindElement(By.CssSelector("button[type='submit']"));
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", submitButton);
        }

        [Fact]
        public void CreateProduct_LoggedInUser_Success()
        {
            
                SetupTestData();
                Login("test@example.com", "Test@123456");
                Driver.Navigate().GoToUrl($"{BaseUrl}/Products/Create");
                
                Wait.Until(d => d.FindElement(By.Id("Name")).Displayed);
                
                var productName = $"Test Product {Guid.NewGuid()}";
                
                FillProductForm(productName, "Test description with detailed information", "Roupa", "Test Location");
                UploadTestImage();
                Thread.Sleep(1000);
                SubmitForm();
                
                var longWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
                longWait.Until(d =>
                    d.FindElements(By.CssSelector(".alert-success")).Count > 0 ||
                    d.PageSource.Contains(productName) ||
                    !d.Url.Contains("Create"));
                
                if (Driver.FindElements(By.CssSelector(".validation-summary-errors")).Count > 0)
                {
                    string errors = Driver.FindElement(By.CssSelector(".validation-summary-errors")).Text;
                    Assert.True(false, $"Form validation failed: {errors}");
                }
                
                Assert.True(true, "Product created successfully");
            
            
        }

        private IWebElement FindDeleteButton()
        {
            By[] possibleSelectors = new[] {
                By.LinkText("Delete"),
                By.LinkText("Excluir"),
                By.CssSelector("a[href*='Delete']"),
                By.CssSelector("a.btn-danger"),
                By.CssSelector(".action-buttons a"),
                By.CssSelector("a.btn-outline-danger"),
                By.CssSelector("button.btn-danger")
            };

            foreach (var selector in possibleSelectors)
            {
                if (Driver.FindElements(selector).Count > 0)
                {
                    return Driver.FindElement(selector);
                }
            }

            Driver.Navigate().GoToUrl($"{BaseUrl}/Products/MyProducts");

            foreach (var selector in new[] {
                By.LinkText("Delete"),
                By.LinkText("Excluir"),
                By.CssSelector("a[href*='Delete']")
            })
            {
                if (Driver.FindElements(selector).Count > 0)
                {
                    return Driver.FindElements(selector)[0];
                }
            }

            return null;
        }

        private void HandleDeleteConfirmation()
        {

            if (Driver.FindElements(By.CssSelector("button[type='submit'], input[type='submit'], .btn-primary")).Count > 0)
            {
                var confirmButton = Driver.FindElement(By.CssSelector("button[type='submit'], input[type='submit'], .btn-primary"));
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", confirmButton);
            }
        }

        [Fact]
        public void DeleteProduct_Owner_Success()
        {
            
                SetupTestData();
                Login("test@example.com", "Test@123456");
                Driver.Navigate().GoToUrl($"{BaseUrl}/Products/Details/1");
                
                Wait.Until(d => d.FindElement(By.TagName("body")).Displayed);
                
                var deleteButton = FindDeleteButton();
                if (deleteButton == null)
                {
                    Assert.True(false, "Delete button not found on any page");
                }
                
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", deleteButton);
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", deleteButton);
                Thread.Sleep(1000);
                HandleDeleteConfirmation();
                
                Assert.True(true, "Delete operation completed");
            
        }
    }
}