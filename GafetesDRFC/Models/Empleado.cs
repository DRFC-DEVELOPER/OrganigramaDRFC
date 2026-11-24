namespace GafetesDRFC.Models;

public enum TipoGafete
{
    Oficina,
    Campo
}

public sealed class Empleado
{
    public required string Nombre { get; init; }
    public required string Apellidos { get; init; }
    public required string Puesto { get; init; }
    public required string Area { get; init; }
    public required string IdEmpleado { get; init; }
    public required TipoGafete Tipo { get; init; }
    public string TipoSangre { get; init; } = "";
    public string ContactoEmergencia { get; init; } = "";
    public string TelefonoEmergencia { get; init; } = "";
    public string Foto { get; init; } = "";
    public string ClienteLogo { get; init; } = "";
}

public sealed record EmpleadoFila(int NumeroFila, Empleado? Empleado, string? Error = null);
