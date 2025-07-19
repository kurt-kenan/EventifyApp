using Eventify.DataAccess.Data;
using Eventify.Mvc.Middlewares;
using Eventify.Mvc.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. MVC
builder.Services.AddControllersWithViews();

// 2. DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient("Eventify.WebAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:44315/"); // API adresi
});


// 4. Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 5. Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";     // Giri� yap�lmad�ysa y�nlendirilecek yer
        options.LogoutPath = "/Auth/Logout";   // ��k�� y�nlendirmesi
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 6. Error Handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 7. Middleware Pipelines
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Session'� aktif et
app.UseMiddleware<JwtSessionMiddleware>(); // Session i�indeki token'� HTTP header'a ekle

app.UseAuthentication();
app.UseAuthorization();

// 8. Default Routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
