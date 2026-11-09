namespace ReservasPro.Domain;

public static class Telefono
{
    /// <summary>Deja solo los dígitos: "+57 300 123 4567" → "573001234567".</summary>
    public static string SoloDigitos(string numero)
    {
        var digitos = new string(numero.Where(char.IsAsciiDigit).ToArray());
        if (digitos.Length is < 7 or > 15)
            throw new ReglaDeNegocioException("El teléfono debe tener entre 7 y 15 dígitos.");
        return digitos;
    }
}
