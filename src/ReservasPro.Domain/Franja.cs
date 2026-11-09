namespace ReservasPro.Domain;

/// <summary>Un intervalo dentro de un mismo día, cerrado al inicio y abierto al final: [Inicio, Fin).</summary>
public readonly record struct Franja
{
    public TimeOnly Inicio { get; }
    public TimeOnly Fin { get; }

    public Franja(TimeOnly inicio, TimeOnly fin)
    {
        if (fin <= inicio)
            throw new ReglaDeNegocioException("La hora de fin debe ser posterior a la de inicio.");
        Inicio = inicio;
        Fin = fin;
    }

    public bool SeSolapaCon(Franja otra) => Inicio < otra.Fin && otra.Inicio < Fin;
}
