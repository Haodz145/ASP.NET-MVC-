using Microsoft.AspNetCore.Authentication.Cookies;
using SV22T1020105.Shop;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews()
                .AddMvcOptions(opt =>
                {
                    opt.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                });

// Cookie Authentication cho khách hàng
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(opt =>
                {
                    opt.Cookie.Name = "LiteCommerce.Shop";
                    opt.LoginPath = "/Account/Login";
                    opt.AccessDeniedPath = "/Account/Login";
                    opt.ExpireTimeSpan = TimeSpan.FromDays(7);
                    opt.SlidingExpiration = true;
                    opt.Cookie.HttpOnly = true;
                    opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                });

// Session
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromHours(2);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Culture
var cultureInfo = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// ApplicationContext
ApplicationContext.Configure(
    httpContextAccessor: app.Services.GetRequiredService<IHttpContextAccessor>(),
    webHostEnvironment: app.Services.GetRequiredService<IWebHostEnvironment>(),
    configuration: app.Configuration
);

// BusinessLayers connection string
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("ConnectionString 'LiteCommerceDB' not found.");
SV22T1020105.BusinessLayers.Configuration.Initialize(connectionString);

app.Run();
