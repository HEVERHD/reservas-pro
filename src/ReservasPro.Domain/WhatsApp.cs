using System.Globalization;

namespace ReservasPro.Domain;

public static class WhatsApp
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-CO");

    /// <summary>Enlace wa.me que abre el chat con el negocio con el mensaje ya escrito.</summary>
    public static string Enlace(string numero, string mensaje) =>
        $"https://wa.me/{Telefono.SoloDigitos(numero)}?text={Uri.EscapeDataString(mensaje)}";

    /// <summary>Mensaje que el cliente envía al negocio para confirmar su cita.</summary>
    public static string MensajeConfirmacion(Negocio negocio, Servicio servicio, Cita cita) =>
        $"Hola {negocio.Nombre}, soy {cita.ClienteNombre}. Confirmo mi cita de {servicio.Nombre} " +
        $"el {cita.Fecha.ToString("dddd d 'de' MMMM", Es)} a las {cita.Inicio.ToString("h:mm tt", Es)}. " +
        $"Código de reserva: #{cita.Id}.";

    public static string EnlaceConfirmacion(Negocio negocio, Servicio servicio, Cita cita) =>
        Enlace(negocio.WhatsApp, MensajeConfirmacion(negocio, servicio, cita));
}
