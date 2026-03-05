using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AspnetCoreStarter.Data;

var builder = WebApplication.CreateBuilder(args);

// Configurar a base de dados (MySQL)
var connectionString = builder.Configuration.GetConnectionString("UserContext");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

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
        // EnsureCreated() will create the DB if it doesn't exist.
        // If it exists, it won't add new tables. So we check if Schools exists.
        context.Database.EnsureCreated();
        
        // Manual check for Schools table as a fallback for slow migrations
        try {
            context.Database.ExecuteSqlRaw("SELECT 1 FROM Schools LIMIT 1;");
        } catch {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Schools (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(200) NOT NULL,
                    Address VARCHAR(500) NOT NULL,
                    ContactEmail VARCHAR(200) NOT NULL,
                    Phone VARCHAR(50) NULL,
                    RegisteredAt DATETIME DEFAULT CURRENT_TIMESTAMP
                ) ENGINE=InnoDB;
            ");
        }
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
