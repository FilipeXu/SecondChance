using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TestProject1
{
    public class PerformanceTests : IClassFixture<TestDatabaseFixture>
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly HttpClient _client = new HttpClient();
        private const string BaseUrl = "https://localhost:7052";
        private bool _isAppRunning = false;

        public PerformanceTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            
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

        [Fact]
        public async Task HomePage_LoadTest_ShouldHandleMultipleRequests()
        {
            var tasks = new List<Task>();
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < 100; i++)
            {
                tasks.Add(_client.GetAsync($"{BaseUrl}/Products/Index"));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds / 100.0 < 2000);
        }

        [Fact]
        public async Task Products_StressTest_ShouldHandleRepeatedRequests()
        {
            var stopwatch = Stopwatch.StartNew();
            var successfulRequests = 0;

            for (int i = 0; i < 1000; i++)
            {
                try
                {
                    var response = await _client.GetAsync($"{BaseUrl}/Products/Index");
                    if (response.IsSuccessStatusCode)
                        successfulRequests++;
                }
                catch { }
            }

            stopwatch.Stop();
            
            var successRate = (double)successfulRequests / 1000;
            Assert.True(successRate > 0.95);
        }

        [Fact]
        public async Task HomePage_LoadTime_ShouldBeLessThan5Seconds()
        {

            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync($"{BaseUrl}/Products/Index");
            stopwatch.Stop();

            long loadTimeMs = stopwatch.ElapsedMilliseconds;

            Assert.True(loadTimeMs < 5000);
            Assert.True(response.IsSuccessStatusCode);
        }
    }
}