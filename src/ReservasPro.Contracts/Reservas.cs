using System.ComponentModel.DataAnnotations;

namespace ReservasPro.Contracts;

// Lo que ve la página pública de reservas.

public sealed record ServicioDto(int Id, string Nombre, int DuracionMinutos, decimal Precio);

public sealed record HorarioDto(DayOfWeek Dia, TimeOnly Apertura, TimeOnly Cierre);

public sealed record NegocioDto(string Nombre, string Slug, string ZonaHoraria,
    IEnumerable<ServicioDto> Servicios, IEnumerable<HorarioDto> Horarios);

public sealed record DisponibilidadDto(DateOnly Fecha, int ServicioId, IReadOnlyList<TimeOnly> Turnos);

public sealed record NuevaCita(
    [Range(1, int.MaxValue)] int ServicioId,
    DateOnly Fecha,
    TimeOnly Inicio,
    [Required, StringLength(120, MinimumLength = 2)] string ClienteNombre,
    [Required, StringLength(20, MinimumLength = 7)] string ClienteTelefono);

public sealed record CitaCreadaDto(int Id, string Servicio, DateOnly Fecha, TimeOnly Inicio, TimeOnly Fin, string EnlaceWhatsApp);
