using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ReservasPro.Domain;

namespace ReservasPro.Api.Auth;

/// <summary>Configuración de los JWT. La clave es un secreto: user-secrets en desarrollo, Key Vault o variable de entorno en producción.</summary>
public sealed class JwtOpciones
{
    public const string Seccion = "Jwt";

    public string Emisor { get; set; } = "";
    public string Audiencia { get; set; } = "";
    public string Clave { get; set; } = "";
    public int HorasDeValidez { get; set; } = 8;

    public SymmetricSecurityKey ClaveDeFirma()
    {
        var bytes = Encoding.UTF8.GetBytes(Clave);
        if (bytes.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Clave debe tener al menos 32 bytes. En desarrollo: dotnet user-secrets set \"Jwt:Clave\" \"$(openssl rand -base64 48)\"");
        return new SymmetricSecurityKey(bytes);
    }
}

public static class Claims
{
    /// <summary>El negocio que administra el usuario; todos los endpoints del panel filtran por él.</summary>
    public const string Negocio = "negocio_id";

    public static int NegocioId(this ClaimsPrincipal usuario) =>
        int.Parse(usuario.FindFirstValue(Negocio) ?? throw new InvalidOperationException("El token no trae el negocio."));
}

public sealed class EmisorDeTokens(JwtOpciones opciones, TimeProvider reloj)
{
    private readonly SigningCredentials _firma = new(opciones.ClaveDeFirma(), SecurityAlgorithms.HmacSha256);

    public (string Token, DateTimeOffset Expira) Emitir(Usuario usuario)
    {
        var expira = reloj.GetUtcNow().AddHours(opciones.HorasDeValidez);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = opciones.Emisor,
            Audience = opciones.Audiencia,
            Expires = expira.UtcDateTime,
            SigningCredentials = _firma,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = usuario.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = usuario.Email,
                [Claims.Negocio] = usuario.NegocioId.ToString(),
            },
        });
        return (token, expira);
    }
}
