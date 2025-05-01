using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace TestProject1.Integration
{
    public class GoogleFacebookApiTests
    {
        private readonly string _googleClientId = "471247438309-fovn71s75jfrmghrh06mhi1g92mmnpv0.apps.googleusercontent.com";
        private readonly string _googleClientSecret = "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";
        private readonly string _facebookAppId = "1228769704894471";
        private readonly string _facebookAppSecret = "435ccee723c9109ea15cf9eaef8ddbc1";

        private readonly HttpClient _client = new HttpClient();

        [Fact]
        public async Task GoogleAPI_IsAccessible()
        {
            var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={_googleClientId}" +
                $"&redirect_uri=https://localhost/" +
                $"&response_type=code&scope=email";

            var response = await _client.GetAsync(authUrl);
            Assert.True((int)response.StatusCode < 500, 
                $"API do Google não está acessível: {response.StatusCode}");
        }
        [Fact]
        public async Task FacebookAPI_IsAccessible()
        {
            var authUrl = $"https://www.facebook.com/v15.0/dialog/oauth" +
                $"?client_id={_facebookAppId}" +
                $"&redirect_uri=https://localhost/" +
                $"&response_type=code";

            var response = await _client.GetAsync(authUrl);
            Assert.True((int)response.StatusCode < 500,
                $"API do Facebook não está acessível: {response.StatusCode}");
        }
    }
}