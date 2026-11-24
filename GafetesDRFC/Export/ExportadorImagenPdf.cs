using SkiaSharp;

namespace GafetesDRFC.Export;

public static class ExportadorImagenPdf
{
    public const float AnchoPdf = 153.02f;
    public const float AltoPdf = 242.65f;

    public static void GuardarPng(SKBitmap bitmap, string ruta)
    {
        using var datos = bitmap.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidDataException("SkiaSharp no pudo codificar el PNG.");
        using var archivo = File.Create(ruta);
        datos.SaveTo(archivo);
    }

    public static void GuardarPdfEmpleado(SKBitmap frente, SKBitmap reverso, string ruta)
    {
        using var archivo = File.Create(ruta);
        using var documento = SKDocument.CreatePdf(archivo);
        AgregarPagina(documento, frente);
        AgregarPagina(documento, reverso);
        documento.Close();
    }

    public static void AgregarPagina(SKDocument documento, SKBitmap bitmap)
    {
        var canvas = documento.BeginPage(AnchoPdf, AltoPdf);
        using var paint = new SKPaint
        {
            IsAntialias = true
        };
        canvas.DrawBitmap(bitmap, new SKRect(0, 0, bitmap.Width, bitmap.Height),
            new SKRect(0, 0, AnchoPdf, AltoPdf),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), paint);
        documento.EndPage();
    }
}

public sealed class ExportadorPdfLote : IDisposable
{
    private readonly FileStream _archivo;
    private readonly SKDocument _documento;
    private bool _cerrado;

    public ExportadorPdfLote(string ruta)
    {
        _archivo = File.Create(ruta);
        _documento = SKDocument.CreatePdf(_archivo);
    }

    public void AgregarEmpleado(SKBitmap frente, SKBitmap reverso)
    {
        if (_cerrado)
            throw new ObjectDisposedException(nameof(ExportadorPdfLote));
        ExportadorImagenPdf.AgregarPagina(_documento, frente);
        ExportadorImagenPdf.AgregarPagina(_documento, reverso);
    }

    public void Dispose()
    {
        if (_cerrado)
            return;
        _documento.Close();
        _documento.Dispose();
        _archivo.Dispose();
        _cerrado = true;
    }
}
