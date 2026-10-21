# Hill Adventurer — verificación y cierre local, 20 octubre 2026

**NOT READY.** Se corrigieron la búsqueda de corredores con nombres numéricos, la lectura QA de recibos Unity y las capturas que no acreditaban un salto. Compilaron Windows y Android; las suites relevantes terminan sin fallos. La matriz élite nueva termina en **69/72 llegadas, tres DNF y 16 recuperaciones**. Permanecen pendientes la comparación de rendimiento de la build actual, Android físico, app-ads.txt y aprobación visual/HumanGate.

Se trabajó en `main`, HEAD `152279fb5a4cf443a90d31654ad7a90f51e5abb3`, conservando el trabajo local previo. No hubo commit, push, pull, cambio de rama, publicación ni firma de producción. El usuario confirmó conservar **DAPEX azul/Whity**; esa decisión se aplicó y no constituye aprobación de la pista.

[Gates en JSON](release-gates.json) · [verificación automatizada](verification.json) · [arquitectura](ARCHITECTURE.md) · [cambios y rollback](CHANGES-AND-ROLLBACK.md) · [reproducción](REPRODUCE.md).

## Gates

| Gate | Resultado | Evidencia / límite |
| --- | --- | --- |
| Preservación de fuente, assets y datos productivos | PASS | [Integridad final](integrity-final.json): 32 escenas/prefabs, ocho artefactos/modelos anteriores, 20 archivos productivos, registro nativo y dos keystores conservados |
| Núcleo competitivo y persistencia | PASS | 29 contratos CPPS, 142 temporadas, 1047 experimentales, 145 lookup; cero fallos tras el cambio del lector QA |
| Lector QA Unity/CLI | PASS | [14 contratos](fixed-release-json/release-json-contracts.json) y [recibo físico leído](physical-readback/physical-league-readback.json); sin escritura de perfiles |
| Windows y suites Editor | PASS | [Build reviewed](windows-reviewed/build-size.json), [siete suites](windows-reviewed/rc-contracts.json), producto 52/0 y UI experimental 18/0 |
| Diseño visual Rainbow Road | BLOCKED | [Nueve comparaciones](rainbow-build/REVIEW.html); aprobación humana pendiente. Render D3D, navegación y colisiones comprobados |
| Captura de salto/aterrizaje observado | PASS | [Diez comprobaciones](capture-contracts.json), vuelo nodo 172 y aterrizaje 175 sin recuperación intermedia |
| Calidad élite oficial | **FAIL** | [Seis carreras](official-matrix-analysis/matrix.json): 69/72, tres DNF, 16 recuperaciones; gaps fuera de 3–8 s |
| Comparación nueva Off/ML02 Shadow | BLOCKED | [Intento](performance-gate.json) rechazado por otro Unity abierto; ningún proceso ajeno se cerró |
| Resultados y búsqueda numérica | PASS | [PlayMode 36/0](ui-after-numeric-fix/competitive-display-contracts.json), con widgets y capturas reales |
| Once rivales reales tras relanzar | PASS | [11/11 por ID y nombre](native-league-readback-fixed/native-league-readback.json), usando copia del mismo save físico |
| Economía y almacenamiento del simulador | PASS | [Tres laboratorios](statistical-readback/statistical-summary.json), presupuestos y recargas reconciliados. Alcance estadístico, sin aprobación de distribución aspiracional ni Android |
| Android: build y presupuesto calculado | PASS | [151.249.007 B de AAB](android-build/build-size.json); [147.980.603 B de descarga calculada](android-delivery/delivery-summary.json) en referencia ARM64/API35/en |
| Android físico / servicios | BLOCKED | [Cero dispositivos autorizados](android-device.json); controles, temperatura, FPS, ANR, GPGS, IAP, AdMob y SAF pendientes |
| app-ads.txt | BLOCKED | [Dominio efectivo no establecido](app-ads-gate.json); consulta pública sin resultado utilizable y pregunta al usuario pendiente |
| HumanGate Windows | BLOCKED | Build lanzada con ML Off; [checklist](MANUAL-QA.md). Falta confirmación humana expresa |

Los PASS de contratos, serialización o tamaño no conceden aprobación al gate físico, visual o de servicios. El PASS del smoke de carreras heredado no exige todas las llegadas; el analizador independiente rechaza correctamente esta matriz.

## Qué queda activo en la fuente destinada a producción

No se verificó el estado desplegado de Play Console ni se publicó esta revisión. En la fuente del Player se conservan HA-PW2, doce corredores (humano + once bots), conciliación pairwise a cero antes de límites y recibos idempotentes. La habilidad oculta sigue separada del rating. Placements iniciales 0/3 y clasificación estacional 0/1 mantienen sus condiciones distintas.

Season 1 comienza el 2026-10-01 00:00 UTC; se mantienen trimestres naturales, reset Hermite y títulos Top/ASSASSINER. Producción conserva **10.000 bots locales y ML Off**. Los dos modelos anteriores conservan sus hashes. AFTER ≥3000 /100 y otro reset permanecen fuera del Player; 100k sigue opt-in QA. La apariencia DAPEX azul/Whity queda preservada por decisión expresa del usuario.

La presentación orbital está implementada como propuesta `--rainbow-preview` en Editor/Development. Sus bandas, marcos, refuerzos, flechas y cielo son geometría/presentación original. El trazado físico conserva 192 nodos, ocho checkpoints, dos shortcuts y 31 colliders. Se mantienen idénticos los hashes de navegación y colisiones en las nueve cámaras A/B. La versión 1.2 / code 20 y configuraciones de anuncios, GPGS, atlas y manifiestos permanecen intactas; véase [comprobación de configuraciones](external-configuration-integrity.json).

## Defectos cerrados en esta ejecución

| Defecto reproducido | Cambio y regresión |
| --- | --- |
| El rival real **211** se encuentra por ID, pero su nombre se interpreta como posición | `RacingLeaderboardUI.Locate` prioriza identidades existentes; `#211` busca posición. [Antes: diez nombres de once](native-league-readback/native-league-readback.json), [PlayMode rojo: 29/1](ui-before-numeric-fix/competitive-display-logic.json), después 36/0 y 11/11 en proceso nativo. No se añadieron rivales |
| El lector .NET falla al cargar outcomes del recibo Unity | El lector QA acepta arrays numéricos y base64 previo, conservando el writer CLI. Catorce contratos nuevos y readback del mismo journal pasan. JsonUtility y la fórmula del Player conservan su implementación |
| Una etiqueta por nodo se presentaba como prueba de salto | `RacingClosureProbe` usa nombres de lugar correctos y exige vuelo observado, salto reciente y aterrizaje sin respawn intermedio. [PNG de vuelo](independent-race/mmr-10000/seed-4301/08-main-jump.png), [PNG de aterrizaje](independent-race/mmr-10000/seed-4301/08-main-jump-landing.png) y JSON verificables |
| Herramientas dirigían snapshots a la ejecución anterior o podían reemplazar reanálisis | Destinos nuevos validados, guard de main, rechazo de sobrescrituras, exportación legible de logs PowerShell y comparación efectiva del registro/keystores |

Los arreglos anteriores de caché de mallas, resultados definitivos, índice de identidades y preparación Ranked ya estaban en el árbol inicial. Se preservaron y revalidaron; su reproducción anterior está en [el registro previo](../20261019-01/WORKLOG.md). El listado exacto de esta ejecución contiene **12 archivos de fuente/herramientas**, once modificados y uno nuevo, en [session-changes.json](session-changes.json).

## Carreras físicas

Matriz oficial: misma build congelada, MMR 9700/10000, seeds 4207/4208/4209, High, 1600×900, escala 1, ML Off efectivo y cero inferencias. Humano controlado por un script QA; no representa una demostración humana experta. Cero errores de ejecución y física compartida en los seis casos.

| MMR | Seed | Llegadas | DNF | Recuperaciones | Gap 1.º–12.º (s) |
| --- | --- | --- | --- | --- | --- |
| 9700 | 4207 | 12/12 | 0 | 1 | 19,29 |
| 9700 | 4208 | 12/12 | 0 | 5 | 15,23 |
| 9700 | 4209 | 10/12 | 2 | 3 | No existe: DNF |
| 10000 | 4207 | 12/12 | 0 | 4 | 20,12 |
| 10000 | 4208 | 11/12 | 1 | 1 | No existe: DNF |
| 10000 | 4209 | 12/12 | 0 | 2 | 20,02 |

El histórico original tenía 72/72, cero DNF y siete recuperaciones, con gaps 15,20–27,06 s. Esta nueva tanda no demuestra mejora causal: la conducción no se modificó y la física/scheduling no son bit deterministas por seed. Se conservan ambos resultados.

Las 16 recuperaciones son salidas del circuito; ocho observan nodos 23–26 en su ventana final. Se exportan **1.728 filas de sectores para 72 corredores**, sin muestras ni contactos descartados. Los DNF ocurren al cerrar a los 15 s de la llegada humana; los tres corredores aún estaban en pista. No se alteró ese plazo ni se inventó su tiempo final. El [triage completo](RACE-TRIAGE.md) incluye comparación histórica, posiciones y distancias pendientes.

La semilla independiente **4301** terminó 12/12, cero DNF, tres recuperaciones y gap **30,24 s**. El stress Ranked **4302**, con rivales persistentes de habilidad heterogénea y humano QA de dificultad explícita, terminó **1/12 y once DNF**, tres recuperaciones. Su recibo y búsquedas pasan; su resultado no acredita calidad élite ni se mezcla con la matriz oficial. [Datos adicionales](supplemental-races.json).

La causa inmediata del cierre está comprobada; la contribución causal de contactos, drift, rutas e items a las salidas/gaps continúa abierta. No se ha demostrado que 3–8 s sea físicamente imposible. El siguiente experimento mínimo es aislar anticipación lateral/tráfico en 23–26 y estabilidad del rejoin de crest_cut, con A/B y rollback. Esos candidatos no están activados. No se cambiaron coches, controles, boosts, física, IA, cronómetros ni standings para ajustar cifras.

Las builds de matriz, capturas y búsqueda corregida conservan sus artefactos separados y hashes en [artifact-lineage.json](artifact-lineage.json). La matriz no se reetiqueta como ejecutada sobre una build posterior.

## Rendimiento ML

La repetición sobre la build actual quedó bloqueada antes de comenzar por el Unity ajeno PID 2796. Se preservó el proceso y el intento. Los 35 tests del pipeline ML pasan, sin entrenamiento ni promoción.

El par histórico válido de 60 s fue **recalculado**, con catorce inputs inalterados y resultados idénticos. Corresponde a una build anterior: [procedencia](performance-historical-reanalysis/provenance.json), [comparación](performance-historical-reanalysis/performance-comparison.json). La ventana Off sin foco del informe original continúa descartada.

| Modo histórico | FPS medio | 1% low | 0,1% low | p95 / p99 ms | Inferencias | µs/inferencia |
| --- | --- | --- | --- | --- | --- | --- |
| Off | 332,52 | 149,73 | 100,41 | 4,65 / 5,64 | 0 | 0 |
| ML02 Shadow | 306,54 | 119,84 | 69,91 | 5,42 / 6,79 | 6710 | 332,89 |

| Modo histórico | Main / render ms | Física / IA ms | GC B/frame | Pico working MiB | GPU |
| --- | --- | --- | --- | --- | --- |
| Off | 2,995 / 2,127 | 0,092 / 0,048 | 419,84 | 574,27 | No disponible |
| ML02 Shadow | 3,249 / 2,206 | 0,092 / 0,057 | 423,98 | 559,77 | No disponible |

Ambas ventanas históricas tienen foco completo, corriente y configuración estables, sin pérdidas de muestras. Los scopes se solapan y no se suman. Es un único par observado; no acredita ventaja ML ni el rendimiento de la build nueva o de Android. Producción permanece Off.

## Temporadas, supply y población

Dos laboratorios orgánicos del simulador, **10.000 bots durante cuatro temporadas**, ejecutan **4.851.958 settlements**. Cero humanos simulados, lobbies inválidos o fallos de conservación; las recargas coinciden. No se modificó fórmula ni reset. [CSV por temporada](season-economy.csv), [resumen y presupuestos](economy-summary.json), [distribuciones de rangos y recarga](statistical-readback/statistical-summary.json).

| Seed | Carreras | Media Q4 | P50 | P95 | P99 | Máximo | Supply antes / después del reset Q4 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 4207 | 2.437.355 | 2474,46 | 2409 | 4905,05 | 5985,42 | 7225 | 24.744.588 / 22.971.381 |
| 4208 | 2.414.603 | 2460,26 | 2393 | 4970,10 | 6043,03 | 7281 | 24.602.598 / 22.838.320 |

Con supply inicial cero, seed 4207 reconcilia `0 + 5.477.331 floor + 24.665.073 placements − 7.171.023 reset = 22.971.381`; seed 4208 reconcilia `5.575.003 + 24.373.687 − 7.110.370 = 22.838.320`. Pairwise neto, retirada por techo y migración son cero en ambas. Los percentiles de la tabla son **antes** del reset; las bandas del readback son carry **después**, al comenzar Season 5, con clasificación estacional pendiente.

No aparecen bots por encima de 10000 en esos Q4. En seed 4207 solo 84/10000 pertenecen a la banda superior de habilidad registrada; el presupuesto y la compresión estacional también están medidos. Son indicios de escasez, sin atribución causal aislada ni prueba de equilibrio infinito. La distribución aspiracional continúa sin acreditar; no se introducen cuotas ni rating por actividad.

El escenario **mobility**, separado y explícitamente sintético, usa 1200 bots, intervención de habilidad/rating y 252.346 carreras. Sus trayectorias de subida/caída están en los JSON; no representa progresión orgánica ni personas reales.

| Población QA, un día | Carreras reales del scheduler | Crear / simular ms | Guardar / cargar ms | Heap adicional MiB | Journals B | CPU ms |
| --- | --- | --- | --- | --- | --- | --- |
| 10.000 | 6282 | 141 / 650 | 243 / 65 | 2,73 | 1.843.209 | 1218,75 |
| 100.000 | 65.238 | 1190 / 2949 | 244 / 82 | 27,28 | 18.507.210 | 4687,50 |

Ambos benchmarks conservan presupuesto, identidad y recarga; cero carreras humanas. Son costes de este host y de un día, con registro compacto de 172 B/bot. La presencia de 100k es sintética, Local Offline y opt-in QA. Rendimiento móvil y activación productiva siguen pendientes.

## Android y HumanGate

AAB forense nuevo: **151.249.007 bytes**. Bundletool 1.17.2 calcula **147.980.603 bytes** de descarga ARM64/API35/en en densidades 160/240/320/420/480/560/640: margen **2.019.397 bytes** respecto al presupuesto de 150.000.000. AAB, contenido desempaquetado, descarga e instalación tienen medidas distintas. Esta referencia no sustituye Play por dispositivo.

Se usó la identidad debug existente, sin firma productiva, instalación ni subida. Los hashes de ambas claves y la configuración original coinciden al finalizar. No hay Android autorizado conectado; quedan pendientes controles, cámara, carrera completa, reinicio, FPS/temperatura, ANR/crashes, calidad visual, GPGS, compras de prueba, AdMob autorizado y exportación SAF.

HumanGate está lanzado en la ventana **Hill Adventurer**, build `Builds/ReleaseGates-20261020-02-Reviewed/HillAdventurer-HumanGate.exe`, con ML Off e identidad BlacklineQA. [Instrucciones de revisión](MANUAL-QA.md). La sesión manual guarda evidencia nueva en `Docs/ReleaseGates/20261020-02-HumanGate/`. La aprobación visual, controles, botones, historial y diálogo de exportación Windows requieren confirmación expresa después de probarlos.

## Preservación, archivos y decisiones pendientes

Los snapshots iniciales están en `tmp/ReleaseGates/20261020-02/before/`, junto a copias privadas de saves y registro. [Inventario inicial](baseline-inventory.json) y [entorno](environment.json) identifican los hashes. El registro productivo conserva SHA-256 `f4f6aa9a89407079a058cac6980b4d42c22870231d8f9113ef5d4299007365a7`; los 20 archivos productivos y dos keystores coinciden. La serialización automática de QualitySettings se detectó y restauró con snapshot; se conserva también el fallo preliminar. No se restauraron saves productivos para obtener un PASS.

[Cambios exactos de esta ejecución](session-changes.json), [rollback por archivo](CHANGES-AND-ROLLBACK.md), [logs exportados](logs/manifest.json) y `SHA256SUMS.json` permiten revisar fuente y evidencia. Los archivos nuevos quedan en una carpeta versionable; no se modifica el índice de Git ni se crea un commit.

Decisiones que siguen requiriendo autorización: AFTER ≥3000 /100; otro reset; activación productiva de 100k; aceptación visual y HumanGate; flujos reales de servicios cuando exista dispositivo; firma, subida y publicación. El dominio configurado de app-ads.txt necesita identificarse. La conservación de DAPEX azul/Whity ya fue confirmada y está documentada en [user-decisions.json](user-decisions.json).
