using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReservasPro.Domain;

namespace ReservasPro.Api.Data;

/// <summary>Crea un negocio de demostración en desarrollo para poder probar la API sin configurar nada.</summary>
public static class DatosDeEjemplo
{
    public const string EmailDueno = "dueno@barberia-centro.test";

    public static async Task CargarAsync(ReservasDbContext db, IPasswordHasher<Usuario> hasher, ILogger logger)
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

        // La contraseña del dueño de ejemplo se genera al azar en cada base nueva y solo se muestra
        // en la consola de desarrollo: así no hay ninguna credencial guardada en el repositorio.
        var clave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(12));
        var dueno = new Usuario(barberia, EmailDueno);
        dueno.CambiarHashClave(hasher.HashPassword(dueno, clave));
        db.Usuarios.Add(dueno);
        await db.SaveChangesAsync();

        logger.LogWarning("Usuario de ejemplo del panel: {Email} / {Clave} (solo desarrollo)", EmailDueno, clave);
    }
}
