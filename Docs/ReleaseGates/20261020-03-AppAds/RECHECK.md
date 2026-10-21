# Comprobación pendiente

Restablecer la sesión de Codex antes de repetir las consultas: tanto el conector web como la revisión automática devolvieron errores de autenticación. No se intentó eludir la revisión.

Después, guardar respuestas nuevas en otra carpeta, sin sobrescribir esta evidencia:

1. Consultar `https://7583p.github.io/`, `https://7583p.github.io/app-ads.txt` y `https://7583p.github.io/robots.txt`. Registrar fecha, cadena de redirecciones, URL final, código HTTP, cabeceras y cuerpo.
2. Comparar las líneas del cuerpo público con `prepared/app-ads.txt`. Un error de conexión o del conector no acredita un 404 del sitio.
3. Comprobar acceso de rastreadores y la configuración pública del sitio del desarrollador. Contrastar las condiciones vigentes con la documentación oficial de AdMob, cuya consulta también quedó sin completar por el fallo del conector.
4. Distinguir disponibilidad pública de verificación efectiva en AdMob. El estado de rastreo debe acreditarse mediante la consola o evidencia del usuario; una consulta HTTP por sí sola no demuestra que AdMob haya rastreado el archivo.
5. Si falta el archivo, presentar la corrección preparada y solicitar autorización antes de publicarla. Si existe, revisar su contenido antes de proponer cambios y conservar las entradas válidas que ya contenga.

Estas consultas son lecturas públicas. No requieren cargar anuncios, hacer clic en anuncios ni modificar la ficha de Google Play o GitHub Pages.
