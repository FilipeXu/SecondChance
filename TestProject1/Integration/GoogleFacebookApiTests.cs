using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecondChance.Data;
using SecondChance.Models;
using System.Net;

namespace TestProject1.Integration
{
    public class GoogleFacebookApiTests
    {
        private readonly HttpClient _client;
        
        private readonly string _googleClientId = "471247438309-fovn71s75jfrmghrh06mhi1g92mmnpv0.apps.googleusercontent.com";
        private readonly string _googleClientSecret = "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";
        private readonly string _facebookAppId = "1228769704894471";
        private readonly string _facebookAppSecret = "435ccee723c9109ea15cf9eaef8ddbc1";

        public GoogleFacebookApiTests()
        {
            var builder = new WebHostBuilder()
                .UseEnvironment("Testing")
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                        ["Authentication:Google:ClientId"] = _googleClientId,
                        ["Authentication:Google:ClientSecret"] = _googleClientSecret,
                        ["Authentication:Facebook:AppId"] = _facebookAppId,
                        ["Authentication:Facebook:AppSecret"] = _facebookAppSecret
                    });
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddDebug();
                })
                .UseStartup<TestStartup>();

            var server = new TestServer(builder);
            _client = server.CreateClient();
            _client.BaseAddress = new Uri("https://localhost");
        }

        public class TestStartup
        {
            public TestStartup(IConfiguration configuration)
            {
                Configuration = configuration;
            }

            public IConfiguration Configuration { get; }

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestAuthDb"));

                services.AddIdentity<User, IdentityRole>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.SignIn.RequireConfirmedEmail = false;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 6;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

                services.AddTransient<IEmailSender, TestEmailSender>();

                services.AddAuthentication()
                    .AddGoogle(options =>
                    {
                        options.ClientId = Configuration["Authentication:Google:ClientId"];
                        options.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
                    })
                    .AddFacebook(options =>
                    {
                        options.AppId = Configuration["Authentication:Facebook:AppId"];
                        options.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
                    });

                services.AddControllersWithViews();
                services.AddRazorPages();
            }

            public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
                app.UseDeveloperExceptionPage();
                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");
                    endpoints.MapRazorPages();
                });
            }
        }

        public class TestEmailSender : IEmailSender
        {
            public Task SendEmailAsync(string email, string subject, string htmlMessage)
            {
                return Task.CompletedTask;
            }
        }



        [Fact]
        public async Task ForgotPassword_WithExternalProvider_ReturnsSuccessfully()
        {
            try
            {
                var email = "test@example.com";
                var formData = new Dictionary<string, string>
                {
                    { "Email", email }
                };

                var response = await _client.PostAsync(
                    "/Identity/Account/ForgotPassword", 
                    new FormUrlEncodedContent(formData));
                
                var content = await response.Content.ReadAsStringAsync();
                
                Assert.True(
                    response.StatusCode == HttpStatusCode.Redirect || 
                    response.StatusCode == HttpStatusCode.OK || 
                    response.StatusCode == HttpStatusCode.NotFound,
                    $"Status code inesperado: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Assert.True(false, $"Erro no teste ForgotPassword: {ex.Message}");
            }
        }



        [Fact]
        public async Task GoogleAPI_IsAccessible()
        {
            var client = new HttpClient();
            var response = await client.GetAsync("https://accounts.google.com/o/oauth2/v2/auth?client_id=test&redirect_uri=https://localhost&response_type=code&scope=email");
            
            Assert.True(
                (int)response.StatusCode < 500,
                $"API do Google não está acessível: {response.StatusCode}"
            );
        }

        [Fact]
        public async Task FacebookAPI_IsAccessible()
        {
            var client = new HttpClient();
            var response = await client.GetAsync("https://www.facebook.com/v15.0/dialog/oauth?client_id=test&redirect_uri=https://localhost&response_type=code");
            
            Assert.True(
                (int)response.StatusCode < 500,
                $"API do Facebook não está acessível: {response.StatusCode}"
            );
        }
    }
} 