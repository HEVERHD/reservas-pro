using ReservasPro.Domain;

namespace ReservasPro.Domain.Tests;

public class NegocioTests
{
    [Fact]
    public void Guarda_el_WhatsApp_solo_con_digitos()
    {
        var negocio = new Negocio("Barbería", "barberia", "+57 (300) 123-4567");

        Assert.Equal("573001234567", negocio.WhatsApp);
    }

    [Theory]
    [InlineData("Barberia")]
    [InlineData("barbería")]
    [InlineData("barberia centro")]
    [InlineData("-barberia")]
    public void Rechaza_slugs_que_no_sirven_en_una_URL(string slug)
    {
        Assert.Throws<ReglaDeNegocioException>(() => new Negocio("Barbería", slug, "3001234567"));
    }

    [Fact]
    public void No_permite_horarios_que_se_cruzan_el_mismo_dia()
    {
        var negocio = new Negocio("Barbería", "barberia", "3001234567");
        negocio.AgregarHorario(DayOfWeek.Monday, new(9, 0), new(12, 0));

        Assert.Throws<ReglaDeNegocioException>(() =>
            negocio.AgregarHorario(DayOfWeek.Monday, new(11, 0), new(14, 0)));
        negocio.AgregarHorario(DayOfWeek.Monday, new(12, 0), new(14, 0)); // contiguo sí se permite
    }

    [Fact]
    public void Un_horario_debe_cerrar_despues_de_abrir()
    {
        var negocio = new Negocio("Barbería", "barberia", "3001234567");

        Assert.Throws<ReglaDeNegocioException>(() =>
            negocio.AgregarHorario(DayOfWeek.Monday, new(18, 0), new(9, 0)));
    }
}
