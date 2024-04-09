using Blog.Components;

var builder = WebApplication.CreateBuilder(args);

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