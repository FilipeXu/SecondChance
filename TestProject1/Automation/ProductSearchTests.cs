using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumTests;

namespace TestProject1.Automation
{
    public class ProductSearchTests : SeleniumTestBase
    {

        [Fact]
        public void SearchProducts_ByTerm_ShowsMatchingResults()
        {
            SetupTestData();
            Driver.Navigate().GoToUrl($"{BaseUrl}/Products");
            Wait.Until(d => d.FindElements(By.Id("searchInput")).Count > 0);
            var searchInput = Driver.FindElement(By.Id("searchInput"));
            searchInput.Clear();
            searchInput.SendKeys("Test Product 1");
            Driver.FindElement(By.Id("searchButton")).Click();

            Wait.Until(d => d.PageSource.Contains("Test Product 1"));
            Assert.Contains("Test Product 1", Driver.PageSource);
        }

        [Fact]
        public void FilterProducts_ByCategory_ShowsFilteredResults()
        {
            SetupTestData();
            
            Driver.Navigate().GoToUrl($"{BaseUrl}/Products");
            Wait.Until(d => d.FindElements(By.Name("category")).Count > 0);
            var categoryElement = Driver.FindElement(By.Name("category"));
            if (categoryElement.TagName.ToLower() == "select")
            {
                var selectCategory = new SelectElement(categoryElement);
                selectCategory.SelectByText("Roupa");  
            }
            else
            {
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].value = arguments[1]", categoryElement, "Roupa");
            }
            Wait.Until(d => d.PageSource.Contains("Roupa"));
            Wait.Until(d => d.FindElements(By.CssSelector(".filter-tag")).Count > 0);

            Assert.Contains("Roupa", Driver.PageSource);

            var filterTag = Driver.FindElement(By.CssSelector(".filter-tag"));
            Assert.Contains("Roupa", filterTag.Text);
        }
    }
}