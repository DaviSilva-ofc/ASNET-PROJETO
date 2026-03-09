using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Services;
using AspnetCoreStarter.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Configurar a base de dados (MySQL)
var connectionString = builder.Configuration.GetConnectionString("UserContext");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// CONFIGURAR E-MAIL
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// ATIVAR SESSÕES E AUTENTICAÇÃO
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // Duração máxima se "Lembrar-me" for usado
        options.SlidingExpiration = true;
    });

// Razor Pages
#if DEBUG
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
#else
builder.Services.AddRazorPages();
#endif

var app = builder.Build();

// GARANTIR QUE A BASE DE DADOS E TABELAS EXISTEM
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocorreu um erro ao criar a base de dados ou tabelas.");
    }
}

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// USAR SESSÃO (IMPORTANTE: antes de Authentication)
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Redirecionar root para LandingPage
app.MapGet("/", context =>
{
    context.Response.Redirect("/frontpages/LandingPage");
    return Task.CompletedTask;
});

app.MapRazorPages();

app.Run();
