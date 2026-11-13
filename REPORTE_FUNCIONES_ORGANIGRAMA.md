# Reporte de funciones del Organigrama DRFC

## Propósito

El organigrama digital permite consultar la estructura de DRFC de forma visual e interactiva. Además de mostrar las relaciones de reporte, facilita localizar personas, consultar información laboral, revisar cuadrillas, preparar gafetes y exportar datos.

## Funciones principales

### 1. Explorar la estructura organizacional

- Presenta las relaciones jerárquicas mediante tarjetas y líneas de conexión.
- Permite expandir o contraer ramas con el control **+ / −** de cada responsable.
- Incluye controles para expandir todo el organigrama o regresar a una vista resumida de tres niveles.
- Alterna entre orientación vertical y horizontal.
- Permite desplazarse arrastrando el organigrama, acercar o alejar el zoom y centrar la vista.
- Distingue áreas operativas y administrativas mediante distintivos y colores.

### 2. Buscar y filtrar personas

- Busca personas por nombre o cargo.
- Al encontrar a alguien, resalta su tarjeta y expande la ruta jerárquica necesaria para localizarla.
- Filtra la estructura por área o por tipo de área.
- Muestra el total de personas que coincide con la vista y los filtros.

### 3. Consultar fichas individuales

- Abre una ficha con la fotografía disponible, nombre, cargo, área, tipo de área, ID de empleado y correo.
- Presenta información de jefe directo, preparación académica, fecha de ingreso, fecha de nacimiento, tipo de sangre y antigüedad cuando está capturada.
- Muestra el próximo aniversario laboral y cumpleaños cuando las fechas están registradas.
- Permite actualizar los campos de Recursos Humanos disponibles desde **Editar datos**.
- Desde la ficha se puede generar el gafete de la persona.

### 4. Consultar cuadrillas

- De forma predeterminada, reemplaza las tarjetas individuales de quienes tienen `agrupado: true` por una tarjeta resumen **Cuadrillas** bajo su responsable.
- Resume el total y los conteos de Supervisor, Jefe de Cuadrilla, Técnico A, Técnico B y Ayudante General, junto con un indicador visual proporcional.
- Al abrir el resumen, muestra el listado completo, ordenado por categoría y por nombre. Cada persona permite abrir su ficha.
- Permite exportar la lista de esa cuadrilla a CSV.
- El interruptor **Mostrar cuadrillas completas** presenta las tarjetas individuales de las personas agrupadas; la vista advierte que puede ser extensa.
- Las personas agrupadas siguen incluidas en la búsqueda, los filtros, la tabla, las fichas y la exportación general.

### 5. Consultar la información en tabla

- Cambia del organigrama visual a una tabla con datos de identificación y de trabajo.
- Permite ordenar la tabla por sus encabezados.
- Cada fila abre la ficha de la persona.
- Permite exportar a CSV la vista filtrada.

### 6. Administrar e intercambiar datos

- Permite importar un archivo CSV para actualizar datos de personas existentes.
- Permite exportar todos los datos a CSV, incluidos los campos de agrupación de cuadrillas.
- El panel **Puestos y vacantes** permite agregar y editar puestos, asignar un responsable, cerrar o reabrir vacantes y exportar el listado a JSON.
- Los cambios hechos en las fichas se conservan en el almacenamiento local del navegador.
- Los puestos y vacantes también se guardan en el navegador; todavía no se comparten entre equipos hasta conectar una API o base central autorizada.
- La exportación CSV sirve como respaldo y como medio para trasladar los datos a otro navegador o equipo. Importar el archivo vuelve a cargar los cambios.

### 7. Revisar aniversarios laborales y cumpleaños

- Agrupa los aniversarios laborales y los cumpleaños de hoy, los próximos siete días y el mes actual.
- El botón **Cumpleaños** muestra todas las fechas de nacimiento capturadas, ordenadas por su próxima fecha, con acceso al borrador de felicitación por correo.
- Permite exportar a CSV las personas cuyo aniversario laboral corresponde al mes.
- Si la persona tiene correo capturado, **Enviar felicitación** prepara un mensaje apropiado para el aniversario o cumpleaños en la aplicación de correo predeterminada; no envía el correo automáticamente.

### 8. Preparar e imprimir gafetes

- Genera una vista previa individual con frente y reverso.
- El frente muestra fotografía, nombre completo, puesto, área, ID de empleado, vigencia, logo y nombre de la empresa; no muestra el correo.
- El reverso incluye tipo de sangre, contacto de emergencia, espacios de firma, dirección de la empresa y la leyenda de que el gafete es personal e intransferible.
- Permite elegir un estilo estándar, de ahorro de tinta o con banda azul.
- Permite descargar el frente y el reverso como imágenes PNG, además de conservar la descarga editable en HTML.
- Incluye herramientas para ajustar el encuadre de la fotografía y para imprimir varios gafetes en hojas tamaño carta.

### 9. Imprimir o guardar el organigrama

- Permite imprimir o guardar la vista como PDF desde el diálogo de impresión del navegador.
- La hoja impresa incluye la marca de DRFC y presenta las tarjetas de cuadrillas con su desglose.

### 10. Presentación y accesibilidad

- Ofrece modo claro y oscuro.
- Se adapta a pantallas de distintos tamaños.
- Los controles principales tienen etiquetas para facilitar su uso con teclado y tecnologías de asistencia.

## Guion sugerido para un video informativo

### Apertura

**Imagen:** Vista completa del organigrama.

**Narración:**  
“Conoce el Organigrama DRFC: una herramienta interactiva para explorar la estructura de la empresa, consultar personas y facilitar tareas de Recursos Humanos.”

### Escena 1: Navegar la estructura

**Acción en pantalla:** Expandir una jefatura con el botón **+**, contraerla con **−**, cambiar orientación y usar zoom.

**Narración:**  
“Explora las relaciones de reporte y abre únicamente las áreas que necesitas consultar. Puedes contraer cada rama, cambiar la orientación y acercar o centrar la vista.”

### Escena 2: Encontrar a una persona

**Acción en pantalla:** Buscar por nombre y luego aplicar un filtro de área.

**Narración:**  
“Encuentra personas por nombre o cargo, o filtra por área y tipo de área. El organigrama resalta las coincidencias y muestra la ruta para ubicarlas.”

### Escena 3: Consultar una ficha

**Acción en pantalla:** Abrir una tarjeta y recorrer la ficha.

**Narración:**  
“Cada ficha reúne los datos disponibles de la persona, como su cargo, área, jefe directo y antigüedad. Los campos que Recursos Humanos captura pueden editarse desde la misma ficha.”

### Escena 4: Mostrar cuadrillas de forma resumida

**Acción en pantalla:** Abrir la rama de Samuel Ferrel Silva en Mantenimiento de red y seleccionar la tarjeta **Cuadrillas**.

**Narración:**  
“Para facilitar la lectura de equipos numerosos, las cuadrillas se presentan resumidas por categoría. Al abrir la tarjeta puedes consultar el listado ordenado, entrar a cada ficha o exportar la lista.”

**Acción opcional:** Activar **Mostrar cuadrillas completas**.

**Narración:**  
“Cuando se necesita ver toda la estructura individualmente, el interruptor muestra las tarjetas completas; esta vista puede ser extensa.”

### Escena 5: Tabla y exportación

**Acción en pantalla:** Cambiar a **Tabla**, ordenar una columna y usar la exportación.

**Narración:**  
“La vista de tabla ofrece otra forma de revisar y ordenar los registros. También puedes exportar todos los datos o solo los que coinciden con los filtros.”

### Escena 6: Aniversarios y gafetes

**Acción en pantalla:** Mostrar el panel de aniversarios y la vista previa de un gafete.

**Narración:**  
“Consulta aniversarios laborales y prepara gafetes con frente y reverso. La felicitación abre un borrador en el programa de correo; el envío requiere confirmarse desde esa aplicación.”

### Cierre

**Narración:**  
“El Organigrama DRFC conecta la estructura visual con herramientas de consulta, actualización y exportación. Para conservar o trasladar cambios, exporta los datos a CSV.”

## Recomendaciones para la grabación

- Usa datos de demostración o verifica que tienes autorización para mostrar la información en el video.
- Evita exponer IDs de empleado, correos personales, números de seguridad social y contactos de emergencia; difumínalos o utiliza registros de prueba.
- Para mostrar cuadrillas, una demostración recomendada es **Mantenimiento de red → Samuel Ferrel Silva → Cuadrillas**.
- Presenta el aviso de que la edición se guarda en el navegador y que se debe exportar el CSV para tener un respaldo.
- La disponibilidad de algunos contenidos, como aniversarios y la habilitación de felicitaciones, depende de que las fechas de ingreso y los correos estén capturados.
