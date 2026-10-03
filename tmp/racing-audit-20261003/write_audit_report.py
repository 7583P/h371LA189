"""Write the audit report only after all final evidence is complete."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
AUDIT = ROOT / "tmp/racing-audit-20261003"
CLOSURE = ROOT / "Logs/RacingPhase2/closure"


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def number(value, digits=2):
    return "N/A" if value is None else f"{value:,.{digits}f}"


def table(headers, rows):
    return "\n".join(["| " + " | ".join(headers) + " |", "| " + " | ".join("---" for _ in headers) + " |"] +
                     ["| " + " | ".join(str(cell).replace("|", "\\|") for cell in row) + " |" for row in rows])


def main():
    evidence = read(AUDIT / "final-evidence.json")
    if not evidence["allEvidenceComplete"]:
        raise SystemExit("Final validation is incomplete; the report will not claim completion.")
    measured = read(AUDIT / "final-analysis/measured-summary.json")
    baseline, ranked, profile = (measured[key] for key in ["baseline", "ranked", "profile"])
    curves = read(CLOSURE / evidence["labels"]["contracts"] / "skill-and-meta-curves.json")["rows"]
    rows = measured["benchmark"]
    by_mmr = {row["mmr"]: row for row in rows}
    fast = 100 * (1 - by_mmr[100000]["meanBestLap"] / by_mmr[20000]["meanBestLap"])
    gain = 100 * (1 - ranked["meanBestLap"] / baseline["meanBestLap"])
    recovery_gain = 100 * (1 - ranked["recoveriesTotal"] / baseline["recoveriesTotal"])
    curve_table = table(["MMR", "Línea", "Drift", "Atajos: conocimiento", "Consistencia", "META base", "META observado"],
                        [[row["mmr"], number(row["line"], 3), number(row["drift"], 3), number(row["knowledge"], 3),
                          number(row["consistency"], 3), number(row["metaProbability"] * 100, 1) + "%",
                          number(row["observedMetaRate"] * 100, 1) + "%"] for row in curves])
    benchmark_table = table(["MMR", "Mejor vuelta media (s)", "Vuelta media (s)", "km/h", "Recuperaciones / IA", "Atajos / IA", "Mini-turbos / IA", "Salto-truco-aterrizaje / IA"],
                            [[row["mmr"], number(row["meanBestLap"]), number(row["meanLap"]), number(row["meanSpeedKmh"]),
                              number(row["recoveriesPerBot"]), number(row["shortcutCompletionsPerBot"]),
                              number(row["miniTurbosPerBot"]), number(row["jumpTrickLandingChainsPerBot"])] for row in rows])
    mechanics_table = table(["MMR", "Contactos / piloto-min", "Drift (s) / IA", "Saltos / IA", "Trucos / IA", "Landing boosts / IA", "Ítems / IA", "Offroad (s) / IA"],
                            [[row["mmr"], number(row["contactsPerRacerMinute"]), number(row["driftSecondsPerBot"]),
                              number(row["jumpsPerBot"]), number(row["tricksPerBot"]), number(row["landingBoostsPerBot"]),
                              number(row["itemsPerBot"]), number(row["offroadSecondsPerBot"])] for row in rows])
    render_rows, proof_rows = [], []
    for directory in sorted((CLOSURE / evidence["labels"]["profile"]).glob("seed-*")):
        metrics, proof = read(directory / "metrics.json"), read(directory / "render-proof.json")
        render_rows.append([metrics["seed"], metrics["frames"], number(metrics["measuredSeconds"]), number(metrics["averageFps"]),
                            number(metrics["p95FrameMs"]), number(metrics["p99FrameMs"]), number(metrics["onePercentLowFps"]),
                            number(1000 / metrics["minimumFps"])])
        proof_rows.append([metrics["seed"], proof["renderedFrames"], f'{proof["width"]} × {proof["height"]}',
                           proof["antiAliasing"], proof["canvases"], proof["targetCreated"]])
    render_table = table(["Seed", "Frames medidos", "Segundos", "FPS medios", "p95 ms", "p99 ms", "1% low FPS", "Peor frame ms"], render_rows)
    proof_table = table(["Seed", "Frames renderizados incl. preparación", "Resolución", "MSAA", "Canvas", "RT creado"], proof_rows)
    test_table = table(["Suite final", "PASS", "FAIL"], [[name, result["passed"], result["failed"]] for name, result in evidence["assertions"].items()])
    race_table = table(["Validación final", "Carreras", "PASS", "Errores runtime"],
                       [[key, evidence["runs"][key]["races"], evidence["runs"][key]["racePasses"], evidence["runs"][key]["errors"]]
                        for key in ["benchmark", "ranked", "profile", "qa"]])
    reasons = {
        "AiItemAgent.cs": "Timing táctico, reservas de turbo y decisiones por amenaza/rango.",
        "AiRacerProfile.cs": "Habilidades explícitas de trucos y encadenamiento.",
        "AiSkillModel.cs": "Curva continua de MMR, personalidad y pequeñas imperfecciones.",
        "AiVehicleController.cs": "Dirección por velocidad/slip, frenado anticipado, tráfico, defensa y rutas comprometidas.",
        "RaceManager.cs": "Test MMR sin persistencia, benchmark de build uniforme y coches terminados sin colisiones.",
        "RaceRules.cs": "Ventana de llegada de 15 s y opción explícita de benchmark uniforme.",
        "RacingSceneBootstrap.cs": "Telemetría automática restringida a Editor/development.",
        "RacingClosureContracts.cs": "Comprobaciones reales de ítems, trayectorias, recovery y colisiones.",
        "RacingClosureProbe.cs": "Matriz MMR, métricas separadas, perfil High real y evidencia reproducible.",
        "RacingFinalQa.cs": "Fixtures físicos correctos, UI Shop y medición de combustible sin pickups accidentales.",
        "RacingRaceMetrics.cs": "Contadores físicos y de cadenas, percentiles, costes y reserva de muestras antes de GO.",
        "CanyonRushBuilder.cs": "Night City, cuatro rutas reconstruidas, corredores libres, color, tráfico y batching espacial.",
        "RacingJumpPad.cs": "Contabiliza saltos reales y cadenas mediante la física compartida.",
        "RacingTrackGeometry.cs": "Rutas Bézier tangentes y ribbons abiertos con volumen continuo.",
        "ItemEntityBase.cs": "Descarta objetivos que ya terminaron.",
        "ItemInventory.cs": "Visibilidad/muzzle de misil, tiempos de adquisición y consultas sin arrays transitorios.",
        "RacingItemPool.cs": "Consulta cacheada de proyectiles amenazantes.",
        "MatchmakingService.cs": "Adopción probabilística, alternativas competitivas y diversidad por seed.",
        "MetaProgressionConfig.cs": "Curva 0–91% y pool de alternativas competitivas.",
        "RacingQualityScope.cs": "Notificación de calidad seleccionada y estado visual consistente.",
        "RacingVehicleVisuals.cs": "Amarillo legible, cian en ruedas completas y púrpura con emisión controlada.",
        "RacingSaveData.cs": "Getters que respetan Jeep/ruedas guardados y override temporal de pruebas.",
        "CheckpointTrigger3D.cs": "Entrada/estancia con validación de orden y avance real.",
        "RacingTrackInfo.cs": "Identidad Night City coherente.",
        "TrackNode.cs": "Distancia acumulada para planificación de pista y rutas.",
        "TrackProgressTracker.cs": "Finalización legal y soporte de recorrido/checkpoints.",
        "TrackSplineGraph.cs": "Proyección y distancias reutilizables para planificar y anticipar rutas.",
        "RacingDesign.cs": "Contraste de botones, selección y lenguaje visual común.",
        "RacingGarageUI.cs": "Estado META/custom y selección/tuning representados correctamente.",
        "RacingHUD.cs": "Lobby actualizado por cambios, briefing correcto, mapa sin boxing y HUD conciso.",
        "RacingMainMenuUI.cs": "Cuatro destinos visibles, foco Adventure y contenido acorde al producto.",
        "RacingModeSelectUI.cs": "Hub, presets/inspección de 12 corredores y test temporal que preserva rango.",
        "RacingResultsUI.cs": "Resultados de prueba sin recompensas/delta/tier fingidos.",
        "RacingShowroom.cs": "Iluminación aislada de previews y restauración de máscaras de luces.",
        "RacingVehiclePhysics.cs": "Grounding, hop de 3,6 m/s, dos trucos, landing boosts, contadores y finish ghost compartidos.",
        "VehicleRecovery.cs": "Desplazamiento neto para stuck y límite exterior coherente con barreras.",
        "RacingClosureTests.cs": "Integra contratos nuevos con las suites existentes.",
        "RacingClosureValidation.cs": "Invoca explícitamente los cinco vehículos × cinco mapas en el runner existente.",
    }
    source_rows = [[f'`{path}`', reasons.get(Path(path).name, "Soporte del pase de Racing.")]
                   for path in evidence["changedFromOriginalWorkingTree"]]
    source_rows += [
        ["`Assets/AnimeHillRacing/Scripts/UI/CoinPackShopUI.cs`", "Suscripciones sólo cuando el panel está activo; guard de UI destruida."],
        ["`Assets/AnimeHillRacing/Scripts/UI/GemPackShopUI.cs`", "Evita listeners estáticos de paneles nunca activados y referencias destruidas."],
        ["`Assets/AnimeHillRacing/Scripts/Racing3D/Diagnostics/RacingImprovementContracts.cs` (+ meta, nuevo)", "Curvas, física igual, geometría y UI read-only verificadas."],
        ["`Assets/AnimeHillRacing/Scripts/Racing3D/Diagnostics/RacingRenderedPlayerProbe.cs` (+ meta, nuevo)", "Render completo explícito y capturas fuera del intervalo medido."],
        ["`Assets/AnimeHillRacing/Resources/RacingNightSky.shader` (+ meta, nuevo)", "Gradiente, estrellas, twinkle y haze con un sky shader Built-in."],
        ["`tmp/phase2-closure/Run-UnityValidation.ps1`", "Modos de validación protegidos, headless clasificado y player renderizado oculto."],
        ["`tmp/racing-audit-20261003/`", "Baseline, hashes, compilación, análisis CSV/JSON y scripts de informe."],
        ["`Docs/RacingAudit-20261003.md`", "Auditoría y resultados finales verificables."],
    ]
    changed_table = table(["Archivo respecto al working tree inicial", "Cambio"], source_rows)
    text = f"""# Hill Adventurer — Racing audit + improvement pass

Workspace: `C:/Peru3124/restoredclimb/ClimbAnime`. Unity **6000.3.13f1**, rama **racing-climb-golden**, HEAD **24d9cefceaa60706275e2ccdfdcd1e42c2cf24b7**. La auditoría partió del working tree existente, incluyendo sus 11 archivos Racing ya modificados. Baseline, diff original y hashes se conservan en `tmp/racing-audit-20261003/baseline`. No se hizo commit ni push.

## AUDIT

Se inspeccionaron PreStart/loading, Main Menu, entrada Adventure/Racing, Hub, Garage, matchmaking offline, reglas/rango/guardados, cámara, física, AI, ítems, jumps/tricks, cuatro rutas, Night City, UI y rendimiento. Se siguieron los entry points y llamadas reales. El renderer actual es Built-in. `CanyonRushBuilder` mantiene su nombre de clase histórico y construye Night City.

| Prioridad | Hallazgo comprobado | Resolución |
| --- | --- | --- |
| P0 | Endpoints e índices de atajos no coinciden; el viaducto omite un checkpoint obligatorio. El follower abandona rutas al agotarse el turbo o persigue puntos en orden incorrecto. | Reconstrucción tangente, orden, compromiso hasta salida, puertas legales y pruebas geométricas/físicas. |
| P1 | Skills casi saturadas a 20k; riesgo/agresión sin progresión; poca predicción de tráfico; contravolante usa el signo de steering en vez del drift bloqueado. | Curva continua, brake envelope, slip/yaw feedback y decisiones de pack. |
| P1 | Boost improvisado, grounded prematuro, resets de trucos, coches terminados que bloquean la pista. | Recursos reservados, contacto real, cadenas verificadas y finish ghost. |
| P1 | Recuperación basada sólo en velocidad no detecta contacto oscilante; margen exterior de 18 m permite quedar en laderas junto al túnel. | Desplazamiento de 3 m/timeout compartido; margen exterior de 2 m con excepción de rutas reales. |
| P1 | Lectura de selección reemplaza Jeep/ruedas guardados; Test MMR escribe el rango; META forzado a 7–8 bots. | Lecturas sin escrituras, sesión de prueba temporal, probabilidad + diversity guard. |
| P1 | Shop conserva listeners de paneles nunca activados y accede a Text destruidos al recoger gems, rompiendo eventos de GameOver. | Dos guards mínimos de ciclo de vida; recorrido Adventure probado después. |
| P2 | UI anuncia funciones inexistentes, briefing Canyon Rush, calidad sin selección y botones con contraste débil. | Navegación visible y contenido offline/Night City coherente. |
| P2 | Tráfico plegado en scenery estático; ventanas repetitivas, poca señalización y sombras innecesarias. | Animación de raíces reales, variedad, landmarks, batching espacial y geometría emisiva sin sombras. |
| P2 | Telemetría confunde spinouts de ítems/contactos y offroad/atajos; FPS acelerados o player oculto sin render resultan inválidos. | Contadores tipados, completions reales y cámara+HUD renderizados cada frame a timeScale 1. Telemetría excluida del release. |
| P3 | Exceso de texto interno y presentación META lavada por emisión/iluminación superpuesta. | Copy compacto, previews aislados y materiales legibles. |

En las tres carreras baseline de 100k hubo **112 recuperaciones de 33 bots** y sólo **17/33 bots finalizaron**. Las notificaciones de impacto antiguas incluyen ítems: no permiten comparar choques físicos con la instrumentación nueva.

## FIXES

Se corrigieron los defectos de rutas, control AI, grounding/boosts, recuperación, coches terminados, selección guardada y rango de pruebas. Se completó el pase de Night City/UI y se añadieron diagnósticos reproducibles. El inventario siguiente compara archivos con el snapshot inicial, para distinguir este trabajo de los cambios del usuario que ya existían.

{changed_table}

## AI

La AI emite los mismos `VehicleCommands` que el jugador y usa `RacingVehiclePhysics`, `FinalRacingStats`, masa, superficies, ítems y recuperación compartidos. Se comprobaron los nueve stats y la masa de los 12 coches. No se introdujeron modificadores físicos por MMR.

El controlador anticipa la curva mediante cinco muestras y un horizonte de frenado de hasta 95 m. Usa dirección de velocidad, slip y yaw; suaviza steering según habilidad y compensa el drift usando su dirección bloqueada. El turbo aumenta el objetivo en rectas; las curvas futuras mantienen el límite seguro. La estrategia corre a **10 Hz**, desfasada entre bots; el control de comandos continúa por FixedUpdate.

Prepara saltos, alinea aterrizajes, libera mini-turbos y hace hasta dos trucos en aire real. Mantiene una ruta tras expirar el boost, reserva recursos al aproximarse y cuenta salida completada, sin inferirla de offroad. Las pequeñas variaciones de perfil y de ejecución siguen sembradas por RaceSeed. Los contratos reproducen perfiles y lobbies; no se promete simulación PhysX bit a bit entre distintas cadencias de frames.

## AI AWARENESS

Tres carriles candidatos reciben costes por posición lateral, velocidad relativa y posición futura de rivales. El bot mantiene una maniobra durante 1,1–1,8 s para limitar lane ping-pong, frena si no hay salida, cambia preparación de curva, protege el interior cuando llega presión trasera y ajusta riesgo según agresión. Considera posición, última vuelta, anchura, checkpoint siguiente, recursos y amenazas de misil.

Los misiles exigen objetivo visible y muzzle libre; los shields reaccionan a amenaza/densidad, las spikes a rival detrás/apex, y la shockwave sólo a rivales dentro de 8 m. El turbo se guarda para una entrada ejecutable o una salida controlada. Los contadores de passing/defense miden **decisiones e intentos**, no adelantamientos completados.

En el lote ranked final de 100k: **{ranked['passingDecisionsTotal']:.0f} decisiones de pase**, **{ranked['defensiveDecisionsTotal']:.0f} de defensa**, **{ranked['trafficBrakingDecisionsTotal']:.0f} de frenado por tráfico** y **{ranked['boostsForShortcutsTotal']:.0f} boosts usados para atajos**. Los contactos siguen existiendo y se reportan debajo; la evitación no separa coches mediante fuerzas magnéticas.

## MMR SKILL CURVE

`t = clamp(MMR/100000)^0.86`, con crecimiento asintótico pequeño sobre 100k y límites **0,985**. La variación de habilidad baja de ±0,045 a ±0,012; quedan imperfecciones y temperamentos distintos. Agresión y riesgo progresan sin ser idénticos entre bots. Trucos, ejecución de ruta, timing de ítems y chaining tienen curvas propias.

Promedios de **64 seeds de perfil y lobby por cada uno de 13 ratings**:

{curve_table}

0–10k conserva líneas/timing débiles y apenas usa mecánicas avanzadas; 20–40k introduce trucos y luego drift; 50–70k aprovecha algunas rutas; 80–125k combina recursos, rutas, drift, saltos y aterrizaje. La evidencia es una tendencia agregada; tráfico e ítems impiden exigir monotonicidad por seed.

## META ADOPTION

La probabilidad base de Formula Apex/plasma sube de 0% a 90% en 100k y 91% en 125k. Se decide probabilísticamente por MMR/seed. El guard limita exact META a **9 de 11 AI**, conservando al menos dos alternativas; no impone el antiguo mínimo rígido de 7–8. Desde 70k los builds Standard se convierten en opciones competitivas; desde 90k las alternativas visibles usan Rally, Sport Bike u Orbit competitivos.

Se muestrearon **832 lobbies y 9.152 selecciones AI**, con repetición idéntica por seed. La tasa observada en la tabla ya incluye dispersión de MMR y diversity guard: en 100k es aproximadamente {curves[11]['observedMetaRate']*100:.1f}%, equivalente a {curves[11]['observedMetaRate']*11:.2f} exact META por lobby. El benchmark de conducción fuerza el mismo build y se etiqueta `ControlledEqualBuild`; no se usa para medir adopción.

## FORMULA APEX

Se conservó el amarillo canario `(1, 0.98, 0.02)` y se corrigió su respuesta de material/iluminación. Carrocería amarilla, banda/rim/sidewalls de rueda completamente cian, conductor/traje/casco retro púrpura y brazos hacia el volante son visibles en los renders. La emisión queda controlada y las luces del showroom se aíslan de las del mundo.

La identidad visual de Formula permanece reconocible con otras ruedas, mientras el Garage y los stats reflejan su tuning real: el color cian no convierte un build custom en META. La configuración actual exact META es **Formula Apex + plasma**.

## DRIVING

El jugador conserva su vía de input. Gas y dirección touch llegan a la física en ≤120 ms; se verificaron brake y reverse reales. El hop compartido usa **3,6 m/s** y el grounding requiere distancia ≤1,10 m y velocidad normal <1,5 m/s, evitando borrar un salto que está subiendo. Dos trucos como máximo y los landing boosts dependen del aterrizaje real.

La recuperación mide avance neto, detecta contacto oscilante y permite que un coche libre rápido siga conduciendo sin falsos resets. La AI intenta reverse antes de recuperar. Al terminar, un coche deja de bloquear o atacar; los controles/colliders se restauran al reactivar la carrera. Los casos de drift, pared/rival, jumps y recovery se probaron con cuerpos físicos reales.

Cámara: **{evidence['camera']['frames']} observaciones**, 16 escenarios, **0 poses inválidas, 0 coche fuera de vista, 0 obstrucciones**. Incluye recta, velocidad alta, curvas, drift, túnel, puente, salto, aterrizaje, finish y fixtures de reverse, hairpin de 180°, recovery y contactos. El hairpin controlado verifica framing; no prueba una curva de 180° inexistente en Night City.

## SHORTCUTS

Las cuatro rutas se reconstruyeron con 25 puntos Bézier tangentes, decks abiertos volumétricos y entradas/salidas que coinciden con el grafo de 160 nodos. Barreras, torres y túneles respetan el corredor real; los gaps y ramps se colocan en segmentos rectos. Los contratos comprueban superficie, giros, endpoints, clearance del cuerpo y orden obligatorio de checkpoints.

| Ruta | Nodos de entrada/salida | Planificación | Papel |
| --- | --- | --- | --- |
| RIDGE CUT | 80 → 110 | Boost, skill ≥0,42 | Ruta fuerte de suelo rough; ahorro geométrico >30 m. |
| SKYWAY LINK | 50 → 70 | Boost + salto, skill ≥0,62 | Ruta elevada/gap, alternativa al salto y tráfico de la vía principal. |
| WATERFALL LINK | 25 → 46 | Boost, skill ≥0,52 | Entrada rough y recorte más moderado. |
| APEX VIADUCT | 113 → 134 | Boost + salto, skill ≥0,70 | Recorte fuerte >30 m sin omitir checkpoints 112/136. |

Se planifica hasta 150 m antes, se prepara la entrada a 65 m y se compromete sólo con recurso, alineación, grounded y gate válido. Una ruta se intenta como máximo una vez por vuelta. Se retrasa el boost hasta estar alineado en su deck; la salida requiere proximidad real y suelo. La mejora principal fue hacer aprovechables estas rutas existentes, no sumar un desvío redundante.

En las 3 carreras ranked finales: **{ranked['shortcutCompletionsTotal']:.0f} completions de {ranked['shortcutVisitsTotal']:.0f} entradas**, **{ranked['shortcutCompletionsBots']}/{ranked['bots']} bots** usaron alguna ruta, **{ranked['miniTurbosTotal']:.0f} mini-turbos**, **{ranked['jumpTrickLandingChainsTotal']:.0f} cadenas salto-truco-aterrizaje** y **{ranked['driftJumpChainsTotal']:.0f} cadenas mini-turbo→salto**. Esta última cadena es oportunista y menos frecuente que la de salto/truco/landing.

## VISUAL DESIGN

Night City usa sectores con naranja, verde, violeta y teal; ventanas warm/cyan/pink/yellow, coronas y alturas distintas, arrows de boost/ruta, guías de ápice, iluminación de túnel/puente y landmarks. Se anima tráfico lejano mediante raíces visibles y billboards a 5 Hz. La ciudad se combina por material y celdas espaciales de 160 m para conservar culling; geometría emisiva evita sombras repetidas.

El nuevo sky shader Built-in aporta estrellas, twinkle, gradiente y haze. Luz de luna neutra, ambient tri-light y fog de densidad 0,00145 mantienen profundidad y pista legible. Las capturas finales muestran los materiales, la ciudad y el HUD reales; el pase usa geometría/shaders del proyecto y efectos baratos.

## UI

Main Menu expone **ADVENTURE, RACING / 3D MODE, GARAGE y SHOP**; Adventure conserva el foco inicial. PreStart/loading y navegación real llegan a ambos modos. Se eliminan promesas falsas de multiplayer/drafting y alias invisibles de tests. Hub/briefing muestran Night City; calidad tiene selección cian visible. Botones brillantes usan texto de alto contraste y Garage diferencia META/custom.

HUD mantiene posición/lap/timer/ítem/drift/minimap y reduce trabajo del lobby a cambios de reveal. Results ranked persiste una vez; el modo test presenta rango/recompensas preservados y evita delta/tier ficticios. Test MMR ofrece **1k, 10k, 25k, 50k, 75k, 90k, 100k y 125k**, reroll por seed y 12 filas con MMR, build, vehículo/ruedas, probabilidad, línea/frenada/drift, shortcuts, ítems, consistencia, chaining y agresión. DRIVE TEST usa un override temporal y Hub lo limpia.

## PERFORMANCE

**Dos carreras completas GO→Results**, 12 corredores, 100k MMR, ciudad, ítems, VFX y HUD; **timeScale 1**, **High**, **1600×900**, **MSAA 4×**, D3D11, development player. Hardware: **{profile['hardware']['processor']} / {profile['hardware']['graphics']}**. La producción mantiene su límite de 60 FPS; el diagnóstico lo desactiva para medir margen.

Una ventana Hidden puede omitir rendering; se rechazó esa medición anterior. El player final ejecuta su cámara Built-in y el HUD hacia un RenderTexture cada frame, con contador explícito de CPU/render, cientos de draw calls y render-proof. Se capturan PNG antes de GO y después de Results, sin readback durante el intervalo medido. El método usa la API oficial [Camera.Render](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera.Render.html).

{render_table}

Media ponderada: **{profile['frames']:,} frames**, **{profile['seconds']:.2f} s**, **{profile['averageFps']:.2f} FPS**, frame **{profile['meanFrameMs']:.3f} ms**, CPU main **{profile['meanMainThreadMs']:.3f} ms**, física **{profile['meanPhysicsMs']:.3f} ms**, AI **{profile['meanAiMs']:.3f} ms**, render CPU **{number(profile['meanRenderMs'],3)} ms**, **{profile['meanDrawCalls']:.1f} draw calls / {profile['meanBatches']:.1f} batches por frame**.

GC medido, incluida instrumentación: **{profile['meanGcBytes']:.0f} bytes/frame**; las muestras se reservan antes de GO para evitar crecimiento artificial del profiler. Hubo **0 Instantiate, 0 Destroy, 0 entidades de ítem creadas durante la carrera y 0 peticiones perdidas del pool**, en ambas seeds. Las listas/collider queries y decisiones de tráfico evitan arrays transitorios. Telemetría automática está excluida de release.

{proof_table}

La media y el 1% low tienen margen sobre 60 en este PC. Hay frames aislados más largos; no se declara que cada frame individual cumpla 16,67 ms. El tiempo GPU separado es **N/A**, ya que FrameTimingManager no produjo muestras válidas; no significa coste GPU cero. La limitación se identifica siguiendo [FrameTimingManager](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/FrameTimingManager.html). Este ensayo valida carga gráfica real offscreen, no el compositor de una ventana visible ni hardware móvil/thermal.

## AI BENCHMARK

**26 carreras: 13 ratings × 2 seeds (4207 y 99173), 286 conducciones AI**, más 26 coches del slot jugador conducidos automáticamente. Todos usan Formula/plasma para aislar habilidad; stats y masa iguales. Física real de Unity, 3 vueltas, TimeScale 6, headless. **26/26 PASS**, 0 errores, 0 stalled, 0 under-track; llegadas legales y DNF con tiempo cero. Los FPS de este benchmark no son evidencia gráfica.

{benchmark_table}

{mechanics_table}

100k frente a 20k reduce la mejor vuelta media **{fast:.2f}%**, incorpora drift/mini-turbo/rutas y aumenta cadenas. 125k conserva pequeñas variaciones: no mejora estrictamente cada seed o mejor vuelta. Los contactos son eventos tipados por vehículo con cooldown de 0,18 s, no pares únicos ni impactos de ítems. Los contactos por piloto-minuto no disminuyen monótonamente con MMR; tráfico más rápido y rutas arriesgadas siguen generándolos.

Comparación de las mismas 3 seeds ranked de 100k (4207/99173/71829), working tree inicial vs final:

| Métrica AI | Baseline | Final |
| --- | --- | --- |
| Mejor vuelta media | {baseline['meanBestLap']:.2f} s | {ranked['meanBestLap']:.2f} s ({gain:.2f}% menos) |
| Vuelta completada media | {baseline['meanLap']:.2f} s | {ranked['meanLap']:.2f} s |
| Velocidad media | {baseline['meanSpeedKmh']:.2f} km/h | {ranked['meanSpeedKmh']:.2f} km/h |
| Recuperaciones / 33 bots | {baseline['recoveriesTotal']:.0f} | {ranked['recoveriesTotal']:.0f} ({recovery_gain:.2f}% menos) |
| Bots que finalizaron | {baseline['finishedBots']}/33 | {ranked['finishedBots']}/33 |
| Contactos físicos tipados | N/A | {ranked['physicalContacts']} |
| Atajos completados reales | N/A | {ranked['shortcutCompletionsTotal']:.0f} |

Los tres finales ranked tienen 11 finishers cada uno: un DNF legal por carrera al acabar la ventana de 15 s. El slot jugador automático quedó P6/P7/P1. Demuestra oposición más fuerte; no sustituye una sesión subjetiva de conducción humana. Los ítems físicos nuevos se usaron **{ranked['itemsTotal']:.0f} veces**. CSV con todos los campos: `tmp/racing-audit-20261003/final-analysis/benchmark.csv`; datos completos: `measured-summary.json`.

## TESTS

Build Windows development final **{evidence['build']['result']}**, **{evidence['build']['errors']} errores**, **{evidence['build']['warnings']} warnings**. Los warnings restantes corresponden al legacy Box2D/Experiments y la vinculación Unity Services; no se modifican esos sistemas en este pase.

{test_table}

**307 assertions PASS / 0 FAIL** en las suites finales. Los contratos incluyen los 6 ítems, shields/occlusion/rango, 100 reutilizaciones del pool, drift sin yaw snap, salto/truco/landing, checkpoints en OnTriggerStay, reverse/duplicados, finish ghost/restauración, recuperación sin falso positivo de coche libre, geometría, stats/masa, UI/rango y las curvas/seed. Se han contado assertions, no tests ficticios por cada muestra estadística.

{race_table}

**32/32 carreras finales PASS**, **384 slots participantes (352 AI + 32 slots jugador automáticos)**, más **25 combinaciones de regresión Adventure**. Los resultados corresponden a los labels finales en `tmp/racing-audit-20261003/final-evidence.json`; las iteraciones previas fallidas se conservan como diagnóstico y no se incluyen como PASS. Cada una de las siete invocaciones finales restaura y verifica **388 valores PlayerPrefs byte por byte**. El snapshot original y los cambios previos del usuario se conservan.

## ADVENTURE REGRESSION

**25/25 PASS**: Jeep, Hill Adventurer, Formula, Rally y Sport Bike × Countryside, Rainyer, Neon City, Mountains y Space. Se usó el runner/probe existente, **FixedUpdate real**, timeScale 1, **sin Physics2D.Simulate manual**. Valida rig, ruedas/joints, geometría local, movimiento adelante, brake/reverse y aterrizaje. Sus sondas de teletransporte extremo a decenas/centenares de km son diagnósticas y no certifican el comportamiento de gameplay continuo a esas distancias.

El recorrido UI también probó selección de mapas, Garage, Skins, Shop, conducción, consumo/refill de fuel, pickups de coins/gems, driver-down, GameOver, retry, out-of-fuel y regreso al menú. Fue aquí donde se detectaron y corrigieron los listeners de Shop.

Hashes: **373 archivos protegidos comprobados, 371 idénticos, 2 cambios esperados** (`CoinPackShopUI.cs` / `GemPackShopUI.cs`), **0 ausentes**. Assets, mapas, vehículos y física de Adventure mantienen sus hashes originales. La invocación ampliada cambia sólo el dispatcher de pruebas de Racing, no el runner de Adventure.

## REMAINING ISSUES

- Persisten contactos físicos de pack: en el benchmark de 100k son **{by_mmr[100000]['contactsPerRacerMinute']:.2f} eventos por piloto-minuto**, y **{by_mmr[100000]['recoveriesTotal']:.0f} recuperaciones / 22 bots**. La AI mejora su ritmo y deja de atascarse, pero no conduce sin errores ni golpes.
- La cadena mini-turbo→salto aparece **{ranked['driftJumpChainsTotal']:.0f} veces / 33 bots** en ranked; la cadena salto→truco→landing es mucho más frecuente. No se presenta la primera como universal.
- El perfil conserva GC y picos aislados de frame. No se promete cero allocations ni 60 FPS perfectos en todos los frames. Timing GPU, compositor visible, Android y estabilidad térmica móvil siguen sin medición en este ensayo.
- La percepción del riesgo/recompensa, steering y dificultad por una persona requiere playtesting humano; las carreras aquí fueron conducidas automáticamente. El código y las mediciones sí demuestran una diferencia fuerte frente a 20k y frente al baseline.

## VERDICT

**PASS de funcionalidad Racing, progresión competitiva y regresión Adventure dentro del alcance medido.** La dificultad alta proviene de decisiones/mecánicas con físicas iguales: 100k mejora {fast:.2f}% sobre 20k en el benchmark uniforme; ranked mejora {gain:.2f}% sobre el working tree inicial y reduce recuperaciones {recovery_gain:.2f}%. UI y presentación están respaldadas por capturas del producto real.

El render High/timeScale 1 tiene margen sobre 60 en la media y el 1% low de este PC. El resultado es específico a esta carga offscreen y hardware; no certifica uniformidad absoluta ni móvil. Todo queda en el working tree para revisión, sin commit ni push.
"""
    (ROOT / "Docs/RacingAudit-20261003.md").write_text(text, encoding="utf-8")
    print("Report written:", ROOT / "Docs/RacingAudit-20261003.md")


if __name__ == "__main__":
    main()
