using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using ReservasPro.Contracts;

namespace ReservasPro.Panel.Conexion;

/// <summary>Un error que la API explicó (regla de negocio, datos no válidos…); el mensaje se puede mostrar tal cual.</summary>
public sealed class ErrorDeApi(string message) : Exception(message);

/// <summary>Cliente tipado de la API: agrega el token y traduce los errores a mensajes para el usuario.</summary>
public sealed class ApiPanel(HttpClient http, SesionActual sesion, NavigationManager navegacion)
{
    public async Task<Sesion> IniciarSesionAsync(Login login)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Post, "api/auth/login") { Content = JsonContent.Create(login) };
        using var respuesta = await EnviarSeguroAsync(pedido);
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            throw new ErrorDeApi("Correo o contraseña incorrectos.");
        if (respuesta.StatusCode == HttpStatusCode.TooManyRequests)
            throw new ErrorDeApi("Demasiados intentos. Espera un minuto y vuelve a probar.");
        await AsegurarExitoAsync(respuesta);
        return (await respuesta.Content.ReadFromJsonAsync<Sesion>())!;
    }

    public Task<List<CitaDelDia>> CitasAsync(DateOnly fecha) => GetAsync<List<CitaDelDia>>($"api/panel/citas?fecha={fecha:yyyy-MM-dd}");
    public Task CancelarCitaAsync(int id) => EnviarAsync(HttpMethod.Post, $"api/panel/citas/{id}/cancelar");

    public Task<List<ServicioAdmin>> ServiciosAsync() => GetAsync<List<ServicioAdmin>>("api/panel/servicios");
    public Task CrearServicioAsync(NuevoServicio servicio) => EnviarAsync(HttpMethod.Post, "api/panel/servicios", servicio);
    public Task DesactivarServicioAsync(int id) => EnviarAsync(HttpMethod.Post, $"api/panel/servicios/{id}/desactivar");

    public Task<List<HorarioAdmin>> HorariosAsync() => GetAsync<List<HorarioAdmin>>("api/panel/horarios");
    public Task CrearHorarioAsync(NuevoHorario horario) => EnviarAsync(HttpMethod.Post, "api/panel/horarios", horario);
    public Task QuitarHorarioAsync(int id) => EnviarAsync(HttpMethod.Delete, $"api/panel/horarios/{id}");

    private async Task<T> GetAsync<T>(string url)
    {
        using var respuesta = await SolicitarAsync(HttpMethod.Get, url, null);
        return (await respuesta.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task EnviarAsync(HttpMethod metodo, string url, object? cuerpo = null)
    {
        using var _ = await SolicitarAsync(metodo, url, cuerpo);
    }

    private async Task<HttpResponseMessage> SolicitarAsync(HttpMethod metodo, string url, object? cuerpo)
    {
        var actual = await sesion.ObtenerAsync();
        using var pedido = new HttpRequestMessage(metodo, url);
        if (actual is not null)
            pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actual.Token);
        if (cuerpo is not null)
            pedido.Content = JsonContent.Create(cuerpo, cuerpo.GetType());

        var respuesta = await EnviarSeguroAsync(pedido);
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
        {
            respuesta.Dispose();
            await sesion.CerrarAsync();
            navegacion.NavigateTo("login");
            throw new ErrorDeApi("Tu sesión terminó. Vuelve a iniciar sesión.");
        }
        await AsegurarExitoAsync(respuesta);
        return respuesta;
    }

    private async Task<HttpResponseMessage> EnviarSeguroAsync(HttpRequestMessage pedido)
    {
        try
        {
            return await http.SendAsync(pedido);
        }
        catch (HttpRequestException)
        {
            throw new ErrorDeApi("No hay conexión con el servidor. Revisa tu internet e inténtalo de nuevo.");
        }
    }

    private static async Task AsegurarExitoAsync(HttpResponseMessage respuesta)
    {
        if (respuesta.IsSuccessStatusCode)
            return;

        var problema = await LeerProblemaAsync(respuesta);
        respuesta.Dispose();
        throw new ErrorDeApi(problema ?? "No se pudo completar la operación. Inténtalo de nuevo.");
    }

    private static async Task<string?> LeerProblemaAsync(HttpResponseMessage respuesta)
    {
        try
        {
            var problema = await respuesta.Content.ReadFromJsonAsync<Problema>();
            var errores = problema?.Errors?.SelectMany(e => e.Value);
            return errores?.Any() == true ? string.Join(" ", errores) : problema?.Detail;
        }
        catch (Exception)
        {
            return null; // la respuesta no era ProblemDetails
        }
    }

    private sealed record Problema(string? Detail, Dictionary<string, string[]>? Errors);
}
