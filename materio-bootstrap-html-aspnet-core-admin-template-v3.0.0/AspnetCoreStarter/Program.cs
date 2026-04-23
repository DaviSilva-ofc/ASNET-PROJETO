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
builder.Services.AddTransient<IStockService, StockService>();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// ATIVAR SESSÕES E AUTENTICAÇÃO
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    })
    .AddCookie("External") // Esquema temporário para logins externos
    .AddGoogle(options =>
    {
        options.SignInScheme = "External";
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        // Forçar a exibição do seletor de contas
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
            return Task.CompletedTask;
        };
    })
    .AddMicrosoftAccount(options =>
    {
        options.SignInScheme = "External";
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? "";
        // Forçar a exibição do seletor de contas
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
            return Task.CompletedTask;
        };
    });

// Razor Pages
#if DEBUG
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
#else
builder.Services.AddRazorPages();
#endif

// SIGNALR
builder.Services.AddSignalR();

var app = builder.Build();

// GARANTIR QUE A BASE DE DADOS E TABELAS EXISTEM
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        Console.WriteLine("Tentando aplicar migrações na base de dados...");
        
        // Manual column addition because of build locks on migrations
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN id_solicitante INT NULL;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD CONSTRAINT FK_tickets_utilizadores_id_solicitante FOREIGN KEY (id_solicitante) REFERENCES utilizadores(id_utilizador);"); } catch { }
        
        // StockEmpresa updates for Professors
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_empresa ADD COLUMN id_professor INT NULL;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_empresa ADD CONSTRAINT FK_stock_empresa_professores_id_professor FOREIGN KEY (id_professor) REFERENCES utilizadores(id_utilizador);"); } catch { }
        
        // StockEmpresa updates for Directors
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_empresa ADD COLUMN id_diretor INT NULL;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_empresa ADD CONSTRAINT FK_stock_empresa_diretores_id_diretor FOREIGN KEY (id_diretor) REFERENCES utilizadores(id_utilizador);"); } catch { }

        // StockEmpresa updates for Tickets
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_empresa ADD COLUMN id_ticket INT NULL;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE stock_empresa ADD CONSTRAINT FK_stock_empresa_tickets_id_ticket FOREIGN KEY (id_ticket) REFERENCES tickets(id_ticket);"); } catch { }

        // Equipamento updates for Tickets
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE equipamentos ADD COLUMN id_ticket INT NULL;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE equipamentos ADD CONSTRAINT FK_equipamentos_tickets_id_ticket FOREIGN KEY (id_ticket) REFERENCES tickets(id_ticket);"); } catch { }

        // CSAT columns for Tickets
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN satisfacao_rating INT NULL;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN satisfacao_feedback TEXT NULL;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE tickets ADD COLUMN data_avaliacao DATETIME NULL;"); } catch { }

        // ── SOFT DELETE COLUMNS ──────────────────────────────────────────────────
        // Ensure is_deleted column exists on all soft-deletable tables.
        // ALTER TABLE ignores silently if column already exists.
        var softDeleteTables = new[]
        {
            "utilizadores", "escolas", "agrupamentos", "blocos", "salas",
            "equipamentos", "tickets", "stock_empresa", "empresas",
            "contratos", "emprestimos", "reparos", "pedidos_stock"
        };
        foreach (var table in softDeleteTables)
        {
            try { await context.Database.ExecuteSqlRawAsync($"ALTER TABLE {table} ADD COLUMN is_deleted TINYINT(1) NOT NULL DEFAULT 0;"); } catch { }
        }

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

// MAP SIGNALR HUB
app.MapHub<AspnetCoreStarter.Hubs.ChatHub>("/chatHub");

app.Run();
