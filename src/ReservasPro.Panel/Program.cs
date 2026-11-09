using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ReservasPro.Panel;
using ReservasPro.Panel.Conexion;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Si el panel lo sirve la propia API, la API está en el mismo origen. Si se publica aparte
// (Static Web Apps), su URL llega en Api:Url desde wwwroot/appsettings.{entorno}.json.
var urlApi = builder.Configuration["Api:Url"] is { Length: > 0 } configurada ? configurada : builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(urlApi) });

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<SesionActual>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<SesionActual>());
builder.Services.AddScoped<ApiPanel>();

await builder.Build().RunAsync();
