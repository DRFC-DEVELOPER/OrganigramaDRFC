using System.Text;
using System.Text.Json;
using GafetesDRFC.Data;
using GafetesDRFC.Export;
using GafetesDRFC.Models;
using GafetesDRFC.Rendering;
using SkiaSharp;

namespace GafetesDRFC;

internal static class Program
{
    private static int Main(string[] args)
    {
        var carpetaBase = Directory.GetCurrentDirectory();
        var modoDebug = args.Any(argumento =>
            string.Equals(argumento, "--debug", StringComparison.OrdinalIgnoreCase));
        var argumentosEntrada = args
            .Where(argumento => !string.Equals(argumento, "--debug", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (argumentosEntrada.Length > 1)
        {
            Console.Error.WriteLine("Indica un solo archivo de entrada y, opcionalmente, --debug.");
            return 1;
        }

        var rutaEntrada = argumentosEntrada.Length == 1
            ? Path.GetFullPath(argumentosEntrada[0], carpetaBase)
            : Path.Combine(carpetaBase, "datos_ejemplo.csv");

        try
        {
            var configuracion = LeerConfiguracion(carpetaBase);
            var carpetaAssets = Path.Combine(carpetaBase, "Assets");
            var carpetaFotos = Path.Combine(carpetaBase, "Fotos");
            var carpetaFotosOrganigrama = Path.GetFullPath(
                Path.Combine(carpetaBase, "..", "assets", "img"));
            var carpetaSalida = Path.Combine(carpetaBase, "Salida");
            Directory.CreateDirectory(carpetaSalida);
            Directory.CreateDirectory(carpetaFotos);
            Directory.CreateDirectory(Path.Combine(carpetaAssets, "Clientes"));

            var filas = LectorDatos.Leer(rutaEntrada);
            using var renderizador = new RenderizadorGafete(
                carpetaAssets, carpetaFotos, carpetaFotosOrganigrama, configuracion);
            using var lote = new ExportadorPdfLote(Path.Combine(carpetaSalida, "Lote.pdf"));
            using var log = new StreamWriter(Path.Combine(carpetaSalida, "generacion.log"),
                append: false, encoding: new UTF8Encoding(false));

            var generados = 0;
            var errores = 0;
            var filasConAdvertencias = new SortedSet<int>();
            var nombresSalida = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var fila in filas)
            {
                if (fila.Empleado is null)
                {
                    Registrar(log, fila.NumeroFila, "error", fila.Error ?? "Registro inválido.");
                    errores++;
                    continue;
                }

                var advertencias = new List<string>();
                try
                {
                    Action<string> registrarAdvertencia = mensaje =>
                    {
                        advertencias.Add(mensaje);
                        Registrar(log, fila.NumeroFila, "advertencia", mensaje);
                    };
                    using var frente = renderizador.RenderizarFrente(fila.Empleado, registrarAdvertencia);
                    using var reverso = renderizador.RenderizarReverso(fila.Empleado, registrarAdvertencia);
                    using var frenteDebug = modoDebug && fila.Empleado.Tipo == TipoGafete.Oficina
                        ? renderizador.RenderizarFrente(fila.Empleado, _ => { }, debug: true)
                        : null;

                    var prefijo = NombreSeguro(fila.Empleado.IdEmpleado);
                    if (!nombresSalida.Add(prefijo))
                        throw new InvalidDataException("Identificador duplicado en el archivo de entrada.");
                    ExportadorImagenPdf.GuardarPng(frente,
                        Path.Combine(carpetaSalida, $"{prefijo}_Frente.png"));
                    ExportadorImagenPdf.GuardarPng(reverso,
                        Path.Combine(carpetaSalida, $"{prefijo}_Reverso.png"));
                    if (frenteDebug is not null)
                        ExportadorImagenPdf.GuardarPng(frenteDebug,
                            Path.Combine(carpetaSalida, $"{prefijo}_Frente_Debug.png"));
                    ExportadorImagenPdf.GuardarPdfEmpleado(frente, reverso,
                        Path.Combine(carpetaSalida, $"{prefijo}.pdf"));
                    lote.AgregarEmpleado(frente, reverso);
                    generados++;
                    if (advertencias.Count > 0)
                        filasConAdvertencias.Add(fila.NumeroFila);
                }
                catch (Exception ex) when (ex is IOException or InvalidDataException or
                                           UnauthorizedAccessException or ArgumentException or
                                           FormatException or SKException)
                {
                    Registrar(log, fila.NumeroFila, "error", $"No se generó el registro ({ex.GetType().Name}).");
                    errores++;
                }
            }

            log.Flush();
            Console.WriteLine($"Gafetes generados: {generados}");
            Console.WriteLine($"Filas con advertencias: {FormatearFilas(filasConAdvertencias)}");
            Console.WriteLine($"Filas con errores: {errores}. Consulta Salida/generacion.log.");
            return generados == 0 ? 1 : 0;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or
                                   UnauthorizedAccessException or JsonException or ArgumentException)
        {
            var detalle = ex is InvalidDataException or FileNotFoundException
                ? $": {ex.Message}"
                : ". Revisa el archivo de entrada, los recursos y los permisos.";
            Console.Error.WriteLine($"No se pudo iniciar la generación ({ex.GetType().Name}){detalle}");
            return 1;
        }
    }

    private static ConfiguracionGafetes LeerConfiguracion(string carpetaBase)
    {
        var ruta = Path.Combine(carpetaBase, "appsettings.json");
        if (!File.Exists(ruta))
            throw new FileNotFoundException("No se encontró appsettings.json.");
        var contenido = File.ReadAllText(ruta);
        return JsonSerializer.Deserialize<ConfiguracionGafetes>(contenido,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new InvalidDataException("appsettings.json no contiene una configuración válida.");
    }

    private static void Registrar(StreamWriter log, int fila, string nivel, string mensaje) =>
        log.WriteLine($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} | fila {fila} | {nivel} | {mensaje}");

    private static string NombreSeguro(string idEmpleado)
    {
        var nombre = new string(idEmpleado.Trim()
            .Select(caracter => Path.GetInvalidFileNameChars().Contains(caracter) ? '_' : caracter)
            .ToArray());
        if (string.IsNullOrWhiteSpace(nombre) || nombre is "." or "..")
            throw new InvalidDataException("El identificador no es válido para crear archivos.");
        return nombre;
    }

    private static string FormatearFilas(IEnumerable<int> filas)
    {
        var lista = filas.ToArray();
        return lista.Length == 0 ? "ninguno" : string.Join(", ", lista);
    }
}
