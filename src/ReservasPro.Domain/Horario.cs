namespace ReservasPro.Domain;

/// <summary>
/// Una franja de atención semanal. Un día puede tener varias (por ejemplo 8:00–12:00 y 14:00–18:00).
/// </summary>
public sealed class Horario
{
    public int Id { get; private set; }
    public int NegocioId { get; private set; }
    public DayOfWeek Dia { get; private set; }
    public TimeOnly Apertura { get; private set; }
    public TimeOnly Cierre { get; private set; }

    public Franja Franja => new(Apertura, Cierre);

    private Horario() { }

    public Horario(DayOfWeek dia, TimeOnly apertura, TimeOnly cierre)
    {
        _ = new Franja(apertura, cierre); // valida que el cierre sea posterior a la apertura
        Dia = dia;
        Apertura = apertura;
        Cierre = cierre;
    }
}
