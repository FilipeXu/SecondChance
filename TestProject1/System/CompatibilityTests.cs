using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Edge;
using System.Diagnostics;

namespace TestProject1
{
    public class CompatibilityTests : IClassFixture<TestDatabaseFixture>, IDisposable
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly HttpClient _client;
        private const string BaseUrl = "https://localhost:7052";
        private IWebDriver _driver;
        private bool _isAppRunning = false;

        public CompatibilityTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _client = new HttpClient();

            try
            {
                var response = _client.GetAsync(BaseUrl).GetAwaiter().GetResult();
                _isAppRunning = response.IsSuccessStatusCode;
            }
            catch
            {
                _isAppRunning = false;
            }
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }

        private void TestMobileView()
        {
            _driver.Manage().Window.Size = new System.Drawing.Size(375, 812);

            bool menuExists = _driver.FindElements(By.CssSelector(".navbar-toggler")).Count > 0;
            Assert.True(menuExists, "Mobile menu should be available on small screens");
        }

        private void TestDesktopView()
        {
            _driver.Manage().Window.Size = new System.Drawing.Size(1920, 1080);

            bool navVisible = _driver.FindElement(By.CssSelector(".navbar-nav")).Displayed;
            Assert.True(navVisible, "Navigation menu should be visible on desktop");
        }

        [Theory]
        [InlineData(typeof(ChromeDriver))]
        public void TestBrowserCompatibility(Type browserType)
        {
            _driver = (IWebDriver)Activator.CreateInstance(browserType);
            
            try
            {
                _driver.Navigate().GoToUrl(BaseUrl);
                Assert.Contains("SecondChance", _driver.Title);
                
                TestMobileView();
                TestDesktopView();
            }
            finally
            {
                _driver.Quit();
            }
        }

        [Fact]
        public void System_ShouldBeCompatibleWithOtherApplications()
        {
            var browsers = new List<(string Name, Func<IWebDriver> Create)>
            {
                ("Chrome", () => {
                    try {
                        var options = new ChromeOptions();
                        options.AddArgument("--headless");
                        options.AddArgument("--no-sandbox");
                        options.AddArgument("--disable-dev-shm-usage");
                        return new ChromeDriver(options);
                    } catch { return null; }
                }),

                ("Edge", () => {
                    try {
                        var options = new EdgeOptions();
                        options.AddArgument("--headless");
                        return new EdgeDriver(options);
                    } catch { return null; }
                })
            };

            var pagesToCheck = new[]
            {
                $"{BaseUrl}/Home/Index",
                $"{BaseUrl}/Products",
                $"{BaseUrl}/Identity/Account/Login"
            };

            int totalTests = 0;
            int passedTests = 0;

            foreach (var browser in browsers)
            {
                _driver = browser.Create();

                try
                {
                    foreach (var page in pagesToCheck)
                    {
                        totalTests++;

                        try
                        {
                            _driver.Navigate().GoToUrl(page);

                            if (_driver.Title != null && !_driver.PageSource.Contains("Server Error") &&
                                !_driver.PageSource.Contains("Runtime Error"))
                            {
                                passedTests++;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                finally
                {
                    _driver.Quit();
                    _driver = null;
                }
            }

            double compatibilityScore = (double)passedTests / totalTests * 100;
            Assert.True(compatibilityScore >= 90);
        }
    }
}