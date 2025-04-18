using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecondChance.Data;
using SecondChance.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using SecondChance.Services;
using SecondChance.Hubs;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AzureConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<User, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequireNonAlphanumeric = false;
}
                              )
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.AddSignalR();

builder.Services.AddRazorPages();

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = "471247438309-fovn71s75jfrmghrh06mhi1g92mmnpv0.apps.googleusercontent.com";
        options.ClientSecret = "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";
    })
     .AddFacebook(options =>
     {
         options.ClientId = "1228769704894471";
         options.ClientSecret = "435ccee723c9109ea15cf9eaef8ddbc1";
     });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapHub<ChatHub>("/chatHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
