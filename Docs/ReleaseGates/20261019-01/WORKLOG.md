# Hipótesis, pruebas y rollback

## Presentación orbital (fase 1)

Hipótesis: bandas cromáticas estables, bordes luminosos continuos, señales que apuntan a la curva real, arcos y refuerzos tridimensionales y un horizonte orbital mejoran la lectura respecto al material prismático oscuro sobre ciudad. Es una propuesta original de geometría visual. No se cambian los 192 nodos, perfil físico, anchuras, colliders, checkpoints, shortcuts, respawns, cámaras de juego ni récords. No hay evidencia que justifique alterar todavía el trazado físico.

Activación: únicamente `--rainbow-preview` en Editor/Development. La rama de producción devuelve `false`; la presentación anterior sigue disponible en el mismo binario QA sin ese argumento. No se declara aprobación visual.

A/B: nueve cámaras iguales por variante en `rainbow-validated/before` y `after`, incluyendo curva, salto, landing y llegada; dos aproximaciones host de iluminación diurna/calidad Low. `REVIEW.html` conserva los PNG originales, sin retocar píxeles. Navegación SHA-256 `3e48f9f323ec15290c763ebf8c56ca4d3c4cd937acf123ea5afadaf7b76240da`; colisiones `ebe301a28f360c54a3c1a222550aeb5f52284c1cd8bf10aefc884d07c3242266`; ambos iguales antes/después, 31 colliders. La primera revisión fallida queda en `rainbow-review`; no es la revisión final validada.

Rollback de la propuesta: omitir `--rainbow-preview`. Para revertir la integración de fuente, guardar primero la revisión actual y restaurar `CanyonRushBuilder.cs` desde el snapshot `tmp/ReleaseGates/20261019-01/before/`. Los archivos nuevos pueden permanecer inactivos; no es necesario borrar assets ni evidencia.

## Mallas desaparecidas al recargar escenas (defecto reproducido y corregido)

Reproducción A: capturar dos circuitos en escenas nuevas y ejecutar contratos de producto con D3D. Los ítems 1/6/8/14 incumplieron la geometría mínima. `item-diagnostics` muestra mallas válidas inicialmente y, tras abrir otra escena, `Combine mesh instance … is null` y una excepción al consultar `sharedMesh.vertexCount`. El fallo se conserva en los logs originales de `tmp/ReleaseGates/20261019-01/` y en los JSON fallidos versionables.

Causa comprobada: siete entradas de la caché de primitivas de `RacingArt` devolvían el wrapper de una malla nativa destruida sin comprobar el null de Unity. Los materiales ya comprobaban ese caso. Cambio mínimo B: `cached != null` en las siete factorías existentes, reutilizando sus generadores originales. No cambian vértices, materiales, colores, ruedas, suspensión, estadísticas ni colliders cuando la malla sigue viva.

Regresión B: el mismo orden capturas → contratos ahora da producto 52/0, siete suites RC sin fallos y UI experimental 18/0; build Windows Development `Succeeded`, 0 errores, 4 warnings. Las seis carreras de esta build se registran por separado. No se interpreta este arreglo de render como una mejora causal de IA.

Rollback: snapshot original de `Presentation/RacingVehicleVisuals.cs` en `tmp/ReleaseGates/20261019-01/before/Assets/AnimeHillRacing/Scripts/Racing3D/Presentation/`. Respaldar primero el archivo actual. No usar git reset/checkout/clean ni borrar evidencia. Revertir recupera también el defecto reproducido.

## Recuperaciones y variación de la reproducción (fase 2)

El artefacto anterior se verificó por hash y volvió a ejecutarse en 10000/4207, ML Off, High, escala 1: 12/12, 0 DNF, 0 recuperaciones, frente a 4 registradas históricamente. Misma seed no acredita determinismo de la física, tiempos de inicialización ni igualdad del frame scheduling. No se atribuye una mejora causal a esta diferencia.

Los siete anillos históricos se reanalizaron en `prior-recovery-analysis.json`: seis eventos Outside the course y uno Vehicle stuck. Los sectores/nodos y los contadores de contacto son observaciones; no hay un contrafactual que asigne segundos perdidos a contactos, items o rutas.

La primera tanda nueva se ejecuta en seis casos secuenciales con su binario congelado. Tiene métricas por corredor, sectores, rutas, telemetría de conducción y anillos de recuperación. Su comando inicial no incluyó `--elite-forensics`, por lo que no se le atribuyen CSV nuevos de contactos completos. Se corrigió el lanzador para próximas tandas; cualquier ampliación con esa instrumentación se identifica como tal y no sustituye en silencio esta evidencia.

No se han modificado IA, velocidad, boosts, física, cronómetros ni standings para intentar ajustar gaps. El primer caso nuevo 9700/4207 dio 15,81 s y ya incumple 3–8 s. Esto acredita un fallo medido, no una demostración de imposibilidad física del objetivo.

## Resultados y búsqueda persistente (fase 4)

Hipótesis A: la tabla de resultados usa MMR/placement anteriores mientras el resumen anima el recibo definitivo; el leaderboard solo permite posición/ID. Los contratos PlayMode `ui-before-play-2` reproducen siete fallos de producto. Los primeros intentos de harness (`ui-before`, `ui-before-play`) también se conservan y se distinguen de defectos del juego.

Cambio B: tabla y badges consultan el recibo final confirmado; sin recibo no muestran un cambio de MMR y señalan UNSETTLED. Se añade un índice perezoso sobre los nombres e IDs existentes, sin crear competidores ni tocar el settlement. Los nombres repetidos conservan sus coincidencias; un ID desambigua. Las 14 filas reutilizadas y la paginación siguen vigentes.

Regresión: `ui-after-play` 21/0 verifica lógica; sus capturas prematuras no acreditan presentación. `ui-after-rendered` espera frames y el tiempo real de animación: 27/0, seis capturas legibles de los widgets reales. Son fixtures de UI identificados, no carreras ni ratings orgánicos. `lookup-contracts` añade 145/0 en 10k y 100k, con reservas, settlement, recarga, duplicados y búsqueda ausente sin fabricación de perfiles.

Rollback: snapshot inicial de `RacingResultsUI.cs` y `RacingLeaderboardUI.cs`; el nuevo índice queda sin uso al restaurar el consumidor. Guardar primero cualquier revisión posterior y volver a ejecutar los contratos. No restaurar ni migrar saves para revertir UI.

## Rendimiento controlado (fase 3)

La condición inicial que impedía medir queda registrada en `performance-gate.json`; no se borra ni se presenta como estado final. Más tarde no había ningún Unity externo bajo carga. No se cerró ningún proceso ajeno. Se midieron Off y ML02 Shadow con el mismo binario final, seed 4207, MMR 10000, High, 1600×900, escala 1, corriente y plan de energía estables: dos ventanas completas de 60 s, todos los frames con foco, cero pérdidas de muestras.

`focused-performance-20261020-041114-1a968eaa/performance-comparison.json` acepta la pareja. Off: 332,52 FPS, 0 inferencias. Shadow: 306,54 FPS, 6710 ticks y 332,89 µs por inferencia. Es una pareja observada, sin atribución causal universal ni ventaja de rendimiento ML. GPU no disponible. Los scopes CPU se solapan y no deben sumarse; los contadores de GC por frame y por proceso tienen semánticas distintas. Producción permanece Off; no hubo entrenamiento ni promoción.

## Laboratorios y almacenamiento (fase 4)

Dos simulaciones nuevas de 1200 bots durante cuatro temporadas ejecutan 518898 settlements orgánicos del simulador. El caso mobility añade 252346 con intervención sintética de habilidad/rating inicial y se informa por separado. Los presupuestos reconciliados y recargas son válidos en los tres. No son conducción física ni una prueba de equilibrio de 100k a largo plazo; el umbral AFTER observado en los JSON es hipotético.

Los procesos aislados de un día para 10k/100k miden el scheduler real, disco, heap y CPU del host. No hay actividad humana simulada, migración productiva ni activación de 100k. No se extrapolan los tiempos a Android. Las reglas/reset de producción y los dos modelos conservan su contenido.

## Android forense y protección (fase 5)

Se elimina la lectura automática de contraseñas de producción del constructor forense. Se usa únicamente la identidad debug existente; no se instala ni se distribuye ese AAB. La configuración temporal de backend/arquitecturas se restaura en finally. El constructor Windows también rechaza un ejecutable ya existente.

AAB actual: 151255078 bytes; build Succeeded, cero errores, 29 warnings. Bundletool 1.17.2 calcula 147979553 bytes para ARM64/API35/en en densidades 160/240/320/420/480/560/640: margen 2020447. El tamaño de build-summary incluye contenido desempaquetado y no representa descarga. Los warnings de API obsoleta/licencia no impidieron la compilación; el log completo se conserva. No se modifican versionName 1.2 ni versionCode 20.

No hay Android conectado. Controles, temperatura, ANR, servicios, compras, anuncios y SAF en dispositivo siguen BLOCKED. El dominio efectivo de app-ads.txt no está acreditado por la configuración/evidencia disponible; no se inventa uno. No hubo publicación, subida ni firma de producción.

## Decisión explícita del usuario

El HEAD contiene META DAPEX azul oscuro con ruedas Whity, distinto del Formula amarillo/cyan indicado en el TXT. El usuario confirmó «Conservar el coche actual y documentar la discrepancia». Se conserva la apariencia actual completa. El arreglo de caché reutiliza las mismas mallas originales; no cambia su diseño. La aprobación visual de la pista y el HumanGate siguen pendientes.
