using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ReservasPro.Panel;
using ReservasPro.Panel.Conexion;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// La URL de la API sale de wwwroot/appsettings.json (o appsettings.{entorno}.json)
var urlApi = builder.Configuration["Api:Url"] ?? throw new InvalidOperationException("Falta Api:Url en wwwroot/appsettings.json.");
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(urlApi) });

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<SesionActual>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<SesionActual>());
builder.Services.AddScoped<ApiPanel>();

await builder.Build().RunAsync();
