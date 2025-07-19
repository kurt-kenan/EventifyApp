using Eventify.DataAccess.Data;
using Eventify.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 💾 DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔐 JWT servisi
builder.Services.AddSingleton(new JwtService("super-secret-key-you-should-store-in-config", 60));

var key = Encoding.UTF8.GetBytes("super-secret-key-you-should-store-in-config");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddSingleton(new EmailSender("kurtkenan236@gmail.com", "dwby rbxh tsmp lvlu"));

// 🌐 Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🧪 Dev ortamı için Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 📡 Middleware sırası
app.UseHttpsRedirection();
app.UseStaticFiles(); // Static files için gerekli

app.UseRouting(); // ⭐️ Eklendi: Route eşleşmeleri için şart!

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
