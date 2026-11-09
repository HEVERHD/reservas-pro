namespace ReservasPro.Domain;

public enum EstadoCita
{
    Confirmada,
    Cancelada,
}

public sealed class Cita
{
    public int Id { get; private set; }
    public int NegocioId { get; private set; }
    public int ServicioId { get; private set; }
    public Servicio? Servicio { get; private set; }
    public string ClienteNombre { get; private set; } = "";
    public string ClienteTelefono { get; private set; } = "";
    /// <summary>Fecha y horas en la zona horaria del negocio.</summary>
    public DateOnly Fecha { get; private set; }
    public TimeOnly Inicio { get; private set; }
    public TimeOnly Fin { get; private set; }
    public EstadoCita Estado { get; private set; } = EstadoCita.Confirmada;
    public DateTimeOffset CreadaEn { get; private set; }

    public Franja Franja => new(Inicio, Fin);

    private Cita() { }

    /// <summary>Solo <see cref="Agenda.Reservar"/> crea citas, después de comprobar que el turno está libre.</summary>
    internal Cita(Negocio negocio, Servicio servicio, string clienteNombre, string clienteTelefono,
        DateOnly fecha, TimeOnly inicio, DateTimeOffset creadaEn)
    {
        if (string.IsNullOrWhiteSpace(clienteNombre))
            throw new ReglaDeNegocioException("El nombre del cliente es obligatorio.");

        NegocioId = negocio.Id;
        ServicioId = servicio.Id;
        Servicio = servicio;
        ClienteNombre = clienteNombre.Trim();
        ClienteTelefono = Telefono.SoloDigitos(clienteTelefono);
        Fecha = fecha;
        Inicio = inicio;
        Fin = inicio.AddMinutes(servicio.DuracionMinutos);
        CreadaEn = creadaEn;
    }

    public void Cancelar()
    {
        if (Estado == EstadoCita.Cancelada)
            throw new ReglaDeNegocioException("La cita ya estaba cancelada.");
        Estado = EstadoCita.Cancelada;
    }
}
