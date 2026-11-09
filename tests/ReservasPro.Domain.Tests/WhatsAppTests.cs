using ReservasPro.Domain;

namespace ReservasPro.Domain.Tests;

public class WhatsAppTests
{
    [Fact]
    public void Arma_un_enlace_wa_me_con_el_mensaje_codificado()
    {
        var enlace = WhatsApp.Enlace("+57 300 123 4567", "Hola, ¿a las 3:00 p. m.?");

        Assert.Equal("https://wa.me/573001234567?text=Hola%2C%20%C2%BFa%20las%203%3A00%20p.%20m.%3F", enlace);
    }

    [Fact]
    public void El_mensaje_de_confirmacion_trae_servicio_fecha_y_hora()
    {
        var negocio = new Negocio("Barbería Centro", "barberia-centro", "3001234567");
        negocio.AgregarHorario(DayOfWeek.Tuesday, new(9, 0), new(12, 0));
        var corte = negocio.AgregarServicio("Corte", 30, 25_000m);
        var ahora = new DateTimeOffset(2026, 11, 9, 8, 0, 0, TimeSpan.FromHours(-5));
        var cita = Agenda.Reservar(negocio, corte, new(2026, 11, 10), new(10, 0), "Ana", "3001112233", [], ahora);

        var mensaje = WhatsApp.MensajeConfirmacion(negocio, corte, cita);

        Assert.StartsWith("Hola Barbería Centro, soy Ana. Confirmo mi cita de Corte el martes 10 de noviembre a las 10:00", mensaje);
    }
}
