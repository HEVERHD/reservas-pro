using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReservasPro.Api.Data;

/// <summary>
/// La usa <c>dotnet ef</c> sin arrancar la API, así no hace falta la clave de los JWT.
/// Toma la conexión de la variable ConnectionStrings__Reservas cuando existe (el dev container la define),
/// para que comandos como <c>database update</c> o <c>database drop</c> actúen sobre la base real.
/// Crear una migración no se conecta a la base, así que en CI basta con una conexión ficticia.
/// </summary>
public sealed class FabricaDeDiseno : IDesignTimeDbContextFactory<ReservasDbContext>
{
    public ReservasDbContext CreateDbContext(string[] args)
    {
        var conexion = Environment.GetEnvironmentVariable("ConnectionStrings__Reservas")
            ?? "Host=localhost;Database=reservas_diseno";
        return new(new DbContextOptionsBuilder<ReservasDbContext>().UseNpgsql(conexion).Options);
    }
}
