using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ReservasPro.Api.Data;
using ReservasPro.Api.Endpoints;
using ReservasPro.Api.Errores;

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

var origenes = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origenes).AllowAnyHeader().WithMethods("GET", "POST")));

// Evita que un mismo cliente llene la agenda con reservas falsas
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(Program.LimiteReservas, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromHours(1) }));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // En desarrollo se crea el esquema y un negocio de ejemplo; en producción se usan migraciones
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ReservasDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DatosDeEjemplo.CargarAsync(db);
}

app.MapGet("/health", () => TypedResults.Ok(new { estado = "ok" })).ExcludeFromDescription();
app.MapReservasPublicas();

app.Run();

public partial class Program
{
    public const string LimiteReservas = "reservas";
}
