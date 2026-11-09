using System.ComponentModel.DataAnnotations;

namespace ReservasPro.Contracts;

// Lo que usa el panel del dueño. Los formularios son clases con propiedades editables
// porque el EditForm de Blazor las enlaza y valida con los mismos atributos que la API.

public sealed class Login
{
    [Required(ErrorMessage = "Escribe tu correo."), EmailAddress(ErrorMessage = "El correo no es válido.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu contraseña."), StringLength(128)]
    public string Clave { get; set; } = "";
}

public sealed record Sesion(string Token, DateTimeOffset Expira, string Negocio, string Slug);

public sealed record CitaDelDia(int Id, string Servicio, string ClienteNombre, string ClienteTelefono,
    TimeOnly Inicio, TimeOnly Fin, bool Cancelada);

public sealed record ServicioAdmin(int Id, string Nombre, int DuracionMinutos, decimal Precio, bool Activo);

public sealed class NuevoServicio
{
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(120)]
    public string Nombre { get; set; } = "";

    [Range(5, 480, ErrorMessage = "La duración debe estar entre 5 y 480 minutos.")]
    public int DuracionMinutos { get; set; } = 30;

    [Range(0, 100_000_000, ErrorMessage = "El precio no es válido.")]
    public decimal Precio { get; set; }
}

public sealed record HorarioAdmin(int Id, DayOfWeek Dia, TimeOnly Apertura, TimeOnly Cierre);

public sealed class NuevoHorario
{
    public DayOfWeek Dia { get; set; } = DayOfWeek.Monday;
    public TimeOnly Apertura { get; set; } = new(9, 0);
    public TimeOnly Cierre { get; set; } = new(18, 0);
}
