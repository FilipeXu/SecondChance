using OpenQA.Selenium;
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
            searchInput.SendKeys("Electronics");
            Driver.FindElement(By.Id("searchButton")).Click();

            Wait.Until(d => d.PageSource.Contains("Electronics"));
            Assert.Contains("Electronics", Driver.PageSource);
        }

        [Fact]
        public void FilterProducts_ByCategory_ShowsFilteredResults()
        {
            SetupTestData();
            
            Driver.Navigate().GoToUrl($"{BaseUrl}/Products");
            Wait.Until(d => d.FindElements(By.Name("category")).Count > 0);

            var categorySelect = Driver.FindElement(By.Name("category"));
            var selectElement = new OpenQA.Selenium.Support.UI.SelectElement(categorySelect);
            selectElement.SelectByText("Electronics");

            Thread.Sleep(1000);

            Assert.Contains("Electronics", Driver.PageSource);

            var filterTag = Driver.FindElement(By.CssSelector(".filter-tag"));
            Assert.Contains("Electronics", filterTag.Text);
        }
    }
}