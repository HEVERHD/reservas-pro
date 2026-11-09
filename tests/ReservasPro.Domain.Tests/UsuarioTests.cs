using ReservasPro.Domain;

namespace ReservasPro.Domain.Tests;

public class UsuarioTests
{
    [Fact]
    public void Guarda_el_correo_normalizado_para_que_el_login_no_dependa_de_mayusculas()
    {
        var negocio = new Negocio("Barbería", "barberia", "3001234567");

        var usuario = new Usuario(negocio, "  Dueno@Barberia.COM ");

        Assert.Equal("dueno@barberia.com", usuario.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sin-arroba")]
    public void Rechaza_correos_no_validos(string email)
    {
        var negocio = new Negocio("Barbería", "barberia", "3001234567");

        Assert.Throws<ReglaDeNegocioException>(() => new Usuario(negocio, email));
    }
}
