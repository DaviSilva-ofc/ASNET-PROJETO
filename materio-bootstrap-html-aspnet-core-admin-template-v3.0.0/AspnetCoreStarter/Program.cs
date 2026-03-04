using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
// using AspnetCoreStarter.Data;
// using AspnetCoreStarter.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Add services to the container.
// During development we enable runtime compilation so changes to .cshtml files
// are picked up without restarting dotnet. You'll need to run
//    dotnet add package Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
// once to install the package.
#if DEBUG
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
#else
builder.Services.AddRazorPages();
#endif

// builder.Services.AddDbContext<UserContext>(options =>
// {
//   options.UseSqlite(builder.Configuration.GetConnectionString("UserContext") ?? throw new InvalidOperationException("Connection string 'UserContext' not found."));
// }, ServiceLifetime.Scoped);

var app = builder.Build();

// using (var scope = app.Services.CreateScope())
// {
//   var services = scope.ServiceProvider;

//   SeedData.Initialize(services);
// }

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();



// Redirect root URL to Landing Page
app.MapGet("/", context =>
{
  context.Response.Redirect("/frontpages/LandingPage");
  return Task.CompletedTask;
});

app.MapRazorPages();


app.Run();
