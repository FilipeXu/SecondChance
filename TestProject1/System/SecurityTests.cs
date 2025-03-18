namespace TestProject1
{
    public class SecurityTests : IClassFixture<TestDatabaseFixture>
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly HttpClient _client;
        private const string BaseUrl = "https://localhost:7052";
        private bool _isAppRunning = false;

        public SecurityTests(TestDatabaseFixture fixture)
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

        [Fact]
        public async Task Security_PreventsSQLInjection()
        {
            var maliciousInputs = new[]
            {
                "' OR '1'='1",
                "; DROP TABLE Users--",
                "' UNION SELECT * FROM Users--"
            };

            foreach (var input in maliciousInputs)
            {
                var response = await _client.GetAsync($"{BaseUrl}/Products/Index?search={input}");
                Assert.True(response.IsSuccessStatusCode);
                var content = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("error", content.ToLower());
            }
        }

        [Fact]
        public async Task Security_PreventsXSS()
        {
            var xssPayload = "<script>alert('xss')</script>";
            
            var response = await _client.GetAsync($"{BaseUrl}/Products/Index?search={Uri.EscapeDataString(xssPayload)}");
            var content = await response.Content.ReadAsStringAsync();
            
            Assert.DoesNotContain(xssPayload, content);
            
            if (content.Contains("search") && content.Contains("xss"))
            {
                Assert.DoesNotContain("<script>", content);
            }
        }


        [Fact]
        public async Task Security_PreventsCrossSiteRequestForgery()
        {
            var loginResponse = await _client.GetAsync($"{BaseUrl}/Identity/Account/Login");
            var content = await loginResponse.Content.ReadAsStringAsync();
            
            Assert.Contains("__RequestVerificationToken", content);
            
            var formData = new Dictionary<string, string>
            {
                { "Email", "test@example.com" },
                { "Password", "Test@123" }
            };

            var postResponse = await _client.PostAsync($"{BaseUrl}/Identity/Account/Login", 
                new FormUrlEncodedContent(formData));
            
            Assert.False(postResponse.IsSuccessStatusCode);
        }
    }
}