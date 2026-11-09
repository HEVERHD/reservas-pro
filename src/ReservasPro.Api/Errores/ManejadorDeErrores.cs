using Microsoft.AspNetCore.Diagnostics;
using Npgsql;
using ReservasPro.Domain;

namespace ReservasPro.Api.Errores;

/// <summary>Convierte los errores esperados en respuestas ProblemDetails; el resto queda como 500 sin detalles internos.</summary>
public sealed class ManejadorDeErrores(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception error, CancellationToken ct)
    {
        (int estado, string titulo, string? detalle) = error switch
        {
            TurnoNoDisponibleException e => (StatusCodes.Status409Conflict, "Turno no disponible", e.Message),
            // 40001: otra transacción reservó el mismo turno al mismo tiempo
            _ when EsConflictoDeConcurrencia(error) => (StatusCodes.Status409Conflict, "Turno no disponible", new TurnoNoDisponibleException().Message),
            ReglaDeNegocioException e => (StatusCodes.Status400BadRequest, "Datos no válidos", e.Message),
            _ => (0, "", null),
        };
        if (estado == 0)
            return false;

        http.Response.StatusCode = estado;
        return await problemDetails.TryWriteAsync(new()
        {
            HttpContext = http,
            Exception = error,
            ProblemDetails = { Status = estado, Title = titulo, Detail = detalle },
        });
    }

    private static bool EsConflictoDeConcurrencia(Exception error) =>
        error is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure }
        || error.InnerException is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure };
}
