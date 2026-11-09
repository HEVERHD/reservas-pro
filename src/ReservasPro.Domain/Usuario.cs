namespace ReservasPro.Domain;

/// <summary>El dueño (o un empleado) que administra un negocio desde el panel.</summary>
public sealed class Usuario
{
    public int Id { get; private set; }
    public int NegocioId { get; private set; }
    public string Email { get; private set; } = "";
    /// <summary>Hash de la contraseña; el formato lo define la capa que lo calcula.</summary>
    public string HashClave { get; private set; } = "";

    private Usuario() { }

    public Usuario(Negocio negocio, string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ReglaDeNegocioException("El correo no es válido.");
        NegocioId = negocio.Id;
        Email = Normalizar(email);
    }

    public void CambiarHashClave(string hash) => HashClave = hash;

    public static string Normalizar(string email) => email.Trim().ToLowerInvariant();
}
