using System.Globalization;

namespace ReservasPro.Panel;

/// <summary>
/// Textos en español sin depender de que el navegador cargue los datos de cultura es-CO
/// (Blazor WebAssembly solo trae los del idioma del navegador).
/// </summary>
public static class Formato
{
    private static readonly string[] Dias = ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
    private static readonly string[] Meses = ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio",
        "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    /// <summary>Lunes primero, como se lee una semana de trabajo.</summary>
    public static readonly DayOfWeek[] Semana =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    public static string Dia(DayOfWeek dia) => Dias[(int)dia];

    public static string Fecha(DateOnly fecha) => $"{Dia(fecha.DayOfWeek)} {fecha.Day} de {Meses[fecha.Month - 1]}";

    public static string Hora(TimeOnly hora) => hora.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string Pesos(decimal valor) =>
        "$" + valor.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', '.');
}
