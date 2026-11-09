using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReservasPro.Api.Auth;
using ReservasPro.Api.Data;
using ReservasPro.Contracts;
using ReservasPro.Domain;

namespace ReservasPro.Api.Endpoints;

public static class Autenticacion
{
    public static void MapAutenticacion(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", IniciarSesion)
            .WithTags("Autenticación")
            .RequireRateLimiting(Program.LimiteLogin);
    }

    private static async Task<Results<Ok<Sesion>, UnauthorizedHttpResult>> IniciarSesion(
        Login pedido, ReservasDbContext db, IPasswordHasher<Usuario> hasher, EmisorDeTokens tokens, CancellationToken ct)
    {
        var email = Usuario.Normalizar(pedido.Email);
        var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Email == email, ct);

        // Se verifica un hash aunque el usuario no exista, para que el tiempo de respuesta
        // no delate qué correos están registrados.
        var resultado = hasher.VerifyHashedPassword(usuario ?? UsuarioFicticio.Instancia, usuario?.HashClave ?? UsuarioFicticio.Hash, pedido.Clave);
        if (usuario is null || resultado == PasswordVerificationResult.Failed)
            return TypedResults.Unauthorized();

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.CambiarHashClave(hasher.HashPassword(usuario, pedido.Clave));
            await db.SaveChangesAsync(ct);
        }

        var negocio = await db.Negocios.AsNoTracking().SingleAsync(n => n.Id == usuario.NegocioId, ct);
        var (token, expira) = tokens.Emitir(usuario);
        return TypedResults.Ok(new Sesion(token, expira, negocio.Nombre, negocio.Slug));
    }

    private static class UsuarioFicticio
    {
        public static readonly Usuario Instancia = new(new Negocio("-", "x", "0000000"), "nadie@ejemplo.test");
        public static readonly string Hash = new PasswordHasher<Usuario>().HashPassword(Instancia, Guid.NewGuid().ToString());
    }
}
