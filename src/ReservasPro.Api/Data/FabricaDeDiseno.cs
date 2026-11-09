using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReservasPro.Api.Data;

/// <summary>
/// La usa <c>dotnet ef</c> para crear migraciones sin arrancar la API, así no hacen falta
/// la cadena de conexión real ni la clave de los JWT. Generar una migración no se conecta a la base.
/// </summary>
public sealed class FabricaDeDiseno : IDesignTimeDbContextFactory<ReservasDbContext>
{
    public ReservasDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ReservasDbContext>()
            .UseNpgsql("Host=localhost;Database=reservas_diseno")
            .Options);
}
