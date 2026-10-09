# FINAL BUILD

**Veredicto global: FAIL por conducción elite, Lancer en circuito y estabilidad variable del rendimiento.** La roca dorada está implementada y validada; no se afirma que por sí sola resuelva el balance.

Windows x86_64, Unity 6000.3.13f1, Development/QA. Build 17: **0 errores, 4 warnings** (tres usos heredados de Physics2D.autoSyncTransforms y aviso de Unity Services sin proyecto vinculado). GUID `7617d846f6b4411f90e26d6ba434986f`; Assembly-CSharp SHA256 `ce0931fbcfc0a26c9e64566a2d2ce8e64bba3f812cd49525e82b4a4c82bd1eca`. La identidad QA aísla saves/tienda. No se certifican transacciones reales.

Se continuó desde el working tree recibido: 1969 archivos preservados como referencia; 21 modificados, 2 nuevos, **0 eliminados, 0 cambios inesperados**. Adventure, física base, escenas, modelos, prefabs, Packages y ProjectSettings mantienen sus hashes. Rama `racing-climb-golden`, HEAD `e07e35657d0f0871aa8f07f7b88b35c2265168f3`. Sin commit/push/reset/clean.

# GOLDEN ROCK

Nuevo ID **16**, sin renumerar los anteriores. Apunta al rival de la posición inmediatamente anterior, bloquea ese objetivo al disparar y lo persigue directamente; el disparo trasero usa la posición inmediatamente posterior. Sin objetivo válido, conserva la carga. La protección existente absorbe el golpe y rompe la roca; los obstáculos físicos también pueden interceptarla. Usa el pool existente, sin crear entidades durante la carrera.

| Posición | Distribución |
|---|---|
| P1 | 65% monedas +10 / 35% roca normal; ninguna roca dorada ni otro poder |
| P2–P3 | 40% escudo / 40% monedas / 20% roca dorada |
| P4–P6 | 30% escudo / 30% monedas / 20% mini turbo / 20% roca dorada |
| P7–P12 | Roca dorada 12%; roca normal 6%; Lancer 8%; pesos restantes conservados |

Prueba nativa final: **81 PASS, 0 FAIL**, incluyendo selección por puesto frente a un señuelo más cercano, impacto frontal/trasero, escudo real, carga única, objetivo deshabilitado y pared. 5 disparos, 2 impactos reales y 1 bloqueo de escudo. Arte 3D dorado facetado y HUD GOLDEN ROCK: [captura final](../../RacingPhase2/closure/final-gold-lancer-final-01/04-golden-rock-held.png). El banner TWIN ROCK aún visible pertenece a la etapa anterior de esa fixture; la carga/miniatura actual es Golden Rock. Es una fixture dirigida, no una medición FPS.

# NIGHT CITY ELITE

Tres semillas en 9700 y tres en 10000, política heredada seleccionada y nuevos objetos. Carreras funcionales **timeScale4**, físicas reales y builds iguales; no se usan sus FPS. La matriz corresponde al build16; build17 conserva AI/física/pista/balance y cambia únicamente validación al exportar telemetría y su catálogo. [Proveniencia y gate estricto](elite-gate.json).

| MMR | Seed | Llegadas | DNF | Recoveries | P1→P12 s | Atajos completados/entradas |
|---:|---:|---:|---:|---:|---:|---|
| 9700 | 4207 | 7/12 | 5 | 19 | N/D | crest_cut: 18/23; return_drop: 29/29 |
| 9700 | 4208 | 12/12 | 0 | 17 | 42.86 | crest_cut: 20/28; return_drop: 32/32 |
| 9700 | 4209 | 9/12 | 3 | 13 | N/D | crest_cut: 23/27; return_drop: 30/31 |
| 10000 | 4207 | 11/12 | 1 | 10 | N/D | crest_cut: 24/29; return_drop: 26/26 |
| 10000 | 4208 | 12/12 | 0 | 15 | 23.72 | crest_cut: 24/29; return_drop: 26/28 |
| 10000 | 4209 | 11/12 | 1 | 16 | N/D | crest_cut: 19/23; return_drop: 26/27 |

**Ninguna carrera cumple el gate elite.** N/D significa que faltan finishers: no se sustituye P1→P12 por el spread de quienes sí llegaron. El cierre a 15 s tras terminar el jugador permanece intacto; no se alarga para esconder DNF. El passed de la suite antigua solo comprueba invariantes/UI y puede coexistir con DNF; no constituye aprobación elite.

Prueba rápida de rangos inferiores, build17, seed4207/timeScale4:

| MMR | Llegadas | DNF | Recoveries | Mejor vuelta s | Spread s |
|---:|---:|---:|---:|---:|---:|
| 100 | 12/12 | 0 | 7 | 120.50 | 13.78 |
| 1000 | 4/12 | 8 | 12 | 111.10 | N/D |
| 2500 | 12/12 | 0 | 7 | 99.36 | 22.94 |
| 5000 | 12/12 | 0 | 2 | 84.78 | 13.64 |
| 7500 | 5/12 | 7 | 12 | 75.68 | N/D |
| 9000 | 12/12 | 0 | 13 | 69.68 | 23.44 |

El ritmo de vuelta mejora con MMR. Las incidencias de 1000/7500/9000 se conservan; esta muestra no demuestra ausencia global de regresiones de balance. La política de conducción original no fue sustituida.

# RECOVERY ROOT CAUSES

Trazas circulares de **12 s / 121 muestras por incidente**, con nodo/ruta, decisiones, aire/salto, contactos, item hits y recovery. Clasificación de las 90 recoveries del control y las 46 del candidato:

| Contexto principal | Control seleccionado | Candidato descartado |
|---|---:|---:|
| shortcut entry | 29 | 6 |
| shortcut abort | 1 | 0 |
| shortcut exit | 1 | 0 |
| rejoin | 0 | 0 |
| drift exit | 8 | 0 |
| hop | 2 | 1 |
| landing | 19 | 19 |
| wall | 13 | 4 |
| open track edge | 1 | 4 |
| collision | 1 | 2 |
| traffic avoidance | 0 | 0 |
| checkpoint target | 0 | 0 |
| navigation target | 0 | 0 |
| boost timing | 0 | 0 |
| item reaction | 15 | 10 |
| Lancer | 0 | 0 |
| other | 0 | 0 |

[Cada incidente y su evidencia/confianza](final-gold-control-elite-01-classification.json). En el control, 19 casos de seed4207/9700 se revisaron individualmente y el resto se asignó mediante eventos y fase observada, con causalidad calificada; las 46 del candidato se revisaron individualmente. Un contexto temporal no prueba toda la cadena causal. ItemHit no identifica todavía el proyectil causante; no se atribuyen esos 15 casos específicamente a la roca dorada. Las principales concentraciones están en entrada de crest_cut, aterrizajes y contactos posteriores. No se declaran causales los segundos de estados que se solapan.

Las50 recoveries nuevas de las tres carreras a1× también tienen trazas y clasificación observacional por incidente: [Off inicial18](final-gold-off-9700-01-classification.json), [Shadow19](final-gold-shadow-9700-01-classification.json), [Off repetido13](final-gold-off-9700-02-classification.json). En Off repetido:4 entradas,2 aterrizajes,4 paredes,1 borde y2 reacciones a item. Son contextos respaldados por eventos, con confianza/método anotados; no se convierten en una causa única cuando falta la identidad del impulso.

# PACK COMPRESSION

Los cambios localizados de aproximación y protección del borde mejoraron seed4207 a 1× (8→6→4 recoveries; spread34.32→28.58→20.10 s), pero la prueba posterior de seis casos con el nuevo balance mostró regresiones. El experimento con histéresis/heading produjo un DNF y también se descartó.

| MMR | Seed | Control llegadas/recoveries | Candidato llegadas/recoveries | Spread control/candidato s |
|---:|---:|---:|---:|---:|
| 9700 | 4207 | 7/19 | 12/4 | N/D / 35.70 |
| 9700 | 4208 | 12/17 | 9/10 | 42.86 / N/D |
| 9700 | 4209 | 9/13 | 6/10 | N/D / N/D |
| 10000 | 4207 | 11/10 | 12/7 | N/D / 33.90 |
| 10000 | 4208 | 12/15 | 12/6 | 23.72 / 24.62 |
| 10000 | 4209 | 11/16 | 12/9 | N/D / 32.60 |

El candidato reduce recoveries agregadas 90→46, pero añade DNF en 9700/4208 y empeora 9700/4209. **Se descartó** conforme a la regla de no desplazar fallos. La fuente seleccionada solo añade dos getters diagnósticos a la política heredada; no hay ventaja de motor, masa, grip, boost o colisión por MMR. [A/B por semilla](ai-ab-comparison.json).

Exposición exclusiva observada, sumada sobre los 72 racers de la matriz seleccionada:

| Estado | Racer-segundos |
|---|---:|
| recovery_penalty | 77.40 |
| item_spin | 40.22 |
| wall_slow | 238.36 |
| racer_contact_slow | 282.24 |
| traffic_braking | 77.80 |
| boundary_correction | 0.00 |
| road_rejoin | 494.22 |
| shortcut_driving | 3883.38 |
| drift | 6293.75 |
| airborne | 656.50 |
| normal_driving | 3910.81 |
| other | 0.00 |

Son duraciones en estados, **no una resta causal de tiempo perdido**. Las trazas de contactos/rutas y estos tiempos priorizan el siguiente ajuste; no justifican inventar pérdidas exactas por categoría. La compresión a 3–8 s sigue pendiente.

# VERDANT

Build17, seed4207, 9700 MMR/timeScale4: **12/12**, 0 DNF, 10 recoveries, spread 41.06 s. Pista, tres rutas, césped y checkpoints se conservan. [Resultado completo](final-gold-verdant-02-summary.json). No se presenta como un benchmark FPS ni como un gate elite aprobado. El intento anterior01 inició por error la matriz predeterminada; se detuvo solo su proceso QA, restauró442 PlayerPrefs y quedó archivado sin usarlo como evidencia de9700.

# LANCER

**300 km/h, 6 segundos**, sobre la pista real; permanece el frenado en curvas y la reducción del 50% en atajos. Motor temporal compartido humano/bot: stats, colliders y masa originales conservados. Prueba final: 81 checks PASS; duración humano/bot 6.0000/6.0000 s, velocidad en recta 300.00–300.00 km/h. Pared de 0.35 m y vehículo dinámico a 300 km/h: 1/2 contactos reales, estado finito, sin atravesar el plano de pared. Transformaciones calientes: 0 B en el lote medido.

Prueba adicional de circuito real: **FAIL**, 8 PASS / 2 checks fallidos. 231 muestras, 158 con pista debajo, 1 recovery, pico 210.04 km/h. A los3.10s el offset supera12m; el coche sale del borde, cae y la recuperación compartida cancela Lancer a los4.62s. Máximo lateral73.09m/altura absoluta8.39m. No se usa una pista aérea alternativa: el fallo es mantenerse sobre la carretera real, no un fallo del temporizador de6s en recta. No se cambió la fixture para ocultarlo. [CSV real](../../RacingPhase2/closure/final-gold-lancer-circuit-01/lancer-real-circuit.csv). La traza no identifica el impulso inicial ni todos los contactos; esa parte causal queda pendiente. Se comprobó que Ai.Initialize instala el proveedor de comandos y RaceManager no lo reemplaza al terminar countdown.

# CONTROLS

Mini hop nativo final: **28 PASS**; altura 0.195 m, aire 0.140 s, redirección durante hop 7.219°. PC y touch conservan flanco único para hop, hold para drift, vista trasera y uso direccional sin doble disparo. Los 10 checks de dirección de items también pasan. La fixture de controles no sustituye una revisión humana de sensación.

# SHORTCUTS / CHECKPOINTS

Night City mantiene 192 nodos, crest_cut10→38 y return_drop74→94; gates ordenados {0,8,40,72,104,136,168,184}. Bordes abiertos108–132. Verdant mantiene224 nodos y gates {0,48,96,144,176,200,208}. Minimap solo de pista principal, sin rutas/beacons/carteles reveladores. Contratos dirigidos finales: 56 PASS. Ningún trigger ni geometría de pista se modificó en este cierre.

# ML

**ML02 permanece SHADOW, no autoritativo.** Mismo binario y semilla para Off/Shadow; override explícito al candidato cuyo SHA256 es `75dae7b848febed359ba1c359276360618cd48937e0736d716d5167d0480c59a`. 24263 inferencias en el contador global de forensics; modelos registrados: ML02/75DAE7B848FE. Hay **11 adapters, uno por bot**. PLAYER usa el autopiloto determinista del harness y no recibe adapter ML; por eso su metadata ML está vacía, no por el estado DNF. La media por llamada usa las medias de los11 adapters ponderadas por sus ticks, sin diluirla con PLAYER; el contador global/scopes por frame incluyen todos los adapters activos. La paridad histórica de 64 contextos/8 bandas, máximo ~4.18e−7, se reutiliza porque pesos/inferencia/33 features no cambiaron. Paridad no demuestra competitividad.

Defecto demostrado y corregido: el guard de telemetría conservaba el límite ordinario de 270 km/h y rechazó cuatro sesiones reales de Lancer. Ahora solo un evento registrado de uso de Lancer permite hasta324 km/h durante6 s +2 s de margen de inercia; el objeto sigue durando6 s. Se mantienen checks de discontinuidad, aceleración y orden. C#12/Python12 casos dirigidos PASS; ID16 mapea a la familia existente2 sin cambiar IDs0–15. Los fixtures sintéticos de validator están rotulados y no son datos de conducción/entrenamiento.

Los brutos anteriores no se reescribieron. Quedan rechazos conservadores por aceleración >120 m/s² y telemetría temprana de Verdant en cuarentena; no se entrenó ni se promovió el modelo. [Guard y familias](telemetry-guard-parity.json), [rechazos reales previos](lancer-telemetry-before.json).

Comprobación nueva en las carreras reales del build17: cinco sesiones con uso de Lancer, cuatro legalmente aceptadas. Una de ellas alcanza **296.07km/h** con tres muestras por encima del antiguo límite270 y pasa el guard real; todas esas muestras están dentro de la ventana del item registrado. La quinta sesión sigue rechazada por aceleración127.2m/s²; no se relajó ese filtro. [Sesiones reales después del cambio](lancer-telemetry-after.json).

# PERFORMANCE

Full game, Windows1600×900 High/AA4, 12racers, timeScale1, cámara y HUD normales, misma seed4207/MMR9700/build17, ML Off frente a ML02 Shadow. Sin captura binaria de profiler, screenshots, análisis externos ni sondeo de archivos durante la carrera. CSV/JSON y telemetría se exportan al terminar; instrumentación buffered. Alimentación/foco reales registrados en [comparación completa](performance-comparison.json); no se cambió configuración del equipo ni se cerraron procesos externos.

| Métrica | ML Off | ML02 Shadow |
|---|---:|---:|
| FPS medio | 268.02 | 451.77 |
| FPS mediano | 313.17 | 521.27 |
| 1% low FPS | 90.30 | 148.28 |
| 0.1% low FPS | 58.35 | 87.15 |
| p95 frame ms | 7.25 | 4.22 |
| p99 frame ms | 9.23 | 5.39 |
| máximo frame ms | 38.59 | 46.97 |

El orden fue **Off inicial → Shadow → Off repetido**, sin cambiar binario ni configuración. El Off inicial **no se descarta**: 170.69FPS medios, mediana191.88, 1%low55.33, 0.1%low28.53, p9510.29ms, p9913.86ms, máximo89.98ms. **FAIL** en esa pasada. La tabla principal muestra Off repetido y Shadow; no permite afirmar que ML acelere el juego ni que la estabilidad global esté certificada.

Las tres carreras mantuvieron foco en todas las muestras, AC=1/batería99%, vSync0 y targetFrameRate−1. La instantánea posterior, con juego cerrado, registróCPU7%/cola0/16lógicos ([host-after-final-modes.json](host-after-final-modes.json)); no es un monitor continuo de la carga durante las carreras. La instantánea entre modos no se obtuvo por restricción de sandbox; sus valores nulos no se usan como evidencia de carga. No se atribuye toda la variación a un proceso externo concreto sin traza.

| Contador / proceso | ML Off | ML02 Shadow |
|---|---:|---:|
| CPU main frame ms | 3.7131 | 2.2018 |
| CPU render thread frame ms | 1.9984 | 1.2286 |
| GPU ms (frames válidos) | 0.7509 | 0.5558 |
| Física ms | 0.1757 | 0.0629 |
| AI ms | 0.0599 | 0.0225 |
| ML tick ms/frame | 0.0000 | 0.0768 |
| ML inferencia microsegundos/llamada, metadata registrada | 0.0000 | 303.0835 |
| GC bytes/frame | 399.3172 | 386.9143 |
| GC máximo bytes/frame | 207540.0000 | 225180.0000 |
| GC collections (frames) | 3.0000 | 5.0000 |
| GC collect máximo ms | 3.0149 | 2.9264 |
| Pico RSS MiB | 583.2266 | 581.2148 |
| Pico privado muestreado MiB | 1634.1289 | 1631.5664 |

Gate 1%low≥60FPS: **Off inicial FAIL / Off repetido PASS / Shadow PASS**. Estabilidad global: variable, sin aprobación general. GPU timing: Off `True` / Shadow `True`; si no está disponible no se sustituye por tiempo CPU. Scopes CPU se solapan y no se suman. Render-thread-frame incluye esperas. No se usan totales all-thread con overflow para atribuir stalls.

Resultados 1× Off: 10/12, 2 DNF, 13 recoveries, spreadN/Ds. Shadow: 8/12, 4 DNF, 19 recoveries, spreadN/Ds. La física Unity no es un replay determinista; una diferencia de resultados no prueba autoridad de Shadow.

## FRAME PACING ROOT CAUSE

La evidencia histórica separó GC/IO de los grandes stalls: un frame de279.21ms acumuló14 pasos de física; otro de151.88ms registró104.65ms de Physics.Simulate/siete pasos; otro de122.05ms mostró98.10ms de espera Present/Semaphore. Esos frames no registraron GC. Fuera del juego hubo colas de57–62 hilos sobre16 lógicos y el usuario confirmó otros tres juegos en desarrollo. Esa competencia externa explica una fuente concreta de desplanificación, sin cuantificarla como causa única del antiguo p99 de195ms. El A/B histórico de diagnóstico y los counters tienen sus límites documentados en Logs/SkyHop/20261008/REPORT.md.

Nueva evidencia concreta del build17: Off inicial frame27936 **89.98ms**, con **80.67ms en Gfx.WaitForPresentOnGfxThread /80.66ms Semaphore** yGC0. El frame siguiente fue60.26ms, tres pasos físicos y17.64ms de Physics.Simulate. Shadow frame49179:46.97ms con38.55ms en espera de presentación. Off repetido frame42924: **38.59ms**, dos pasos físicos y **27.54ms Physics.Simulate**, GC0. Present/Semaphore y acumulación/sincronización de física son las fuentes principales observadas de esos picos. Los scopes se solapan; se conservan vecinos porque LastValue puede corresponder al frame completado anterior. El disparador del wait del driver/OS no está identificado de forma única; no se declara eliminado.

No es solo calentamiento inicial: Off inicial en90–120s cae a140.56FPS medios/43.18de1%low. Off repetido también varía entre ventanas. [Análisis completo por30s y picos con vecinos](final-gold-off-9700-01-frame-analysis.json), [Shadow](final-gold-shadow-9700-01-frame-analysis.json), [Off repetido](final-gold-off-9700-02-frame-analysis.json). El muestreo forense medio fue0.082ms en Off inicial y0.035ms en Shadow, muy inferior a los picos de presentación citados; no hubo capturas/exports periódicos durante esas carreras. GC máximo≈3ms, sin collections en los picos citados. No se modificaron física, frecuencia de ticks, HUD o calidad para maquillar los resultados.

La referencia limpia de este cierre alcanzó409.93FPS medios/150.92 de1%low sin cambiar física ni liga. Las cifras finales anteriores son mediciones nuevas completas, sin truncar frames lentos. Se conservan los peores8 frames por modo y sus contadores en performance-comparison.json. No se atribuyen stalls a la liga o miniaturas cuando sus contadores son cero; tampoco se declara resuelto retrospectivamente un stall sin traza correlacionada.

# 10K LEAGUE

| Coste durante carrera | ML Off | ML02 Shadow |
|---|---:|---:|
| leagueSimulatedEventsDuringRace | 0 | 0 |
| leagueSaveRequestsDuringRace | 0 | 0 |
| leagueSnapshotCopiesDuringRace | 0 | 0 |
| leagueRacePumpAllocatedBytes | 0 | 0 |
| meanLeaguePumpMs | 0.000993034802377224 | 0.0006316968938335776 |
| comboCapturesDuringRace | 0 | 0 |
| itemEntitiesCreatedDuringRace | 0 | 0 |

Core compacto existente preservado, 64 checks históricos PASS. Fuera de carrera, medición histórica del mismo core: matchmaking mediana1.2645ms/p9910.3110ms; top100 0.06577ms; vecindario14 0.00566ms; snapshot0.2642ms; save maduro12.1238ms; season completo9.8775ms. Memoria inicial core+índice≈1.229MiB. Son cifras históricas reutilizadas, no una nueva medición Unity/Android. No se escala a100k/1M.

# COMBO THUMBNAILS

Se conservan previews 3D completos coche+ruedas+piloto+accesorio,192×108, offscreen y caché compartida por combo para leaderboard/perfil. No hay modelo vivo por fila ni sustitución por iconos separados. Generación durante las dos carreras finales: **0/0**. No se reimplementó el sistema ya validado. [Ejemplo histórico de la columna de combos](../../RacingPhase2/closure/hill-combo-native-product-20261006-01/04-evolved-leaderboard.png): la imagen solo acredita el render de loadouts del06/10; su etiqueta antigua de Beyond a10000 no es la regla actual, cuyo umbral10001 sí se verifica en los contratos finales. Caché histórica:25generadas/16hits, capacidad96,1.424MiB de texturas; no es una nueva medición de menú.

# SAVE

Smoke focalizado: **32 PASS**, incluyendo journal real,10.000 IDs, save migrado/relaunch, cursor interrumpido137, reanudación e idempotencia, combos, títulos, peaks y entitlements. Wallet compartida Adventure/Racing comprobada en dos procesos del binario final; evidencia `../../RacingPhase2/closure/final-gold-wallet-final-01`. Todos los runners listados restauraron y verificaron byte a byte los **442 PlayerPrefs** originales. No se repitió la migración del producto.

# SEASON

Tres meses de calendario, procesamiento local incremental. **Primero curva de reset; después, si antes estaba en Beyond, cap del resultado a10000**. Ejemplos aprobados:10500→10200→10000;10500→9950 se conserva9950;10100→9600 se conserva9600. Peak histórico/persistent IDs se conservan. Exactamente10000 sigue ranked normal; Beyond empieza en10001. Se verificó reinicio durante transición, sin reset anticipado del humano.

# VIP

SKU **vip_monthly**, una única suscripción registrada, precio localizado/buy/pending/restore y supresión de anuncios conservados. Los contratos de producto 52 PASS y distribución/Lancer 26 PASS. Transacción vacía no concede VIP ni30d ni crea receipt. Serialización mantiene expiración VIP y adFreeUntil de30d tras compra calificada. No se fabricó ninguna compra exitosa: renovación, restore y cobro reales necesitan validación de tienda.

# ADVENTURE

Sin modificaciones respecto al working tree recibido. Smoke Countryside del binario final: recorrido UI normal, física finita, joints/contacts, avance/freno/reversa/aterrizaje; evidencia `../../RacingPhase2/closure/final-gold-adventure-01`. Se conservan los fallos Mountains conocidos de car-01/rally-01/sport-bike-01 (matriz histórica22/25); el smoke no los convierte en PASS ni altera tuning para ocultarlos.

# HUMAN GATE

`C:\Peru3124\restoredclimb\ClimbAnime\Builds\HumanGateLatest\HillAdventurer-HumanGate.exe`

Build GUID `7617d846f6b4411f90e26d6ba434986f`, binario y DLL verificados. Arranque nativo automatizado, Gold/Lancer/hop/wallet/circuit/Adventure en el binario final. Metadatos/manifiesto apuntan a build17; los seis metadatos08 anteriores quedaron archivados en [human-gate-metadata-before.zip](human-gate-metadata-before.zip). No se borraron otros builds. Revisión humana visual/de conducción pendiente.

# FINAL VERDICT

**FAIL.** La nueva roca dorada y los checks mecánicos dirigidos pasan; la conducción elite no cumple12/12 sin DNF/recoveries y spread3–8s. Lancer falla en el circuito real aunque pase la recta. El candidato que desplazaba fallos se descartó. Off inicial **FAIL**, Off repetido **PASS**, ML02 Shadow **PASS**, conforme al mínimo60FPS de1%low; no hay aprobación general de estabilidad por esa variación.

# REMAINING BLOCKERS

- Entradas/aterrizajes de crest_cut, impactos/rejoin y dispersión elite; ninguna de las seis carreras funcionales aprueba el gate.
- Lancer real: salida del borde y recovery que cancela el efecto a4.62s; no se garantiza permanecer en pista ni alcanzar300km/h en ese recorrido.
- Frame pacing variable: primer Off55.33de1%low; se identifican waits de presentación y catch-up físico, pero el disparador único del stall no queda demostrado/eliminado.
- El efecto neto de Golden Rock sobre compresión de pack no está demostrado mediante un A/B sin/con el nuevo objeto; no se promociona como solución del spread.
- DNF observados en pruebas1000/7500 y recoveries restantes de Verdant, sin alterar la política heredada ni Adventure.
- ML02 sigue Shadow; falta evidencia competitiva/telemetría válida suficiente y resolver rechazos conservadores por aceleración antes de entrenar/promover.
- Validación real de tienda VIP/pending/restore/renewal y revisión humana pendientes; fallos históricos Mountains separados.

Siguiente paso concreto: ampliar la traza QA de Lancer real con comandos, ruta y contactos para localizar el impulso de salida; corregir ese foco y/o uno reproducible de entrada/aterrizaje de crest_cut, evaluando la misma matriz antes/después. Conservar únicamente ajustes que no creen nuevos DNF. No retocar globalmente drift ni dar ventajas físicas. No escalar población.

Archivos cambiados respecto a la instantánea heredada:

- `Assets/AnimeHillRacing/Resources/RacingItemBalanceConfiguration.asset`
- `Assets/AnimeHillRacing/Scripts/Editor/RacingBalanceContracts.cs`
- `Assets/AnimeHillRacing/Scripts/Editor/RacingLancerContracts.cs`
- `Assets/AnimeHillRacing/Scripts/Editor/RacingProductContracts.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/AI/AiItemAgent.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/AI/AiVehicleController.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/Core/RaceManager.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/Diagnostics/RacingClosureProbe.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/Diagnostics/RacingLancerProbe.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/Diagnostics/RacingRaceMetrics.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/Items/ItemDistributionTable.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/Items/ItemEntityBase.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/Items/ItemInventory.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/Items/ProjectileItemEntity.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/Items/RaceItemType.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/Items/RacingItemBalanceConfiguration.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/Items/RacingItemPool.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/ML/DeterministicLegalityFilter.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/ML/RacingTelemetrySchema.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/Presentation/RacingItemArt.cs`
- `Assets/AnimeHillRacing/Scripts/Racing3D/UI/RacingEmblem.cs`
- `Assets/AnimeHillRacing/Scripts/Editor/RacingFinalSmokeContracts.cs`
- `Assets/AnimeHillRacing/Scripts/Editor/RacingFinalSmokeContracts.cs.meta`

Fuera de Assets, `Tools/ML/racing_dataset.py` incorpora familia16→2 y el guard acotado de Lancer; el runner agrega proveniencia binaria/balance. Evidencia completa en este directorio y `Logs/RacingPhase2/closure`. [Preservación](source-audit.json), [restauraciones](save-restorations.json).
