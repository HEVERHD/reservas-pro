using System.Text.RegularExpressions;

namespace ReservasPro.Domain;

public sealed partial class Negocio
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = "";
    /// <summary>Identificador público en la URL de reservas, por ejemplo <c>barberia-centro</c>.</summary>
    public string Slug { get; private set; } = "";
    /// <summary>Número de WhatsApp en formato internacional, solo dígitos (573001234567).</summary>
    public string WhatsApp { get; private set; } = "";
    /// <summary>Zona horaria IANA del negocio; los horarios y las citas están en esta hora local.</summary>
    public string ZonaHoraria { get; private set; } = "America/Bogota";
    /// <summary>Cada cuántos minutos empieza un turno posible (15 → 9:00, 9:15, 9:30…).</summary>
    public int IntervaloMinutos { get; private set; } = 15;

    public List<Servicio> Servicios { get; private set; } = [];
    public List<Horario> Horarios { get; private set; } = [];

    private Negocio() { }

    public Negocio(string nombre, string slug, string whatsApp, string zonaHoraria = "America/Bogota", int intervaloMinutos = 15)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaDeNegocioException("El nombre del negocio es obligatorio.");
        if (!SlugValido().IsMatch(slug))
            throw new ReglaDeNegocioException("El slug solo admite minúsculas, números y guiones.");
        if (intervaloMinutos is < 5 or > 120)
            throw new ReglaDeNegocioException("El intervalo entre turnos debe estar entre 5 y 120 minutos.");

        Nombre = nombre.Trim();
        Slug = slug;
        WhatsApp = Telefono.SoloDigitos(whatsApp);
        ZonaHoraria = zonaHoraria;
        IntervaloMinutos = intervaloMinutos;
    }

    public Servicio AgregarServicio(string nombre, int duracionMinutos, decimal precio)
    {
        var servicio = new Servicio(nombre, duracionMinutos, precio);
        Servicios.Add(servicio);
        return servicio;
    }

    public Horario AgregarHorario(DayOfWeek dia, TimeOnly apertura, TimeOnly cierre)
    {
        var horario = new Horario(dia, apertura, cierre);
        if (FranjasDe(dia).Any(f => f.SeSolapaCon(horario.Franja)))
            throw new ReglaDeNegocioException("La franja se cruza con otro horario del mismo día.");
        Horarios.Add(horario);
        return horario;
    }

    /// <summary>Las franjas en que el negocio atiende un día de la semana.</summary>
    public IEnumerable<Franja> FranjasDe(DayOfWeek dia) =>
        Horarios.Where(h => h.Dia == dia).Select(h => h.Franja);

    /// <summary>Convierte un instante a la hora local del negocio.</summary>
    public DateTime HoraLocal(DateTimeOffset instante) =>
        TimeZoneInfo.ConvertTime(instante, TimeZoneInfo.FindSystemTimeZoneById(ZonaHoraria)).DateTime;

    /// <summary>Las franjas en que el negocio atiende un día dado.</summary>
    public IEnumerable<Franja> FranjasDe(DateOnly fecha) => FranjasDe(fecha.DayOfWeek);

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugValido();
}
