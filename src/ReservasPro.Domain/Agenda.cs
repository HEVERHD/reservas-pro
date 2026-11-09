namespace ReservasPro.Domain;

/// <summary>Reglas de la agenda: qué turnos están libres y cuándo se puede reservar uno.</summary>
public static class Agenda
{
    /// <summary>
    /// Los inicios posibles para un servicio en una fecha: caben completos dentro de una franja de atención,
    /// no se cruzan con citas confirmadas y, si la fecha es hoy, todavía no han empezado.
    /// </summary>
    /// <param name="citas">Citas del negocio; las de otras fechas o canceladas se ignoran.</param>
    /// <param name="ahora">Instante actual; se pasa como parámetro para poder probar la lógica.</param>
    public static IReadOnlyList<TimeOnly> TurnosLibres(Negocio negocio, Servicio servicio, DateOnly fecha,
        IEnumerable<Cita> citas, DateTimeOffset ahora)
    {
        var horaLocal = negocio.HoraLocal(ahora);
        var hoy = DateOnly.FromDateTime(horaLocal);
        if (!servicio.Activo || fecha < hoy)
            return [];

        var ocupadas = citas
            .Where(c => c.Fecha == fecha && c.Estado == EstadoCita.Confirmada)
            .Select(c => c.Franja)
            .ToList();
        var duracion = TimeSpan.FromMinutes(servicio.DuracionMinutos);
        var paso = TimeSpan.FromMinutes(negocio.IntervaloMinutos);
        TimeOnly? despuesDe = fecha == hoy ? TimeOnly.FromDateTime(horaLocal) : null;

        var turnos = new List<TimeOnly>();
        foreach (var franja in negocio.FranjasDe(fecha).OrderBy(f => f.Inicio))
        {
            // Se recorre en TimeSpan porque TimeOnly da la vuelta a medianoche en vez de pasarse
            for (var inicio = franja.Inicio.ToTimeSpan(); inicio + duracion <= franja.Fin.ToTimeSpan(); inicio += paso)
            {
                var candidata = new Franja(TimeOnly.FromTimeSpan(inicio), TimeOnly.FromTimeSpan(inicio + duracion));
                if (candidata.Inicio <= despuesDe || ocupadas.Any(o => o.SeSolapaCon(candidata)))
                    continue;
                turnos.Add(candidata.Inicio);
            }
        }
        return turnos;
    }

    /// <summary>Crea la cita si el turno pedido sigue libre; si no, lanza <see cref="TurnoNoDisponibleException"/>.</summary>
    public static Cita Reservar(Negocio negocio, Servicio servicio, DateOnly fecha, TimeOnly inicio,
        string clienteNombre, string clienteTelefono, IEnumerable<Cita> citas, DateTimeOffset ahora)
    {
        if (servicio.NegocioId != negocio.Id)
            throw new ReglaDeNegocioException("El servicio no pertenece a este negocio.");
        if (!TurnosLibres(negocio, servicio, fecha, citas, ahora).Contains(inicio))
            throw new TurnoNoDisponibleException();

        return new Cita(negocio, servicio, clienteNombre, clienteTelefono, fecha, inicio, ahora);
    }
}
