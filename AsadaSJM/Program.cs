
using AsadaSJM.Data;
using AsadaSJM.Services.Firebase;
using AsadaSJM.Services.FotoGaleria;
using AsadaSJM.Services.Galeria;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var credentialPath = Path.Combine(builder.Environment.ContentRootPath, "Credentials", "asada-sjm-firebase-adminsdk-fbsvc-257e7b9b86.json");
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);

// =========================================================
// BASE DE DATOS
// =========================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));

// =========================================================
// AUTENTICACIÓN POR COOKIES
// =========================================================

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";

        options.AccessDeniedPath = "/Account/AccesoDenegado";

        options.Cookie.Name = "AsadaSJM.Auth";

        options.Cookie.HttpOnly = true;

        options.ExpireTimeSpan =
            TimeSpan.FromHours(8);

        options.SlidingExpiration = true;
    });

// =========================================================
// AUTORIZACIÓN
// =========================================================

builder.Services.AddAuthorization();

// =========================================================
// MVC
// =========================================================

// Services
builder.Services.AddSingleton<IFirebaseStorageService, FirebaseStorageService>();
builder.Services.AddScoped<IGaleriaService, GaleriaService>();
builder.Services.AddScoped<IFotoGaleriaService, FotoGaleriaService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// =========================================================
// CONFIGURACIÓN
// =========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// IMPORTANTE: primero Authentication
app.UseAuthentication();

app.UseAuthorization();

// =========================================================
// RUTA PRINCIPAL
// =========================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Portal}/{action=Index}/{id?}");

app.Run();
