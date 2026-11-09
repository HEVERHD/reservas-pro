namespace ReservasPro.Domain;

public sealed class Servicio
{
    public int Id { get; private set; }
    public int NegocioId { get; private set; }
    public string Nombre { get; private set; } = "";
    public int DuracionMinutos { get; private set; }
    public decimal Precio { get; private set; }
    /// <summary>Un servicio inactivo deja de ofrecerse pero conserva sus citas pasadas.</summary>
    public bool Activo { get; private set; } = true;

    private Servicio() { }

    public Servicio(string nombre, int duracionMinutos, decimal precio)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaDeNegocioException("El nombre del servicio es obligatorio.");
        if (duracionMinutos is < 5 or > 480)
            throw new ReglaDeNegocioException("La duración debe estar entre 5 y 480 minutos.");
        if (precio < 0)
            throw new ReglaDeNegocioException("El precio no puede ser negativo.");

        Nombre = nombre.Trim();
        DuracionMinutos = duracionMinutos;
        Precio = precio;
    }

    public void Desactivar() => Activo = false;
}
