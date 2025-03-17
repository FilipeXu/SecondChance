using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecondChance.Data;
using SecondChance.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using SecondChance.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
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

// Add email sender service
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Register IProductRepository
builder.Services.AddScoped<IProductRepository, ProductRepository>();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
