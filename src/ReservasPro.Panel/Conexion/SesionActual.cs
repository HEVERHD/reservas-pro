using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ReservasPro.Contracts;

namespace ReservasPro.Panel.Conexion;

/// <summary>
/// Guarda la sesión del dueño y le dice a Blazor si hay alguien autenticado.
/// El token vive en sessionStorage: sobrevive a recargar la página pero se borra al cerrar la pestaña.
/// </summary>
public sealed class SesionActual(IJSRuntime js) : AuthenticationStateProvider
{
    private const string Clave = "reservas-pro.sesion";
    private static readonly AuthenticationState Anonimo = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private Sesion? _sesion;
    private bool _cargada;

    public async Task<Sesion?> ObtenerAsync()
    {
        if (!_cargada)
        {
            var json = await js.InvokeAsync<string?>("sessionStorage.getItem", Clave);
            _sesion = json is null ? null : JsonSerializer.Deserialize<Sesion>(json, JsonSerializerOptions.Web);
            _cargada = true;
        }
        if (_sesion is not null && _sesion.Expira <= DateTimeOffset.UtcNow)
            await CerrarAsync();
        return _sesion;
    }

    public async Task IniciarAsync(Sesion sesion)
    {
        _sesion = sesion;
        _cargada = true;
        await js.InvokeVoidAsync("sessionStorage.setItem", Clave, JsonSerializer.Serialize(sesion, JsonSerializerOptions.Web));
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task CerrarAsync()
    {
        _sesion = null;
        _cargada = true;
        await js.InvokeVoidAsync("sessionStorage.removeItem", Clave);
        NotifyAuthenticationStateChanged(Task.FromResult(Anonimo));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var sesion = await ObtenerAsync();
        if (sesion is null)
            return Anonimo;

        // Solo sirve para la interfaz; quien de verdad valida el token es la API en cada petición
        var identidad = new ClaimsIdentity([new Claim(ClaimTypes.Name, sesion.Negocio)], authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identidad));
    }
}
