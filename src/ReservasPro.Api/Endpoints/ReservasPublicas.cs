using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ReservasPro.Api.Data;
using ReservasPro.Domain;

namespace ReservasPro.Api.Endpoints;

/// <summary>Lo que usa la página pública de reservas: ver el negocio, consultar turnos y reservar.</summary>
public static class ReservasPublicas
{
    public static RouteGroupBuilder MapReservasPublicas(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/negocios/{slug}").WithTags("Reservas");
        grupo.MapGet("/", ObtenerNegocio).WithName("ObtenerNegocio");
        grupo.MapGet("/disponibilidad", ConsultarDisponibilidad).WithName("ConsultarDisponibilidad");
        grupo.MapPost("/citas", Reservar).WithName("Reservar").RequireRateLimiting(Program.LimiteReservas);
        return grupo;
    }

    public sealed record ServicioDto(int Id, string Nombre, int DuracionMinutos, decimal Precio);
    public sealed record HorarioDto(DayOfWeek Dia, TimeOnly Apertura, TimeOnly Cierre);
    public sealed record NegocioDto(string Nombre, string Slug, string ZonaHoraria, IEnumerable<ServicioDto> Servicios, IEnumerable<HorarioDto> Horarios);
    public sealed record DisponibilidadDto(DateOnly Fecha, int ServicioId, IReadOnlyList<TimeOnly> Turnos);

    public sealed record NuevaCita(
        [Range(1, int.MaxValue)] int ServicioId,
        DateOnly Fecha,
        TimeOnly Inicio,
        [Required, StringLength(120, MinimumLength = 2)] string ClienteNombre,
        [Required, StringLength(20, MinimumLength = 7)] string ClienteTelefono);

    public sealed record CitaCreadaDto(int Id, string Servicio, DateOnly Fecha, TimeOnly Inicio, TimeOnly Fin, string EnlaceWhatsApp);

    private static async Task<Results<Ok<NegocioDto>, NotFound>> ObtenerNegocio(string slug, ReservasDbContext db, CancellationToken ct)
    {
        var negocio = await BuscarNegocio(db, slug, ct);
        if (negocio is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(new NegocioDto(
            negocio.Nombre,
            negocio.Slug,
            negocio.ZonaHoraria,
            negocio.Servicios.Where(s => s.Activo).OrderBy(s => s.Nombre)
                .Select(s => new ServicioDto(s.Id, s.Nombre, s.DuracionMinutos, s.Precio)),
            negocio.Horarios.OrderBy(h => h.Dia).ThenBy(h => h.Apertura)
                .Select(h => new HorarioDto(h.Dia, h.Apertura, h.Cierre))));
    }

    private static async Task<Results<Ok<DisponibilidadDto>, NotFound>> ConsultarDisponibilidad(
        string slug, int servicioId, DateOnly fecha, ReservasDbContext db, TimeProvider reloj, CancellationToken ct)
    {
        var negocio = await BuscarNegocio(db, slug, ct);
        var servicio = negocio?.Servicios.SingleOrDefault(s => s.Id == servicioId);
        if (negocio is null || servicio is null)
            return TypedResults.NotFound();

        var citas = await CitasDelDia(db, negocio.Id, fecha, ct);
        var turnos = Agenda.TurnosLibres(negocio, servicio, fecha, citas, reloj.GetUtcNow());
        return TypedResults.Ok(new DisponibilidadDto(fecha, servicioId, turnos));
    }

    private static async Task<Results<Created<CitaCreadaDto>, NotFound>> Reservar(
        string slug, NuevaCita pedido, ReservasDbContext db, TimeProvider reloj, CancellationToken ct)
    {
        // Serializable: si dos personas piden el mismo turno a la vez, PostgreSQL rechaza una de las dos
        // transacciones y el manejador de errores responde 409 en lugar de crear dos citas cruzadas.
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var negocio = await BuscarNegocio(db, slug, ct);
        var servicio = negocio?.Servicios.SingleOrDefault(s => s.Id == pedido.ServicioId);
        if (negocio is null || servicio is null)
            return TypedResults.NotFound();

        var citas = await CitasDelDia(db, negocio.Id, pedido.Fecha, ct);
        var cita = Agenda.Reservar(negocio, servicio, pedido.Fecha, pedido.Inicio,
            pedido.ClienteNombre, pedido.ClienteTelefono, citas, reloj.GetUtcNow());

        db.Citas.Add(cita);
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

        var dto = new CitaCreadaDto(cita.Id, servicio.Nombre, cita.Fecha, cita.Inicio, cita.Fin,
            WhatsApp.EnlaceConfirmacion(negocio, servicio, cita));
        return TypedResults.Created($"/api/negocios/{slug}/citas/{cita.Id}", dto);
    }

    private static Task<Negocio?> BuscarNegocio(ReservasDbContext db, string slug, CancellationToken ct) =>
        db.Negocios
            .Include(n => n.Servicios)
            .Include(n => n.Horarios)
            .AsSplitQuery()
            .SingleOrDefaultAsync(n => n.Slug == slug, ct);

    private static Task<List<Cita>> CitasDelDia(ReservasDbContext db, int negocioId, DateOnly fecha, CancellationToken ct) =>
        db.Citas
            .Where(c => c.NegocioId == negocioId && c.Fecha == fecha && c.Estado == EstadoCita.Confirmada)
            .AsNoTracking()
            .ToListAsync(ct);
}
