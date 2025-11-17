using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PickForge.Client;
using PickForge.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Read from configuration (already loaded from wwwroot/appsettings*.json)
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7004/";

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBase) });

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ScoreboardService>();

await builder.Build().RunAsync();
