namespace ReservasPro.Domain;

/// <summary>Una operación viola una regla del negocio (datos inválidos, horarios cruzados…).</summary>
public class ReglaDeNegocioException(string message) : Exception(message);

/// <summary>El turno pedido no existe o alguien lo reservó antes.</summary>
public sealed class TurnoNoDisponibleException() : ReglaDeNegocioException("Ese turno ya no está disponible.");
