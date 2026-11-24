# GafetesDRFC

Aplicación local de consola para generar PNG y PDF de gafetes de Oficina y Campo.

## Requisitos

- .NET 8 SDK.
- `Assets/logo_blanco.png`.
- `Assets/fonts/Inter-Regular.otf`, `Inter-Medium.otf` y `Inter-Bold.otf`.
- `Assets/firma_direccion.png` (opcional; si falta, se registra una advertencia).
- Fotos del organigrama en `../assets/img/` (ya se consultan automáticamente).

`Foto` puede señalar un archivo específico dentro de `Fotos/`. Si no está ahí —o el campo está vacío—, el programa busca por `Nombre + Apellidos` entre las fotos ya organizadas bajo `../assets/img/`. Para nombres compuestos, se normalizan acentos y espacios. Los logos de cliente opcionales van en `Assets/Clientes/`; solo se aceptan archivos PNG para `ClienteLogo`.

## Ejecución

Desde esta carpeta:

```powershell
dotnet restore
dotnet run -- datos_ejemplo.csv
```

Para exportar también un PNG de depuración del frente de Oficina, con guías de color:

```powershell
dotnet run -- --debug datos_ejemplo.csv
```

El PNG adicional se guarda como `{IdEmpleado}_Frente_Debug.png` en `Salida/`. La validación fija del layout se ejecuta también sin `--debug`; rechaza textos que se salgan del área, se encimen o invadan la cinta inferior.

También se puede pasar un archivo `.xlsx`:

```powershell
dotnet run -- personal.xlsx
```

Los PNG, PDFs individuales, `Lote.pdf` y el registro de generación se guardan en `Salida/`. El registro solo muestra números de fila, categorías de error y advertencias; no escribe nombres, fotos, tipo de sangre ni datos de contacto.

## CSV

Archivo UTF-8 con encabezados `Nombre,Apellidos,Puesto,Area,IdEmpleado,Tipo,TipoSangre,ContactoEmergencia,TelefonoEmergencia,Foto`. `ClienteLogo` es opcional y aplica solo a `Tipo=Campo`. Nombre y apellidos se imprimen exactamente en dos líneas separadas.

`Foto` y `ClienteLogo` contienen solo el nombre del archivo, no una ruta. Si no se localiza una foto específica, se busca con `Nombre + Apellidos` en las carpetas del organigrama. Ante una foto ausente se dibuja una silueta y se registra una advertencia. Las filas inválidas o con error se omiten, sin detener la generación de las demás.

## Referencias de diseño

Las cuatro imágenes de comparación están en `Referencia/`. La salida se renderiza a 1275 × 2022 px por cara. La app y los recursos operan localmente; no se envían fotos ni datos a servicios externos.
