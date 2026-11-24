using GafetesDRFC.Models;
using SkiaSharp;
using System.Globalization;
using System.Text;

namespace GafetesDRFC.Rendering;

public sealed class RenderizadorGafete : IDisposable
{
    private sealed record CajaTextoOficina(string Nombre, SKRect Rectangulo, SKColor ColorGuia);
    private sealed record LayoutFrenteOficina(
        float YNombre,
        float YApellidos,
        float YBarra,
        float YPuesto,
        float YArea,
        float YCajaId,
        float YSitio,
        SKRect Encabezado,
        SKRect Logo,
        SKRect Foto,
        SKRect Barra,
        SKRect CajaId,
        SKRect CintaInferior,
        IReadOnlyList<CajaTextoOficina> Textos);

    private const int Ancho = 1275;
    private const int Alto = 2022;
    private const string ColorSlate = "#273847";
    private const string ColorBlue = "#318BB1";
    private const string ColorCyan = "#55B5D5";
    private const string ColorDeep = "#0D425C";
    private const string ColorPale = "#EAF4F8";
    private const string ColorGris = "#5B6B78";
    private const string ColorDivisor = "#DFE8EE";
    private const string ColorPunteado = "#96AAB9";

    private readonly SKTypeface _regular;
    private readonly SKTypeface _medium;
    private readonly SKTypeface _bold;
    private readonly string _rutaLogo;
    private readonly string _rutaFirma;
    private readonly string _rutaFotos;
    private readonly string _rutaFotosOrganigrama;
    private readonly string _rutaClientes;
    private readonly ConfiguracionGafetes _configuracion;
    private readonly SKBitmap _logo;
    private readonly SKRect _limitesLogo;
    private readonly SKBitmap? _firma;
    private readonly SKRect _limitesFirma;
    private readonly Dictionary<(TipoGafete Tipo, int Alto), SKBitmap> _gradientes = [];
    private Dictionary<string, string>? _indiceFotosOrganigrama;

    private static readonly IReadOnlyDictionary<string, string> AliasFotos =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [NormalizarNombre("Ana Patricia Pérez Guerrero")] = "Ana Patricia Pérez GuerreroEstrada",
            [NormalizarNombre("Luis Alberto Martínez Cruz")] = "Luis Alberto Martínez CruzReyes Santamaria",
            [NormalizarNombre("Jesús Alberto Juárez Dominguez")] = "Jesús Alberto Juárez DominguezGuadarrama Jaimes"
        };

    public RenderizadorGafete(
        string carpetaAssets,
        string carpetaFotos,
        string carpetaFotosOrganigrama,
        ConfiguracionGafetes configuracion)
    {
        _rutaLogo = Path.Combine(carpetaAssets, "logo_blanco.png");
        _rutaFirma = Path.Combine(carpetaAssets, "firma_direccion.png");
        _rutaFotos = carpetaFotos;
        _rutaFotosOrganigrama = carpetaFotosOrganigrama;
        _rutaClientes = Path.Combine(carpetaAssets, "Clientes");
        _configuracion = configuracion;

        var carpetaFuentes = Path.Combine(carpetaAssets, "fonts");
        var recursosFaltantes = new[]
        {
            _rutaLogo,
            Path.Combine(carpetaFuentes, "Inter-Regular.otf"),
            Path.Combine(carpetaFuentes, "Inter-Medium.otf"),
            Path.Combine(carpetaFuentes, "Inter-Bold.otf")
        }.Where(ruta => !File.Exists(ruta))
            .Select(ruta => Path.GetRelativePath(Directory.GetCurrentDirectory(), ruta))
            .ToArray();
        if (recursosFaltantes.Length > 0)
            throw new FileNotFoundException(
                $"Faltan recursos obligatorios: {string.Join(", ", recursosFaltantes)}.");

        _regular = CargarFuente(Path.Combine(carpetaFuentes, "Inter-Regular.otf"));
        _medium = CargarFuente(Path.Combine(carpetaFuentes, "Inter-Medium.otf"));
        _bold = CargarFuente(Path.Combine(carpetaFuentes, "Inter-Bold.otf"));
        _logo = SKBitmap.Decode(_rutaLogo)
            ?? throw new InvalidDataException("No se pudo leer el logo blanco.");
        _limitesLogo = ObtenerLimitesAlpha(_logo);
        _firma = File.Exists(_rutaFirma) ? SKBitmap.Decode(_rutaFirma) : null;
        if (File.Exists(_rutaFirma) && _firma is null)
            throw new InvalidDataException("No se pudo leer la firma de Dirección.");
        _limitesFirma = _firma is null ? new SKRect(0, 0, 0, 0) : ObtenerLimitesAlpha(_firma);
    }

    public SKBitmap RenderizarFrente(Empleado empleado, Action<string> advertencia, bool debug = false)
    {
        var bitmap = CrearLienzo();
        var conservarBitmap = false;
        try
        {
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            var esCampo = empleado.Tipo == TipoGafete.Campo;
            var altoEncabezado = esCampo ? 700 : 800;
            DibujarEncabezado(canvas, altoEncabezado, empleado.Tipo);
            DibujarCintaInferior(canvas);
            DibujarLogo(canvas, esCampo ? 520 : 600, esCampo ? 130 : 150, altoEncabezado);

            var centroX = Ancho / 2f;
            var centroY = altoEncabezado;
            var diametroFoto = esCampo ? 520 : 640;
            DibujarFoto(canvas, empleado, centroX, centroY, diametroFoto, advertencia);

            if (esCampo)
                DibujarFrenteCampo(canvas, empleado);
            else
            {
                var layout = CrearLayoutFrenteOficina(empleado);
                VerificarLayoutFrenteOficina(layout);
                DibujarFrenteOficina(canvas, empleado, layout);
                if (debug)
                    DibujarGuiasDebugOficina(canvas, layout);
            }

            conservarBitmap = true;
            return bitmap;
        }
        finally
        {
            if (!conservarBitmap)
                bitmap.Dispose();
        }
    }

    public SKBitmap RenderizarReverso(Empleado empleado, Action<string> advertencia)
    {
        var bitmap = CrearLienzo();
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        DibujarEncabezado(canvas, 430, empleado.Tipo);
        DibujarCintaInferior(canvas);
        DibujarLogo(canvas, 520, 140, 430);
        DibujarReverso(canvas, empleado, advertencia);
        return bitmap;
    }

    private void DibujarFrenteOficina(SKCanvas canvas, Empleado empleado, LayoutFrenteOficina layout)
    {
        DibujarTextoCentrado(canvas, empleado.Nombre, _bold, 78, ColorSlate, layout.YNombre, -1);
        DibujarTextoCentrado(canvas, empleado.Apellidos, _bold, 78, ColorSlate, layout.YApellidos, -1);
        DibujarBarra(canvas, layout.YBarra);
        DibujarTextoCentrado(canvas, empleado.Puesto, _bold, 52, ColorBlue, layout.YPuesto, 0);
        DibujarTextoCentrado(canvas, empleado.Area, _medium, 44, ColorGris, layout.YArea, 0);
        DibujarCajaId(canvas, empleado.IdEmpleado, layout.YCajaId, 170, 34, 74, 5, 2);
        DibujarTextoCentrado(canvas, _configuracion.Sitio, _medium, 38, ColorGris, layout.YSitio, 2);
    }

    private LayoutFrenteOficina CrearLayoutFrenteOficina(Empleado empleado)
    {
        const float yEncabezado = 800;
        const float yNombre = yEncabezado + 300 + 40;
        const float yApellidos = yNombre + 88;
        var yBarra = yNombre + (2 * 78) + 48;
        var yPuesto = yBarra + 32;
        var yArea = yPuesto + 66;
        var yCajaId = yPuesto + 170 + 60;
        const float ySitio = 1880;

        var textos = new[]
        {
            CrearCajaTextoOficina("Nombre", empleado.Nombre, _bold, 78, yNombre, -1, new SKColor(220, 55, 55)),
            CrearCajaTextoOficina("Apellidos", empleado.Apellidos, _bold, 78, yApellidos, -1, new SKColor(255, 145, 35)),
            CrearCajaTextoOficina("Puesto", empleado.Puesto, _bold, 52, yPuesto, 0, new SKColor(35, 155, 75)),
            CrearCajaTextoOficina("Área", empleado.Area, _medium, 44, yArea, 0, new SKColor(145, 80, 190)),
            CrearCajaTextoOficina("Etiqueta ID", "ID DE EMPLEADO", _medium, 34, yCajaId + 24, 5, new SKColor(40, 135, 205)),
            CrearCajaTextoOficina("ID", empleado.IdEmpleado, _bold, 74, yCajaId + 72, 2, new SKColor(235, 100, 145)),
            CrearCajaTextoOficina("Sitio", _configuracion.Sitio, _medium, 38, ySitio, 2, new SKColor(20, 165, 165))
        };

        return new LayoutFrenteOficina(
            yNombre,
            yApellidos,
            yBarra,
            yPuesto,
            yArea,
            yCajaId,
            ySitio,
            new SKRect(0, 0, Ancho, yEncabezado),
            ObtenerCajaLogo(600, 150),
            new SKRect(Ancho / 2f - 320, yEncabezado - 320, Ancho / 2f + 320, yEncabezado + 320),
            new SKRect((Ancho - 90) / 2f, yBarra, (Ancho + 90) / 2f, yBarra + 8),
            new SKRect(200, yCajaId, Ancho - 200, yCajaId + 170),
            new SKRect(0, Alto - 44, Ancho, Alto),
            textos);
    }

    private CajaTextoOficina CrearCajaTextoOficina(string nombre, string texto, SKTypeface fuente,
        float tamano, float y, float espaciado, SKColor colorGuia)
    {
        using var paint = CrearPaint(SKColors.Black, fuente);
        paint.TextSize = tamano;
        var ancho = MedirTexto(paint, texto, espaciado);
        return new CajaTextoOficina(nombre,
            new SKRect((Ancho - ancho) / 2, y, (Ancho + ancho) / 2, y + tamano),
            colorGuia);
    }

    private static void VerificarLayoutFrenteOficina(LayoutFrenteOficina layout)
    {
        const float anchoMaximoTexto = Ancho - 160;
        for (var i = 0; i < layout.Textos.Count; i++)
        {
            var texto = layout.Textos[i];
            var caja = texto.Rectangulo;
            if (caja.Left < 0 || caja.Right > Ancho || caja.Width > anchoMaximoTexto ||
                caja.Top < 0 || caja.Bottom > Alto)
                throw new InvalidDataException($"El texto \"{texto.Nombre}\" sale del área imprimible.");
            if (caja.Bottom > layout.CintaInferior.Top)
                throw new InvalidDataException($"El texto \"{texto.Nombre}\" invade la cinta inferior.");

            for (var j = i + 1; j < layout.Textos.Count; j++)
            {
                if (SeIntersectan(caja, layout.Textos[j].Rectangulo))
                    throw new InvalidDataException(
                        $"Se detectó solapamiento entre \"{texto.Nombre}\" y \"{layout.Textos[j].Nombre}\".");
            }
        }
    }

    private static bool SeIntersectan(SKRect a, SKRect b) =>
        a.Left < b.Right && a.Right > b.Left && a.Top < b.Bottom && a.Bottom > b.Top;

    private static void DibujarGuiasDebugOficina(SKCanvas canvas, LayoutFrenteOficina layout)
    {
        DibujarGuiaRectangulo(canvas, layout.Encabezado, new SKColor(230, 40, 40));
        DibujarGuiaRectangulo(canvas, layout.Logo, new SKColor(245, 180, 20));
        DibujarGuiaRectangulo(canvas, layout.Foto, new SKColor(35, 120, 230));
        DibujarGuiaRectangulo(canvas, layout.Barra, new SKColor(35, 175, 75));
        DibujarGuiaRectangulo(canvas, layout.CajaId, new SKColor(140, 70, 190));
        DibujarGuiaRectangulo(canvas, layout.CintaInferior, new SKColor(20, 170, 170));

        foreach (var texto in layout.Textos)
            DibujarGuiaRectangulo(canvas, texto.Rectangulo, texto.ColorGuia);
    }

    private static void DibujarGuiaRectangulo(SKCanvas canvas, SKRect rectangulo, SKColor color)
    {
        using var paint = CrearPaint(color);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 3;
        canvas.DrawRect(rectangulo, paint);
    }

    private SKRect ObtenerCajaLogo(float ancho, float y)
    {
        var alto = ancho / (_limitesLogo.Width / _limitesLogo.Height);
        return new SKRect((Ancho - ancho) / 2, y, (Ancho + ancho) / 2, y + alto);
    }

    private void DibujarFrenteCampo(SKCanvas canvas, Empleado empleado)
    {
        var yNombre = 980f;
        DibujarNombre(canvas, empleado, yNombre, 68, -1);

        var yBarra = yNombre + (2 * 68) + 48;
        DibujarBarra(canvas, yBarra);
        var yPuesto = yBarra + 32;
        DibujarTextoCentradoAuto(canvas, empleado.Puesto, _bold, 52, 36, ColorBlue, yPuesto, 1115);
        DibujarTextoCentradoAuto(canvas, empleado.Area, _medium, 44, 36, ColorGris, yPuesto + 66, 1115);

        var yId = yPuesto + 170;
        DibujarCajaId(canvas, empleado.IdEmpleado, yId, 120, 30, 58, 5, 2);
        var yServicio = yId + 170;
        DibujarTextoCentrado(canvas, "AL SERVICIO DE", _bold, 40, ColorDeep, yServicio, 6);

        var cuadroLogo = new SKRect(170, yServicio + 66, Ancho - 170, yServicio + 330);
        var rutaCliente = ResolverLogoCliente(empleado.ClienteLogo);
        if (rutaCliente is not null)
        {
            DibujarImagenAjustada(canvas, rutaCliente, cuadroLogo, 30);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(empleado.ClienteLogo))
                advertencia("No se encontró el logo de cliente; se usó el recuadro guía.");
            DibujarRecuadroPunteado(canvas, cuadroLogo);
            DibujarTextoCentrado(canvas, "LOGO DE LA MARCA", _medium, 44, ColorPunteado,
                cuadroLogo.Top + 88, 4);
            DibujarTextoCentrado(canvas, "del cliente", _regular, 38, ColorPunteado,
                cuadroLogo.Top + 150, 0);
        }

        DibujarTextoCentrado(canvas, _configuracion.Sitio, _medium, 38, ColorGris,
            cuadroLogo.Bottom + 30, 2);
    }

    private void DibujarReverso(SKCanvas canvas, Empleado empleado, Action<string> advertencia)
    {
        const float margen = 110;
        const float yBase = 500;
        var anchoColumna = (Ancho - (2 * margen) - 60) / 2f;

        DibujarTextoIzquierdo(canvas, "TIPO DE SANGRE", _medium, 36, ColorGris, margen, yBase, 4);
        DibujarTextoIzquierdoAuto(canvas, empleado.TipoSangre, _bold, 116, 54, ColorBlue,
            margen, yBase + 56, anchoColumna - 20);
        DibujarTextoIzquierdo(canvas, "EMERGENCIA", _medium, 36, ColorGris,
            Ancho / 2f + 10, yBase, 4);
        DibujarTextoIzquierdoAuto(canvas, empleado.ContactoEmergencia, _bold, 44, 36,
            ColorSlate, Ancho / 2f + 10, yBase + 62, anchoColumna);
        DibujarTextoIzquierdoAuto(canvas, empleado.TelefonoEmergencia, _medium, 46, 36,
            ColorSlate, Ancho / 2f + 10, yBase + 122, anchoColumna);
        DibujarLinea(canvas, margen, yBase + 230, Ancho - margen, yBase + 230, ColorDivisor, 4);

        var yFirmas = yBase + 520;
        var xFirmaIzquierda = margen;
        var xFirmaDerecha = margen + anchoColumna + 60;
        var xCentroIzquierda = xFirmaIzquierda + anchoColumna / 2;
        var xCentroDerecha = xFirmaDerecha + anchoColumna / 2;
        DibujarLinea(canvas, xFirmaIzquierda, yFirmas, xFirmaIzquierda + anchoColumna, yFirmas,
            ColorSlate, 4);
        DibujarLinea(canvas, xFirmaDerecha, yFirmas, xFirmaDerecha + anchoColumna, yFirmas,
            ColorSlate, 4);

        if (_firma is not null)
        {
            DibujarImagenAjustada(canvas, _firma, _limitesFirma,
                new SKRect(xCentroDerecha - 165, yFirmas - 145, xCentroDerecha + 165, yFirmas + 22), 0);
        }
        else
        {
            advertencia("Falta firma_direccion.png; se omitió la firma de Dirección.");
        }

        DibujarTextoCentrado(canvas, "Firma del colaborador", _medium, 36, ColorGris,
            yFirmas + 16, 0, xCentroIzquierda);
        DibujarTextoCentrado(canvas, "Firma de Dirección", _medium, 36, ColorGris,
            yFirmas + 16, 0, xCentroDerecha);
        DibujarTextoCentrado(canvas, _configuracion.DirectorNombre, _regular, 29, ColorGris,
            yFirmas + 62, 0, xCentroDerecha);
        DibujarTextoCentrado(canvas, _configuracion.DirectorCargo, _regular, 29, ColorGris,
            yFirmas + 98, 0, xCentroDerecha);
        DibujarLinea(canvas, margen, yFirmas + 170, Ancho - margen, yFirmas + 170, ColorDivisor, 4);

        var yDireccion = yFirmas + 225;
        DibujarTextoIzquierdo(canvas, "DIRECCIÓN", _medium, 36, ColorGris, margen, yDireccion, 4);
        var direccion = new[]
        {
            _configuracion.DireccionLinea1,
            _configuracion.DireccionLinea2,
            _configuracion.DireccionLinea3
        };
        for (var i = 0; i < direccion.Length; i++)
            DibujarTextoIzquierdoAuto(canvas, direccion[i], _medium, 44, 36, ColorSlate,
                margen, yDireccion + 58 + (i * 56), Ancho - (2 * margen));

        DibujarCajaAviso(canvas);
        DibujarTextoCentrado(canvas, _configuracion.Sitio, _medium, 38, ColorGris, 1870, 2);
    }

    private void DibujarCajaAviso(SKCanvas canvas)
    {
        using var paint = CrearPaint(ColorSlate);
        var caja = new SKRect(110, 1640, Ancho - 110, 1820);
        canvas.DrawRoundRect(caja, 34, 34, paint);
        DibujarTextoCentrado(canvas, "Este gafete es personal", _bold, 54,
            SKColors.White, caja.Top + 32, 0);
        DibujarTextoCentrado(canvas, "e intransferible", _bold, 54,
            SKColors.White, caja.Top + 100, 0);
    }

    private void DibujarNombre(SKCanvas canvas, Empleado empleado, float y, float tamano, float espaciado)
    {
        DibujarTextoCentradoAuto(canvas, empleado.Nombre, _bold, tamano, 54, ColorSlate, y, Ancho - 160, espaciado);
        DibujarTextoCentradoAuto(canvas, empleado.Apellidos, _bold, tamano, 54, ColorSlate,
            y + tamano + 10, Ancho - 160, espaciado);
    }

    private void DibujarBarra(SKCanvas canvas, float y)
    {
        using var paint = CrearPaint(ColorBlue);
        canvas.DrawRoundRect(new SKRect((Ancho - 90) / 2f, y, (Ancho + 90) / 2f, y + 8), 4, 4, paint);
    }

    private void DibujarCajaId(SKCanvas canvas, string id, float y, float alto,
        float tamanoEtiqueta, float tamanoId, float espaciadoEtiqueta, float espaciadoId)
    {
        using (var paint = CrearPaint(ColorPale))
            canvas.DrawRoundRect(new SKRect(200, y, Ancho - 200, y + alto), alto * .19f, alto * .19f, paint);

        DibujarTextoCentrado(canvas, "ID DE EMPLEADO", _medium, tamanoEtiqueta, ColorGris,
            y + (alto == 170 ? 24 : 10), espaciadoEtiqueta);
        DibujarTextoCentrado(canvas, id, _bold, tamanoId, ColorSlate,
            y + (alto == 170 ? 72 : 48), espaciadoId);
    }

    private void DibujarEncabezado(SKCanvas canvas, int alto, TipoGafete tipo)
    {
        var (inicio, fin) = tipo == TipoGafete.Oficina
            ? (new SKColor(47, 65, 82), new SKColor(33, 47, 60))
            : (new SKColor(16, 80, 110), new SKColor(8, 47, 67));
        var clave = (tipo, alto);
        if (!_gradientes.TryGetValue(clave, out var gradiente))
        {
            gradiente = CrearGradiente(inicio, fin, alto);
            _gradientes.Add(clave, gradiente);
        }
        using (var paint = new SKPaint { IsAntialias = true })
            canvas.DrawBitmap(gradiente, new SKPoint(0, 0),
                new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None), paint);

        var guardado = canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, Ancho, alto));
        DibujarConector(canvas, new[] { new SKPoint(1320, 520), new SKPoint(980, 520), new SKPoint(930, 470), new SKPoint(700, 470) }, ColorCyan, .75f);
        DibujarConector(canvas, new[] { new SKPoint(1320, 600), new SKPoint(1100, 600), new SKPoint(1040, 540), new SKPoint(860, 540) }, ColorBlue, .80f);
        DibujarConector(canvas, new[] { new SKPoint(-40, 300), new SKPoint(180, 300), new SKPoint(240, 360), new SKPoint(380, 360) }, ColorBlue, .80f);
        DibujarConector(canvas, new[] { new SKPoint(-40, 420), new SKPoint(80, 420), new SKPoint(130, 470), new SKPoint(230, 470) }, ColorCyan, .60f);
        DibujarConector(canvas, new[] { new SKPoint(1320, 300), new SKPoint(1120, 300), new SKPoint(1070, 250), new SKPoint(960, 250) }, ColorBlue, .70f);
        canvas.RestoreToCount(guardado);
    }

    private static void DibujarConector(SKCanvas canvas, IReadOnlyList<SKPoint> puntos, string color, float opacidad)
    {
        var skColor = SKColor.Parse(color).WithAlpha((byte)Math.Round(255 * opacidad));
        using var paint = CrearPaint(skColor);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 9;
        paint.StrokeCap = SKStrokeCap.Round;
        paint.StrokeJoin = SKStrokeJoin.Round;
        using var path = new SKPath();
        path.MoveTo(puntos[0]);
        for (var i = 1; i < puntos.Count; i++)
            path.LineTo(puntos[i]);
        canvas.DrawPath(path, paint);
        canvas.DrawCircle(puntos[^1], 22, paint);
    }

    private static SKBitmap CrearGradiente(SKColor inicio, SKColor fin, int alto)
    {
        var bitmap = new SKBitmap(Ancho, alto, SKColorType.Rgba8888, SKAlphaType.Premul);
        for (var y = 0; y < alto; y++)
        {
            for (var x = 0; x < Ancho; x++)
            {
                var t = Math.Clamp(.5 * x / Ancho + .9 * y / alto, 0, 1);
                bitmap.SetPixel(x, y, new SKColor(
                    (byte)Math.Round(inicio.Red * (1 - t) + fin.Red * t),
                    (byte)Math.Round(inicio.Green * (1 - t) + fin.Green * t),
                    (byte)Math.Round(inicio.Blue * (1 - t) + fin.Blue * t)));
            }
        }
        return bitmap;
    }

    private void DibujarLogo(SKCanvas canvas, float ancho, float y, float altoEncabezado)
    {
        var proporcion = _limitesLogo.Width / _limitesLogo.Height;
        var alto = ancho / proporcion;
        var destino = new SKRect((Ancho - ancho) / 2, y, (Ancho + ancho) / 2, y + alto);
        if (destino.Bottom > altoEncabezado)
            destino.Bottom = altoEncabezado;
        DibujarImagen(canvas, _logo, _limitesLogo, destino);
    }

    private void DibujarFoto(SKCanvas canvas, Empleado empleado, float centroX, float centroY,
        float diametroExterior, Action<string> advertencia)
    {
        var diametroImagen = diametroExterior - 40;
        var radioExterior = diametroExterior / 2;
        var radioImagen = diametroImagen / 2;
        using (var aro = CrearPaint(SKColors.White))
            canvas.DrawCircle(centroX, centroY, radioExterior, aro);

        var rutaFoto = ResolverFoto(empleado);
        if (rutaFoto is null)
        {
            advertencia("Foto no encontrada; se usó una silueta genérica.");
            DibujarSilueta(canvas, centroX, centroY, diametroImagen);
            return;
        }

        using var foto = SKBitmap.Decode(rutaFoto);
        if (foto is null)
        {
            advertencia("No se pudo leer la foto; se usó una silueta genérica.");
            DibujarSilueta(canvas, centroX, centroY, diametroImagen);
            return;
        }

        var ladoRecorte = Math.Min(foto.Width, foto.Height) * .94f;
        var izquierda = (foto.Width - ladoRecorte) / 2f;
        var arriba = (foto.Height - ladoRecorte) / 2f;
        var origen = new SKRect(izquierda, arriba, izquierda + ladoRecorte, arriba + ladoRecorte);
        var destino = new SKRect(centroX - diametroImagen / 2, centroY - diametroImagen / 2,
            centroX + diametroImagen / 2, centroY + diametroImagen / 2);
        using var clip = new SKPath();
        clip.AddCircle(centroX, centroY, radioImagen);
        var guardado = canvas.Save();
        canvas.ClipPath(clip, SKClipOperation.Intersect, true);
        DibujarImagen(canvas, foto, origen, destino);
        canvas.RestoreToCount(guardado);
    }

    private static void DibujarSilueta(SKCanvas canvas, float centroX, float centroY, float diametro)
    {
        var radio = diametro / 2;
        using var fondo = CrearPaint(new SKColor(234, 244, 248));
        using var figura = CrearPaint(new SKColor(91, 107, 120));
        using var recorte = new SKPath();
        recorte.AddCircle(centroX, centroY, radio);
        var guardado = canvas.Save();
        canvas.ClipPath(recorte, SKClipOperation.Intersect, true);
        canvas.DrawCircle(centroX, centroY, radio, fondo);
        canvas.DrawCircle(centroX, centroY - radio * .28f, radio * .2f, figura);
        using var hombros = new SKPath();
        hombros.MoveTo(centroX - radio * .62f, centroY + radio * .82f);
        hombros.CubicTo(centroX - radio * .58f, centroY + radio * .25f,
            centroX + radio * .58f, centroY + radio * .25f,
            centroX + radio * .62f, centroY + radio * .82f);
        hombros.Close();
        canvas.DrawPath(hombros, figura);
        canvas.RestoreToCount(guardado);
    }

    private static void DibujarCintaInferior(SKCanvas canvas)
    {
        var alto = 44f;
        var y = Alto - alto;
        using var deep = CrearPaint(ColorDeep);
        using var blue = CrearPaint(ColorBlue);
        using var cyan = CrearPaint(ColorCyan);
        canvas.DrawRect(new SKRect(0, y, Ancho * .34f, Alto), deep);
        canvas.DrawRect(new SKRect(Ancho * .34f, y, Ancho * .74f, Alto), blue);
        canvas.DrawRect(new SKRect(Ancho * .74f, y, Ancho, Alto), cyan);
    }

    private void DibujarRecuadroPunteado(SKCanvas canvas, SKRect cuadro)
    {
        using var dash = SKPathEffect.CreateDash([26, 16], 0);
        using var paint = CrearPaint(ColorPunteado);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 5;
        paint.PathEffect = dash;
        canvas.DrawRect(cuadro, paint);
    }

    private void DibujarImagenAjustada(SKCanvas canvas, string ruta, SKRect caja, float margen)
    {
        using var imagen = SKBitmap.Decode(ruta)
            ?? throw new InvalidDataException("No se pudo leer una imagen requerida.");
        var origen = ObtenerLimitesAlpha(imagen);
        DibujarImagenAjustada(canvas, imagen, origen, caja, margen);
    }

    private void DibujarImagenAjustada(SKCanvas canvas, SKBitmap imagen, SKRect origen,
        SKRect caja, float margen)
    {
        var ancho = caja.Width - (2 * margen);
        var alto = caja.Height - (2 * margen);
        var escala = Math.Min(ancho / origen.Width, alto / origen.Height);
        var w = origen.Width * escala;
        var h = origen.Height * escala;
        var destino = new SKRect(caja.MidX - w / 2, caja.MidY - h / 2,
            caja.MidX + w / 2, caja.MidY + h / 2);
        DibujarImagen(canvas, imagen, origen, destino);
    }

    private void DibujarImagen(SKCanvas canvas, SKBitmap imagen, SKRect origen, SKRect destino)
    {
        var muestreo = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawBitmap(imagen, origen, destino, muestreo, paint);
    }

    private string? ResolverLogoCliente(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return null;
        return ResolverArchivoSeguro(_rutaClientes, nombre, [".png"]);
    }

    private string? ResolverFoto(Empleado empleado)
    {
        var extensiones = new[] { ".png", ".jpg", ".jpeg", ".webp" };
        var fotoExplicita = ResolverArchivoSeguro(_rutaFotos, empleado.Foto, extensiones);
        if (fotoExplicita is not null)
            return fotoExplicita;

        var nombreFoto = Path.GetFileNameWithoutExtension(empleado.Foto.Trim());
        var indice = ObtenerIndiceFotosOrganigrama();
        if (!string.IsNullOrWhiteSpace(nombreFoto) &&
            indice.TryGetValue(NormalizarNombre(nombreFoto), out var rutaPorCampo))
            return rutaPorCampo;

        var nombreCompleto = $"{empleado.Nombre} {empleado.Apellidos}".Trim();
        if (AliasFotos.TryGetValue(NormalizarNombre(nombreCompleto), out var alias) &&
            indice.TryGetValue(NormalizarNombre(alias), out var rutaAlias))
            return rutaAlias;
        return indice.TryGetValue(NormalizarNombre(nombreCompleto), out var rutaPorNombre)
            ? rutaPorNombre
            : null;
    }

    private Dictionary<string, string> ObtenerIndiceFotosOrganigrama()
    {
        if (_indiceFotosOrganigrama is not null)
            return _indiceFotosOrganigrama;

        var indice = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!Directory.Exists(_rutaFotosOrganigrama))
        {
            _indiceFotosOrganigrama = indice;
            return indice;
        }

        var carpetasPrioritarias = new[] { "Líderes", "Personal" };
        var directorios = Directory.GetDirectories(_rutaFotosOrganigrama)
            .Where(directorio => !Path.GetFileName(directorio).Equals("logos", StringComparison.OrdinalIgnoreCase) &&
                                 !Path.GetFileName(directorio).Equals("gafete", StringComparison.OrdinalIgnoreCase))
            .OrderBy(directorio =>
            {
                var prioridad = Array.FindIndex(carpetasPrioritarias,
                    nombre => nombre.Equals(Path.GetFileName(directorio), StringComparison.OrdinalIgnoreCase));
                return prioridad >= 0 ? prioridad : int.MaxValue;
            })
            .ThenBy(directorio => directorio, StringComparer.OrdinalIgnoreCase);

        foreach (var directorio in directorios)
        {
            foreach (var ruta in Directory.EnumerateFiles(directorio)
                         .Where(ruta => new[] { ".png", ".jpg", ".jpeg", ".webp" }
                             .Contains(Path.GetExtension(ruta), StringComparer.OrdinalIgnoreCase))
                         .OrderBy(ruta => ruta, StringComparer.OrdinalIgnoreCase))
            {
                var clave = NormalizarNombre(Path.GetFileNameWithoutExtension(ruta));
                if (!string.IsNullOrEmpty(clave))
                    indice.TryAdd(clave, ruta);
            }
        }

        _indiceFotosOrganigrama = indice;
        return indice;
    }

    private static string NormalizarNombre(string nombre)
    {
        var descompuesto = nombre.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(descompuesto.Length);
        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(caracter))
                resultado.Append(char.ToLowerInvariant(caracter));
        }
        return resultado.ToString();
    }

    private static string? ResolverArchivoSeguro(string carpeta, string nombre, string[] extensiones)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre != Path.GetFileName(nombre))
            return null;
        var extension = Path.GetExtension(nombre);
        if (!extensiones.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return null;
        var ruta = Path.GetFullPath(Path.Combine(carpeta, nombre));
        var prefijo = Path.GetFullPath(carpeta) + Path.DirectorySeparatorChar;
        return ruta.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase) && File.Exists(ruta) ? ruta : null;
    }

    private void DibujarTextoCentradoAuto(SKCanvas canvas, string texto, SKTypeface fuente,
        float tamano, float minimo, string color, float y, float anchoMaximo, float espaciado = 0)
    {
        using var paint = CrearPaint(color, fuente);
        paint.TextSize = tamano;
        while (tamano > minimo && MedirTexto(paint, texto, espaciado) > anchoMaximo)
        {
            tamano = Math.Max(minimo, tamano - 2);
            paint.TextSize = tamano;
        }
        DibujarTextoCentrado(canvas, texto, paint, y, espaciado);
    }

    private void DibujarTextoIzquierdoAuto(SKCanvas canvas, string texto, SKTypeface fuente,
        float tamano, float minimo, string color, float x, float y, float anchoMaximo)
    {
        using var paint = CrearPaint(color, fuente);
        paint.TextSize = tamano;
        while (tamano > minimo && MedirTexto(paint, texto, 0) > anchoMaximo)
        {
            tamano = Math.Max(minimo, tamano - 2);
            paint.TextSize = tamano;
        }
        DibujarTextoIzquierdo(canvas, texto, paint, x, y);
    }

    private void DibujarTextoCentrado(SKCanvas canvas, string texto, SKTypeface fuente,
        float tamano, string color, float y, float espaciado, float centroX = Ancho / 2f)
    {
        using var paint = CrearPaint(color, fuente);
        paint.TextSize = tamano;
        DibujarTextoCentrado(canvas, texto, paint, y, espaciado, centroX);
    }

    private void DibujarTextoIzquierdo(SKCanvas canvas, string texto, SKTypeface fuente,
        float tamano, string color, float x, float y, float espaciado)
    {
        using var paint = CrearPaint(color, fuente);
        paint.TextSize = tamano;
        DibujarTextoIzquierdo(canvas, texto, paint, x, y, espaciado);
    }

    private static void DibujarTextoCentrado(SKCanvas canvas, string texto, SKPaint paint,
        float y, float espaciado, float centroX = Ancho / 2f)
    {
        var ancho = MedirTexto(paint, texto, espaciado);
        DibujarCaracteres(canvas, texto, paint, centroX - ancho / 2, y, espaciado);
    }

    private static void DibujarTextoIzquierdo(SKCanvas canvas, string texto, SKPaint paint,
        float x, float y, float espaciado = 0) =>
        DibujarCaracteres(canvas, texto, paint, x, y, espaciado);

    private static void DibujarCaracteres(SKCanvas canvas, string texto, SKPaint paint,
        float x, float y, float espaciado)
    {
        paint.GetFontMetrics(out SKFontMetrics metricas);
        var baseline = y - metricas.Ascent;
        foreach (var caracter in texto)
        {
            var segmento = caracter.ToString();
            canvas.DrawText(segmento, x, baseline, paint);
            x += paint.MeasureText(segmento) + espaciado;
        }
    }

    private static float MedirTexto(SKPaint paint, string texto, float espaciado) =>
        texto.Length == 0 ? 0 : texto.Sum(caracter => paint.MeasureText(caracter.ToString())) +
            ((texto.Length - 1) * espaciado);

    private static void DibujarLinea(SKCanvas canvas, float x1, float y1, float x2, float y2,
        string color, float grosor)
    {
        using var paint = CrearPaint(color);
        paint.StrokeWidth = grosor;
        canvas.DrawLine(x1, y1, x2, y2, paint);
    }

    private static SKRect ObtenerLimitesAlpha(SKBitmap bitmap)
    {
        var izquierda = bitmap.Width;
        var arriba = bitmap.Height;
        var derecha = 0;
        var abajo = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha == 0)
                    continue;
                izquierda = Math.Min(izquierda, x);
                arriba = Math.Min(arriba, y);
                derecha = Math.Max(derecha, x + 1);
                abajo = Math.Max(abajo, y + 1);
            }
        }
        return derecha <= izquierda || abajo <= arriba
            ? new SKRect(0, 0, bitmap.Width, bitmap.Height)
            : new SKRect(izquierda, arriba, derecha, abajo);
    }

    private static SKPaint CrearPaint(string color, SKTypeface? typeface = null) =>
        CrearPaint(SKColor.Parse(color), typeface);

    private static SKPaint CrearPaint(SKColor color, SKTypeface? typeface = null) => new()
    {
        Color = color,
        IsAntialias = true,
        Typeface = typeface,
    };

    private static SKBitmap CrearLienzo() =>
        new(Ancho, Alto, SKColorType.Rgba8888, SKAlphaType.Premul);

    private static SKTypeface CargarFuente(string ruta) =>
        SKTypeface.FromFile(ruta) ?? throw new FileNotFoundException($"No se pudo cargar la fuente: {ruta}");

    public void Dispose()
    {
        _regular.Dispose();
        _medium.Dispose();
        _bold.Dispose();
        _logo.Dispose();
        _firma?.Dispose();
        foreach (var gradiente in _gradientes.Values)
            gradiente.Dispose();
    }
}
