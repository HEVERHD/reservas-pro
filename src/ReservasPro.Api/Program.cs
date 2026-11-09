using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReservasPro.Api.Auth;
using ReservasPro.Api.Data;
using ReservasPro.Api.Endpoints;
using ReservasPro.Api.Errores;
using ReservasPro.Domain;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión nunca va en el repositorio: user-secrets en desarrollo,
// variable de entorno ConnectionStrings__Reservas (o Key Vault) en producción.
var conexion = builder.Configuration.GetConnectionString("Reservas")
    ?? throw new InvalidOperationException(
        "Falta ConnectionStrings:Reservas. En desarrollo: dotnet user-secrets set \"ConnectionStrings:Reservas\" \"Host=…\"");

builder.Services.AddDbContext<ReservasDbContext>(o => o.UseNpgsql(conexion));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorDeErrores>();

// Autenticación del panel con JWT firmados por esta misma API
var jwt = builder.Configuration.GetSection(JwtOpciones.Seccion).Get<JwtOpciones>() ?? new();
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<EmisorDeTokens>();
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // conserva los nombres cortos (sub, email, negocio_id)
        o.TokenValidationParameters = new()
        {
            ValidIssuer = jwt.Emisor,
            ValidAudience = jwt.Audiencia,
            IssuerSigningKey = jwt.ClaveDeFirma(),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

var origenes = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origenes).AllowAnyHeader().WithMethods("GET", "POST", "DELETE")));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Evita que un mismo cliente llene la agenda con reservas falsas
    o.AddPolicy(Program.LimiteReservas, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromHours(1) }));
    // Frena los intentos de adivinar contraseñas
    o.AddPolicy(Program.LimiteLogin, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();
else
    app.UseHsts(); // Azure termina el TLS; esto le dice al navegador que use siempre HTTPS

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReservasDbContext>();

    // Con una sola instancia basta aplicar las migraciones al arrancar; con varias,
    // conviene aplicarlas desde el pipeline de despliegue antes de publicar.
    if (db.Database.GetMigrations().Any())
        await db.Database.MigrateAsync();
    else if (app.Environment.IsDevelopment())
        await db.Database.EnsureCreatedAsync(); // solo hasta que exista la migración inicial
    else
        throw new InvalidOperationException("No hay migraciones. Crea la inicial con: dotnet ef migrations add Inicial");

    if (app.Environment.IsDevelopment())
        await DatosDeEjemplo.CargarAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>(), app.Logger);
}

app.MapGet("/health", () => TypedResults.Ok(new { estado = "ok" })).ExcludeFromDescription();
app.MapReservasPublicas();
app.MapAutenticacion();
app.MapPanelDueno();

// La API también sirve el panel de Blazor: una sola URL para la API y el panel,
// sin CORS de por medio. Las rutas que no son de la API caen en index.html.
app.UseBlazorFrameworkFiles();
app.MapStaticAssets();
app.MapFallback("/api/{**resto}", () => TypedResults.NotFound()).ExcludeFromDescription();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program
{
    public const string LimiteReservas = "reservas";
    public const string LimiteLogin = "login";
}
