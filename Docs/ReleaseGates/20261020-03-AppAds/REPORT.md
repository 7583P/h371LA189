# Hill Adventurer: actualización del informe, 20 octubre 2026

**NOT READY.** El usuario confirmó `https://7583p.github.io/` como sitio web del desarrollador configurado en Google Play para `com.matias.hilladventurer`. El publisher solicitado coincide con las dos configuraciones locales de AdMob. La verificación HTTP pública y el descubrimiento por AdMob siguen **BLOCKED** por autenticación de las herramientas; no se obtuvo una respuesta HTTP del sitio.

Esta actualización complementa el [informe completo](../20261020-02/REPORT.md) y sus resultados. Conserva sin cambios el conjunto anterior de evidencias y su manifiesto. El bloqueo anterior «dominio desconocido» queda resuelto por la respuesta del usuario; no equivale a comprobar la disponibilidad pública del archivo.

| Comprobación | Resultado | Evidencia o límite |
| --- | --- | --- |
| Sitio del desarrollador en Google Play | CONFIRMADO POR USUARIO | `https://7583p.github.io/`; sin lectura independiente de Play Console o de la ficha pública |
| Publisher local | PASS | `pub-1413480107943515` en la configuración del juego y Google Mobile Ads; [hashes y coincidencias](local-configuration.json) |
| URL solicitada | PREPARADA | `https://7583p.github.io/app-ads.txt`, en el mismo origen que el sitio confirmado |
| Código HTTP del archivo | BLOCKED | No obtenido: el cliente local no pudo conectarse; la consulta web falló en la autenticación del conector |
| Contenido público y entrada exacta | BLOCKED | No se recibió el cuerpo del archivo; no se afirma ni presencia ni ausencia de la entrada |
| Redirecciones, robots y acceso de rastreadores | BLOCKED | Sin respuestas públicas verificables |
| Descubrimiento y estado de rastreo en AdMob | BLOCKED | Sin comprobación pública completa ni lectura del estado de AdMob |

Entrada esperada, facilitada por el usuario:

```text
google.com, pub-1413480107943515, DIRECT, f08c47fec0942fa0
```

Se preparó una [copia local de referencia](prepared/app-ads.txt). No demuestra que falte el archivo en GitHub Pages ni sustituye un archivo existente. No se modificó el sitio, otro proyecto o configuración de producción. Si una consulta posterior confirma que falta, esta entrada permite preparar la corrección concreta; publicarla requiere la autorización solicitada por el usuario.

Los [intentos y errores](query-attempts.json) distinguen el `401/token_revoked` del conector del código HTTP del sitio: ese error **no es una respuesta de GitHub Pages**. La revisión automática del comando HTTPS no pudo completarse porque la sesión requiere iniciar sesión de nuevo. La acción no se ejecutó; el mensaje indicó un fallo de autenticación, sin determinar que la consulta fuera insegura. Se solicitó restablecer la sesión para continuar.

La [matriz de gates actualizada](release-gates.json) conserva el resultado de release **NOT READY**: la matriz élite terminó con **69/72 llegadas, tres DNF y 16 recuperaciones**; también siguen pendientes rendimiento de la build actual, Android físico y aprobación visual/HumanGate. No se repitieron esas pruebas durante esta actualización ni se cambió su resultado. DAPEX azul/Whity y ML Off mantienen la decisión anterior.

La [verificación local](verification.json) comprueba las evidencias anteriores y los archivos de fuente/configuración registrados. Los archivos añadidos en esta continuación están limitados a esta carpeta de documentación. No hubo commit, publicación ni solicitud de anuncios reales.
