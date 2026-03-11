using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AspnetCoreStarter.Data;
using AspnetCoreStarter.Services;
using AspnetCoreStarter.Models;

var builder = WebApplication.CreateBuilder(args);

// Configurar a base de dados (MySQL)
var connectionString = builder.Configuration.GetConnectionString("UserContext");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 📧 CONFIGURAR E-MAIL
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// 🔐 ATIVAR SESSÕES
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // sessão expira em 30 minutos
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Razor Pages
#if DEBUG
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
#else
builder.Services.AddRazorPages();
#endif

var app = builder.Build();

// 🛠️ GARANTIR QUE A BASE DE DADOS E TABELAS EXISTEM
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        Console.WriteLine("Tentando aplicar migrações na base de dados...");
        context.Database.Migrate();
        Console.WriteLine("Base de dados atualizada com sucesso.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocorreu um erro ao criar a base de dados ou tabelas.");
        Console.WriteLine($"ERRO CRÍTICO NA BASE DE DADOS: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
        }
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

// 🔐 USAR SESSÃO (IMPORTANTE: antes de Authorization)
app.UseSession();

app.UseAuthorization();

// Redirecionar root para LandingPage
app.MapGet("/", context =>
{
    context.Response.Redirect("/frontpages/LandingPage");
    return Task.CompletedTask;
});

app.MapRazorPages();

app.Run();
