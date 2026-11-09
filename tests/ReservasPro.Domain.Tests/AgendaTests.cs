using ReservasPro.Domain;

namespace ReservasPro.Domain.Tests;

public class AgendaTests
{
    // Lunes 9 de noviembre de 2026, 8:00 a. m. en Bogotá (UTC-5, sin horario de verano)
    private static readonly DateTimeOffset LunesTemprano = new(2026, 11, 9, 8, 0, 0, TimeSpan.FromHours(-5));
    private static readonly DateOnly Lunes = new(2026, 11, 9);
    private static readonly DateOnly Martes = new(2026, 11, 10);

    private static (Negocio Negocio, Servicio Corte) NegocioDePrueba(int duracion = 30)
    {
        var negocio = new Negocio("Barbería Centro", "barberia-centro", "+57 300 123 4567");
        negocio.AgregarHorario(DayOfWeek.Monday, new(9, 0), new(12, 0));
        negocio.AgregarHorario(DayOfWeek.Tuesday, new(9, 0), new(12, 0));
        var corte = negocio.AgregarServicio("Corte", duracion, 25_000m);
        return (negocio, corte);
    }

    [Fact]
    public void Ofrece_turnos_cada_intervalo_que_caben_completos_en_la_franja()
    {
        var (negocio, corte) = NegocioDePrueba();

        var turnos = Agenda.TurnosLibres(negocio, corte, Martes, [], LunesTemprano);

        Assert.Equal(11, turnos.Count); // 9:00, 9:15 … 11:30
        Assert.Equal(new TimeOnly(9, 0), turnos[0]);
        Assert.Equal(new TimeOnly(11, 30), turnos[^1]);
    }

    [Fact]
    public void Excluye_los_turnos_que_se_cruzan_con_una_cita_confirmada()
    {
        var (negocio, corte) = NegocioDePrueba();
        var cita = Agenda.Reservar(negocio, corte, Martes, new(10, 0), "Ana", "3001112233", [], LunesTemprano);

        var turnos = Agenda.TurnosLibres(negocio, corte, Martes, [cita], LunesTemprano);

        Assert.Contains(new TimeOnly(9, 30), turnos);  // termina justo cuando empieza la cita
        Assert.DoesNotContain(new TimeOnly(9, 45), turnos);
        Assert.DoesNotContain(new TimeOnly(10, 0), turnos);
        Assert.DoesNotContain(new TimeOnly(10, 15), turnos);
        Assert.Contains(new TimeOnly(10, 30), turnos); // empieza justo cuando termina la cita
    }

    [Fact]
    public void Una_cita_cancelada_libera_su_turno()
    {
        var (negocio, corte) = NegocioDePrueba();
        var cita = Agenda.Reservar(negocio, corte, Martes, new(10, 0), "Ana", "3001112233", [], LunesTemprano);
        cita.Cancelar();

        var turnos = Agenda.TurnosLibres(negocio, corte, Martes, [cita], LunesTemprano);

        Assert.Contains(new TimeOnly(10, 0), turnos);
    }

    [Fact]
    public void Hoy_solo_ofrece_turnos_que_aun_no_empiezan()
    {
        var (negocio, corte) = NegocioDePrueba();
        var lunesDiezYCinco = new DateTimeOffset(2026, 11, 9, 10, 5, 0, TimeSpan.FromHours(-5));

        var turnos = Agenda.TurnosLibres(negocio, corte, Lunes, [], lunesDiezYCinco);

        Assert.Equal(new TimeOnly(10, 15), turnos[0]);
    }

    [Fact]
    public void Usa_la_hora_local_del_negocio_aunque_el_instante_llegue_en_UTC()
    {
        var (negocio, corte) = NegocioDePrueba();
        var mismoInstanteEnUtc = new DateTimeOffset(2026, 11, 9, 15, 5, 0, TimeSpan.Zero); // 10:05 en Bogotá

        var turnos = Agenda.TurnosLibres(negocio, corte, Lunes, [], mismoInstanteEnUtc);

        Assert.Equal(new TimeOnly(10, 15), turnos[0]);
    }

    [Fact]
    public void No_ofrece_turnos_en_fechas_pasadas_ni_en_dias_sin_horario()
    {
        var (negocio, corte) = NegocioDePrueba();

        Assert.Empty(Agenda.TurnosLibres(negocio, corte, new DateOnly(2026, 11, 3), [], LunesTemprano));
        Assert.Empty(Agenda.TurnosLibres(negocio, corte, new DateOnly(2026, 11, 11), [], LunesTemprano)); // miércoles
    }

    [Fact]
    public void Respeta_las_franjas_partidas_del_dia()
    {
        var negocio = new Negocio("Spa", "spa", "3001234567");
        negocio.AgregarHorario(DayOfWeek.Tuesday, new(8, 0), new(12, 0));
        negocio.AgregarHorario(DayOfWeek.Tuesday, new(14, 0), new(18, 0));
        var masaje = negocio.AgregarServicio("Masaje", 60, 80_000m);

        var turnos = Agenda.TurnosLibres(negocio, masaje, Martes, [], LunesTemprano);

        Assert.Contains(new TimeOnly(11, 0), turnos);
        Assert.DoesNotContain(new TimeOnly(11, 15), turnos); // terminaría a las 12:15, ya cerrado
        Assert.DoesNotContain(new TimeOnly(13, 0), turnos);
        Assert.Contains(new TimeOnly(14, 0), turnos);
        Assert.Equal(new TimeOnly(17, 0), turnos[^1]);
    }

    [Fact]
    public void Un_servicio_inactivo_no_tiene_turnos()
    {
        var (negocio, corte) = NegocioDePrueba();
        corte.Desactivar();

        Assert.Empty(Agenda.TurnosLibres(negocio, corte, Martes, [], LunesTemprano));
    }

    [Fact]
    public void No_permite_reservar_un_turno_ocupado()
    {
        var (negocio, corte) = NegocioDePrueba();
        var cita = Agenda.Reservar(negocio, corte, Martes, new(10, 0), "Ana", "3001112233", [], LunesTemprano);

        Assert.Throws<TurnoNoDisponibleException>(() =>
            Agenda.Reservar(negocio, corte, Martes, new(10, 15), "Luis", "3004445566", [cita], LunesTemprano));
    }

    [Fact]
    public void La_cita_termina_cuando_acaba_el_servicio()
    {
        var (negocio, corte) = NegocioDePrueba(duracion: 45);

        var cita = Agenda.Reservar(negocio, corte, Martes, new(9, 0), "Ana", "+57 300 111 2233", [], LunesTemprano);

        Assert.Equal(new TimeOnly(9, 45), cita.Fin);
        Assert.Equal("573001112233", cita.ClienteTelefono);
        Assert.Equal(EstadoCita.Confirmada, cita.Estado);
    }
}
