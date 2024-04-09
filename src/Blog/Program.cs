var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureOptions<FilePostServiceOptionsSetup>();
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddScoped<IPostService, FilePostService>();

builder.Services
  .AddRazorComponents()
  .AddInteractiveServerComponents();

var app = builder.Build();

if (app.Environment.IsProduction())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app
  .MapRazorComponents<App>()
  .AddInteractiveServerRenderMode();

app.Run();

public partial class Program {}