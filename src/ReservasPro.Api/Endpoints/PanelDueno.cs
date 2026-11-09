using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ReservasPro.Api.Auth;
using ReservasPro.Api.Data;
using ReservasPro.Contracts;
using ReservasPro.Domain;

namespace ReservasPro.Api.Endpoints;

/// <summary>
/// Endpoints del panel. Todos exigen un JWT y trabajan solo sobre el negocio del token,
/// así un dueño nunca puede ver ni tocar datos de otro negocio cambiando un id en la URL.
/// </summary>
public static class PanelDueno
{
    public static void MapPanelDueno(this IEndpointRouteBuilder app)
    {
        var panel = app.MapGroup("/api/panel").WithTags("Panel del dueño").RequireAuthorization();

        panel.MapGet("/citas", CitasDelDia);
        panel.MapPost("/citas/{id:int}/cancelar", CancelarCita);

        panel.MapGet("/servicios", Servicios);
        panel.MapPost("/servicios", CrearServicio);
        panel.MapPost("/servicios/{id:int}/desactivar", DesactivarServicio);

        panel.MapGet("/horarios", Horarios);
        panel.MapPost("/horarios", CrearHorario);
        panel.MapDelete("/horarios/{id:int}", QuitarHorario);
    }

    private static async Task<Ok<List<CitaDelDia>>> CitasDelDia(DateOnly fecha, ClaimsPrincipal yo, ReservasDbContext db, CancellationToken ct)
    {
        var negocioId = yo.NegocioId();
        var citas = await db.Citas
            .Where(c => c.NegocioId == negocioId && c.Fecha == fecha)
            .OrderBy(c => c.Inicio)
            .Select(c => new CitaDelDia(c.Id, c.Servicio!.Nombre, c.ClienteNombre, c.ClienteTelefono,
                c.Inicio, c.Fin, c.Estado == EstadoCita.Cancelada))
            .ToListAsync(ct);
        return TypedResults.Ok(citas);
    }

    private static async Task<Results<NoContent, NotFound>> CancelarCita(int id, ClaimsPrincipal yo, ReservasDbContext db, CancellationToken ct)
    {
        var negocioId = yo.NegocioId();
        var cita = await db.Citas.SingleOrDefaultAsync(c => c.Id == id && c.NegocioId == negocioId, ct);
        if (cita is null)
            return TypedResults.NotFound();

        cita.Cancelar();
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<List<ServicioAdmin>>> Servicios(ClaimsPrincipal yo, ReservasDbContext db, CancellationToken ct)
    {
        var negocioId = yo.NegocioId();
        var servicios = await db.Servicios
            .Where(s => s.NegocioId == negocioId)
            .OrderByDescending(s => s.Activo).ThenBy(s => s.Nombre)
            .Select(s => new ServicioAdmin(s.Id, s.Nombre, s.DuracionMinutos, s.Precio, s.Activo))
            .ToListAsync(ct);
        return TypedResults.Ok(servicios);
    }

    private static async Task<Created<ServicioAdmin>> CrearServicio(NuevoServicio pedido, ClaimsPrincipal yo, ReservasDbContext db, CancellationToken ct)
    {
        var negocio = await MiNegocio(yo, db, ct);
        var servicio = negocio.AgregarServicio(pedido.Nombre, pedido.DuracionMinutos, pedido.Precio);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/panel/servicios/{servicio.Id}",
            new ServicioAdmin(servicio.Id, servicio.Nombre, servicio.DuracionMinutos, servicio.Precio, servicio.Activo));
    }

    private static async Task<Results<NoContent, NotFound>> DesactivarServicio(int id, ClaimsPrincipal yo, ReservasDbContext db, CancellationToken ct)
    {
        var negocioId = yo.NegocioId();
        var servicio = await db.Servicios.SingleOrDefaultAsync(s => s.Id == id && s.NegocioId == negocioId, ct);
        if (servicio is null)
            return TypedResults.NotFound();

        servicio.Desactivar();
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<List<HorarioAdmin>>> Horarios(ClaimsPrincipal yo, ReservasDbContext db, CancellationToken ct)
    {
        var negocioId = yo.NegocioId();
        var horarios = await db.Horarios
            .Where(h => h.NegocioId == negocioId)
            .OrderBy(h => h.Dia).ThenBy(h => h.Apertura)
            .Select(h => new HorarioAdmin(h.Id, h.Dia, h.Apertura, h.Cierre))
            .ToListAsync(ct);
        return TypedResults.Ok(horarios);
    }

    private static async Task<Created<HorarioAdmin>> CrearHorario(NuevoHorario pedido, ClaimsPrincipal yo, ReservasDbContext db, CancellationToken ct)
    {
        var negocio = await MiNegocio(yo, db, ct);
        var horario = negocio.AgregarHorario(pedido.Dia, pedido.Apertura, pedido.Cierre);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/panel/horarios/{horario.Id}",
            new HorarioAdmin(horario.Id, horario.Dia, horario.Apertura, horario.Cierre));
    }

    private static async Task<Results<NoContent, NotFound>> QuitarHorario(int id, ClaimsPrincipal yo, ReservasDbContext db, CancellationToken ct)
    {
        var negocio = await MiNegocio(yo, db, ct);
        if (negocio.Horarios.All(h => h.Id != id))
            return TypedResults.NotFound();

        negocio.QuitarHorario(id);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Negocio> MiNegocio(ClaimsPrincipal yo, ReservasDbContext db, CancellationToken ct)
    {
        var negocioId = yo.NegocioId();
        return await db.Negocios
            .Include(n => n.Servicios)
            .Include(n => n.Horarios)
            .AsSplitQuery()
            .SingleAsync(n => n.Id == negocioId, ct);
    }
}
