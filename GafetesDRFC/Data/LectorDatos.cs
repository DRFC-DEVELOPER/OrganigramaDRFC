using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using GafetesDRFC.Models;
using System.Globalization;

namespace GafetesDRFC.Data;

public static class LectorDatos
{
    public static IReadOnlyList<EmpleadoFila> Leer(string ruta)
    {
        try
        {
            var extension = Path.GetExtension(ruta);
            if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
                return LeerCsv(ruta);
            if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".xlsm", StringComparison.OrdinalIgnoreCase))
                return LeerExcel(ruta);

            throw new InvalidDataException("Formato no compatible. Usa un archivo CSV o XLSX.");
        }
        catch (CsvHelperException)
        {
            throw new InvalidDataException("No se pudo interpretar el CSV; revisa su codificación, delimitador y encabezados.");
        }
    }

    private static IReadOnlyList<EmpleadoFila> LeerCsv(string ruta)
    {
        var filas = new List<EmpleadoFila>();
        var configuracion = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            PrepareHeaderForMatch = args => args.Header.Trim(),
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var lector = new StreamReader(ruta, new System.Text.UTF8Encoding(false), true);
        using var csv = new CsvReader(lector, configuracion);
        csv.Read();
        csv.ReadHeader();
        var encabezados = csv.HeaderRecord?.Select(x => x.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? throw new InvalidDataException("El CSV no contiene encabezados.");
        ValidarEncabezados(encabezados);

        var numeroFila = 1;
        while (csv.Read())
        {
            numeroFila++;
            try
            {
                var registro = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var encabezado in csv.HeaderRecord!)
                    registro[encabezado.Trim()] = csv.GetField(encabezado)?.Trim() ?? "";
                filas.Add(new EmpleadoFila(numeroFila, CrearEmpleado(registro)));
            }
            catch (Exception ex) when (ex is InvalidDataException or CsvHelperException or FormatException)
            {
                filas.Add(new EmpleadoFila(numeroFila, null, "Registro inválido."));
            }
        }

        return filas;
    }

    private static IReadOnlyList<EmpleadoFila> LeerExcel(string ruta)
    {
        using var libro = new XLWorkbook(ruta);
        var hoja = libro.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("El libro de Excel no tiene hojas.");
        var ultimaColumna = hoja.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
        var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 1;
        if (ultimaColumna == 0)
            throw new InvalidDataException("La primera hoja no tiene encabezados.");

        var nombresColumnas = Enumerable.Range(1, ultimaColumna)
            .ToDictionary(c => hoja.Cell(1, c).GetString().Trim(), c => c, StringComparer.OrdinalIgnoreCase);
        ValidarEncabezados(nombresColumnas.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase));

        var filas = new List<EmpleadoFila>();
        for (var numeroFila = 2; numeroFila <= ultimaFila; numeroFila++)
        {
            var registro = nombresColumnas.ToDictionary(
                columna => columna.Key,
                columna => hoja.Cell(numeroFila, columna.Value).GetFormattedString().Trim(),
                StringComparer.OrdinalIgnoreCase);
            if (registro.Values.All(string.IsNullOrWhiteSpace))
                continue;
            try
            {
                filas.Add(new EmpleadoFila(numeroFila, CrearEmpleado(registro)));
            }
            catch (InvalidDataException)
            {
                filas.Add(new EmpleadoFila(numeroFila, null, "Registro inválido."));
            }
        }

        return filas;
    }

    private static void ValidarEncabezados(HashSet<string> encabezados)
    {
        string[] requeridos = ["Nombre", "Apellidos", "Puesto", "Area", "IdEmpleado", "Tipo",
            "TipoSangre", "ContactoEmergencia", "TelefonoEmergencia", "Foto"];
        var faltantes = requeridos.Where(x => !encabezados.Contains(x)).ToArray();
        if (faltantes.Length > 0)
            throw new InvalidDataException($"Faltan encabezados requeridos: {string.Join(", ", faltantes)}.");
    }

    private static Empleado CrearEmpleado(IReadOnlyDictionary<string, string> fila)
    {
        string Valor(string nombre) => fila.TryGetValue(nombre, out var valor) ? valor : "";

        if (new[] { "Nombre", "Apellidos", "Puesto", "Area", "IdEmpleado" }
            .Any(campo => string.IsNullOrWhiteSpace(Valor(campo))))
            throw new InvalidDataException("Faltan datos requeridos.");

        var tipo = Valor("Tipo").Trim();
        if (!Enum.TryParse<TipoGafete>(tipo, true, out var tipoGafete) ||
            !Enum.IsDefined(tipoGafete))
            throw new InvalidDataException("El tipo debe ser Oficina o Campo.");

        return new Empleado
        {
            Nombre = Valor("Nombre"),
            Apellidos = Valor("Apellidos"),
            Puesto = Valor("Puesto"),
            Area = Valor("Area"),
            IdEmpleado = Valor("IdEmpleado"),
            Tipo = tipoGafete,
            TipoSangre = Valor("TipoSangre"),
            ContactoEmergencia = Valor("ContactoEmergencia"),
            TelefonoEmergencia = Valor("TelefonoEmergencia"),
            Foto = Valor("Foto"),
            ClienteLogo = Valor("ClienteLogo")
        };
    }
}
