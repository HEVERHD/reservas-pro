using Microsoft.EntityFrameworkCore;
using ReservasPro.Domain;

namespace ReservasPro.Api.Data;

/// <summary>Crea un negocio de demostración en desarrollo para poder probar la API sin configurar nada.</summary>
public static class DatosDeEjemplo
{
    public static async Task CargarAsync(ReservasDbContext db)
    {
        if (await db.Negocios.AnyAsync())
            return;

        var barberia = new Negocio("Barbería Centro", "barberia-centro", "573000000000");
        foreach (var dia in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            barberia.AgregarHorario(dia, new(9, 0), new(12, 30));
            barberia.AgregarHorario(dia, new(14, 0), new(19, 0));
        }
        barberia.AgregarHorario(DayOfWeek.Saturday, new(9, 0), new(14, 0));
        barberia.AgregarServicio("Corte clásico", 30, 25_000m);
        barberia.AgregarServicio("Corte y barba", 45, 35_000m);
        barberia.AgregarServicio("Afeitado con toalla caliente", 30, 20_000m);

        db.Negocios.Add(barberia);
        await db.SaveChangesAsync();
    }
}
