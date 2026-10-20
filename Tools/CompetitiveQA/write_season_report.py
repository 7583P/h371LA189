"""Assemble the local evidence report from retained reports, without activation."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / "Logs/SeasonExpansion/20261018"
LAB = ROOT / "Logs/MmrLab/20261018"


def read(path, default=None):
    path = ROOT / path
    return json.loads(path.read_text(encoding="utf-8-sig")) if path.exists() else default


def table(headers, rows):
    return "| " + " | ".join(headers) + " |\n| " + " | ".join("---" for _ in headers) + " |\n" + "\n".join("| " + " | ".join(str(v) for v in row) + " |" for row in rows)


def report():
    summary = read("Logs/MmrLab/20261018/matrix/summary.json")
    cases = read("Logs/SeasonExpansion/20261018/elite-off-complete-analysis.json", [])
    cpp = read("Logs/SeasonExpansion/20261018/cpps-latest/cpps-contracts.json")
    season = read("Logs/SeasonExpansion/20261018/acceptance-latest/season-expansion-contracts.json")
    after = read("Logs/MmrLab/20261018/contracts-policy/mmr-experiment-contracts.json")
    ui = read("Logs/SeasonExpansion/20261018/human-gate-policy/mmr-ui-contracts.json", {})
    rc = read("Logs/SeasonExpansion/20261018/human-gate-policy/rc-contracts.json", {})
    gate = read("Logs/SeasonExpansion/20261018/human-gate-policy/build-size.json", {})
    delivery_path = EVIDENCE / "android-policy-resumed/delivery-bytes.csv"
    delivery_current=delivery_path.exists()
    if not delivery_current:
        delivery_path=EVIDENCE / "android-atlas6-validated/delivery-bytes.csv"
    delivery = int(delivery_path.read_text(encoding="utf-8-sig").strip().splitlines()[-1].split(",")[1])
    bundle=ROOT / "Builds/AndroidSizeForensics-20261018/season-expansion-policy-current.aab"
    bundle_bytes=bundle.stat().st_size if bundle.exists() else "PENDIENTE"
    organic = [r for r in summary["all_final_rows"] if r["scenario"] == "organic" and r["seasons"] == 24]
    reset_rows = [[r["policy"], r["seed"], f'{r["mean"]:.1f}', f'{r["median"]:.1f}', f'{r["p95"]:.1f}',
                   f'{r["p99"]:.1f}', f'{r["after_3000_if_approved"]}/1200', r["current_hill_beyond"]] for r in organic]
    elite_rows = [[r["mmr"], r["seed"], f'{r["finishers"]}/12', r["dnf"], r["recoveries"], f'{r["spread"]:.2f}' if r["spread"] is not None else "INCOMPLETO",
                   f'{r["route_completions"]}/{r["route_entries"]}'] for r in cases]
    finishers=sum(r['finishers'] for r in cases);dnfs=sum(r['dnf'] for r in cases);recoveries=sum(r['recoveries'] for r in cases)
    elite_status=f"Matriz secuencial Off: {len(cases)}/6 casos cerrados, {finishers}/{len(cases)*12} finishers, {dnfs} DNF, {recoveries} recuperaciones."
    elite_quality=len(cases)==6 and finishers==72 and recoveries<=2 and all(r['spread'] is not None and 3<=r['spread']<=8 and set(r['ml_modes'])-{""}=={"Off"} and r['ml_ticks']==0 for r in cases)
    sim_rows = []
    for label in ["expanded-100k-100d", "expanded-100k-90d-seed4208", "expanded-100k-90d-seed4209"]:
        sample = read(f"Logs/SeasonExpansion/20261018/simulations/simulation-{label}.json")
        cp = sample["checkpoints"][-1]
        sim_rows.append([sample["seed"], sample["days"], cp["totalSimulatedRaces"], int(cp["onlineBots"]["min"]),
                         f'{cp["racesPerBot"]["mean"] / sample["days"]:.2f}', sample["reloadIdentical"], sample["invariantFailure"]])
    perf_pair_path = "Logs/SeasonExpansion/20261018/performance-comparison-validated.json"
    if not (ROOT / perf_pair_path).exists():
        perf_pair_path = "Logs/SeasonExpansion/20261018/performance-comparison.json"
    perf_pair = read(perf_pair_path, {})
    perf_rows = []
    perf_cost_rows = []
    def measured(value, digits=3):
        return f"{value:.{digits}f}" if value is not None else "NO DISPONIBLE"
    for mode in ["off", "shadow"]:
        directory = perf_pair.get('source_paths', {}).get(mode, f"Logs/SeasonExpansion/20261018/performance-{mode}-policy")
        sample = read(str(Path(directory) / "seed-4207/performance-analysis.json"), {})
        if sample:
            accepted = perf_pair.get("accepted", False)
            checks = perf_pair.get('cases', {}).get(mode, {}).get('checks', {})
            valid_case = bool(checks) and all(checks.values())
            validity = "VALIDO" if accepted else "VALIDO AISLADO; PAR PENDIENTE" if valid_case else "DESCARTADO"
            perf_rows.append([mode, validity, f'{sample["average_fps"]:.1f}', f'{sample["median_fps"]:.1f}',
                              f'{sample["one_percent_low_fps"]:.1f}', f'{sample["point_one_percent_low_fps"]:.1f}', f'{sample["p95_ms"]:.2f}',
                              f'{sample["p99_ms"]:.2f}', f'{sample["max_frame_ms"]:.2f}', sample["unfocused_frames"]])
            perf_cost_rows.append([mode, measured(sample['main_thread_ms']), measured(sample['render_thread_ms']),
                                   measured(sample['physics_ms']), measured(sample['ai_ms']), measured(sample['gpu_ms']),
                                   measured(sample['gc_mean_bytes'],1), measured(sample['peak_working_mib'],1),
                                   sample['ml_ticks'], measured(sample['ml_full_tick_mean_us'])])
        else:
            perf_rows.append([mode, "PENDIENTE", "—", "—", "—", "—", "—", "—", "—", "—"])
    perf_issues = "; ".join(perf_pair.get("rejection_reasons", [])) or ("Ninguna condición de rechazo en este par local." if perf_pair.get("accepted") else "Medición pendiente.")
    before = read("Logs/SeasonExpansion/20261018/provenance/source-hashes.json")
    inventory = []
    for entry in before:
        path = ROOT / entry["path"]
        if path.exists():
            actual = hashlib.sha256(path.read_bytes()).hexdigest()
            if actual != entry["sha256"]:
                inventory.append(dict(path=entry["path"].replace("\\", "/"), before=entry["sha256"], after=actual))
    (EVIDENCE / "provenance/changed-existing-sources.json").write_text(json.dumps(inventory, indent=2), encoding="utf-8")
    changed = "\n".join("- `" + item["path"] + "`" for item in inventory)
    reset_table = table(["Reset experimental", "Seed", "Media Q24", "P50", "P95", "P99", "AFTER si >=3000", "Beyond actual"], reset_rows)
    elite_table = table(["MMR QA", "Seed", "Finishers", "DNF", "Recuperaciones", "Diferencia 1.º–12.º (s)", "Atajos completados/entradas"], elite_rows)
    sim_table = table(["Seed", "Días", "Carreras", "Mínimo online sintético", "Carreras/bot/día", "Recarga idéntica", "Invariantes"], sim_rows)
    perf_table = table(["ML", "Validez", "FPS medio", "Mediana FPS", "1% low", "0,1% low", "P95 ms", "P99 ms", "Peor ms", "Sin foco"], perf_rows)
    perf_cost_table = table(["ML", "Main ms", "Render ms", "Física ms", "IA ms", "GPU ms", "GC bytes/frame", "Pico working MiB", "Inferencias", "Tick ML µs"], perf_cost_rows) if perf_cost_rows else "Detalle de costes pendiente."
    text = f"""# Hill Adventurer — temporadas, comunidad, MMR y AFTER

Informe local de las solicitudes `Rainbow_Road_Master_Prompt` y `MMR_SoftReset_Titulos_Prompt`, trabajo del 18–19 de octubre de 2026. Las rutas de evidencia conservan la fecha de inicio 20261018. Las copias autorizadas están en [provenance](../Logs/SeasonExpansion/20261018/provenance/).

**Estado: NOT READY FOR PLAY STORE.** Hay implementación y evidencia local. Calidad élite (0–2 recuperaciones y diferencias de 3–8 s): {'acreditada en esta matriz' if elite_quality else 'no acreditada'}. Comparación Windows Off/Shadow: {'válida localmente' if perf_pair.get('accepted') else 'no acreditada'}. Faltan dispositivo Android, aprobación humana del HumanGate y decisiones de producto indicadas abajo. Ningún AAB se publicó ni se firmó como release. No se activó una migración nueva de rating ni AFTER desde 3000.

[Conciliación final de evidencias y gates pendientes](../Logs/SeasonExpansion/20261018/final-gates.json).

## Implementado, probado, aprobado y activado

| Área | Implementado | Probado | Aprobación / activación |
| --- | --- | --- | --- |
| HA-PW2, placements de carrera, reset Hill | Sistema preexistente conservado | Contratos, regresión 10k, simulaciones largas | Regla existente; sin fórmula nueva activada |
| Clasificación estacional 1 llegada, historial, títulos Top y ASSASSINER | Integrado en el código | Contratos puros y Unity | Solicitado por el primer documento; presente en builds QA, sin publicación |
| Comunidad 100k / 50k+ presentes | Migración con backup e identidades persistentes | 3 semillas de 90–100 días y fallos de migración | Solo opt-in Editor/Development `--league-population-100000`; producción sigue en 10k |
| AFTER >=3000 /100 | Núcleo, recibos, archivo y rombo experimental | {after['passed']} comprobaciones puras, UI real separada | **Sin autorización de escala, sin integración ni migración de perfiles de producción** |
| Resets de umbral 2500/85% y 1000/95% | Herramienta QA aislada | 24 temporadas, 2 semillas por alternativa | No activados; el reset instalado sigue igual |
| Carretera prismática y llantas | Material/código visual | Compilación y carreras renderizadas | No cambia geometría, colisiones, atajos ni físicas; revisión humana pendiente |
| Android: atlas decorativo ASTC 6x6 y 2 texturas de coches | Candidato forense a resolución original | Entrega {'actual' if delivery_current else 'del AAB anterior'} {delivery:,} bytes en configuración de referencia | Revisión visual/dispositivo pendiente; no es una release |
| ML | Pipeline offline preexistente auditado y prioridad del override QA corregida | 35 tests del pipeline; comparación Off/ML02 {'válida localmente' if perf_pair.get('accepted') else 'pendiente o descartada; ver evidencia'} | Modelo instalado ML01 intacto; producción Off; candidato ML02 solo Shadow QA |

## Auditoría inicial y compatibilidad

`main`, HEAD `3be766620f6c38e5287bbb73e3a925973806df96`. El repositorio ya tenía numerosas modificaciones, assets y herramientas sin seguimiento. Se guardaron estado, hashes y fuentes previas antes de modificar código. No hubo commit, push, pull, reset, cambio de rama ni descarte del trabajo existente. La eliminación preexistente de `Logs/FinalClosure/20261008/REPORT.md` sigue siendo del estado inicial.

Racing usa 12 corredores: un humano y 11 bots persistentes reservados; el simulador estadístico usa 12 bots, nunca un humano simulado. Hay placements iniciales de carrera 0/3 y rating provisional separado. El límite técnico HA-PW2 es **999999**, la escala normal Hill termina en 10000 y el prestigio actual es **APEX BEYOND estrictamente por encima de 10000**, con pasos de 1000. `ratingScaleMaximum=10000` y el MMR de benchmark no son un cap visible de 100000. La población 100000 tampoco representa un límite de rating.

No se adoptaron la escala ni los rangos de Survival 100. Divisiones actuales de 500 puntos: Rookie I/II 0–999, Scout I/II 1000–1999, Challenger I/II 2000–2999, Pro I/II 3000–3999, Expert I/II 4000–4999, Elite I/II 5000–5999, Master I/II 6000–6999, Grandmaster I/II 7000–7999, Titan I/II 8000–8999, Apex I/II 9000–10000; Beyond comienza en 10001.

La habilidad oculta ya era independiente en los datos, pero el lobby físico usaba rating visible para bots ya clasificados. Ahora `GenerateRankedLobby` usa siempre `drivingRating` derivado de esa habilidad; los benchmarks conservan su dificultad explícita. El reset de rating no reduce habilidad, velocidad, agarre ni capacidades físicas. Los 36 archivos protegidos comprobados de IA, vehículos, pista, cámara, items y reglas base coinciden con los hashes previos; véase [protected-source-checks.json](../Logs/SeasonExpansion/20261018/provenance/protected-source-checks.json).

## Fórmula de carrera y economía

Se conserva **HA-PW2**. Cada corredor se compara con sus 11 rivales. Para cada par: resultado 1/0/0,5 según orden; expectativa `1/(1+10^((rj-ri)/Dij))`; `Dij = 1100*(1+0,18*(ui+uj)/2)` y `Kij = ((50+120*ui)+(50+120*uj))/2`. Se suman `Kij*(resultado-expectativa)/11` entre participantes clasificados. Se reconcilian enteros por restos mayores: suma exacta cero antes de límites. Se usan posición final, DNF ordenado por progreso y abandono por debajo de DNF. Trucos, drift, atajos y vueltas no añaden MMR independiente.

Las tres carreras iniciales actualizan una estimación provisional con prior 2500, K 1500/1100/800, máximo provisional 9000 y emisión de rating visible al completar placement, limitada a 6500. Los pares contra participantes provisionales no alteran el rating establecido. Se conserva el comportamiento existente de DNF/abandono en estos placements, separado de la llegada requerida para clasificación estacional.

Fuentes auditadas: emisión única al terminar placement y corrección explícita del suelo cero. Sumideros: techo de seguridad, soft reset y migraciones existentes. El presupuesto registra cada contador. Identidad de conservación: `supply_final = supply_inicial + pairwiseNet + floorInjection - ceilingRemoval + placementIssuance - softResetRemoval - migrationRemoval`. `pairwiseNet` debe ser cero. Presencia, conexión, volumen y replays no emiten rating. No se añadió reposición competitiva: las muestras largas no demuestran una erosión indefinida que la justifique.

La liquidación autoritativa conserva `raceId`, reserva de 11 bots, recibos persistentes y aplicación idempotente. Se rechazan participantes repetidos, resultados ajenos a la reserva y temporadas incompatibles. El cierre/reinicio de una carrera empezada se trata como abandono; un recibo de llegada realmente completada se recupera una vez. El resultado visible se sustituye por el settlement del coordinador, no por una predicción del HUD.

## Calendario, reset y clasificación vigente

Se mantiene el calendario trimestral global con época autorizada `2026-10-01T00:00:00Z`, meses naturales y UTC. El reset existente interpola una curva Hermite monótona por `(fin,inicio)`: `(0,0),(1000,980),(2500,2350),(5000,4500),(7500,6500),(9000,8200),(9500,9200),(10000,10000)`. Primero calcula la curva; para MMR anterior >10000 aplica el cap final 10000. El reset conserva o reduce, nunca aumenta. Mantiene IDs, habilidad, récords, compras y títulos. No convierte el cap en un suelo ni baja artificialmente un carry permitido de 10000.

Transiciones por chunks, cursor durable, archivo antes del reset y recibo `ST:origen->destino:c1` evitan repetir la transición tras un cierre. Reloj confiable, monotónico y anomalías siguen vigentes. Inicial 0/3 tiene prioridad visual. Después de cada reset hay una clasificación estacional separada 0/1: una llegada válida en la temporada. Mientras está pendiente se conserva MMR numérico y aparece exactamente `(Season Placement)`, sin puesto oficial ni título Top vigente. Los clasificados van por delante en leaderboard; entre ellos se ordena por MMR final y desempate estable de identidad.

## AFTER y títulos: decisión todavía pendiente

La alternativa solicitada es `floor((MMR-3000)/100)` para MMR >=3000: 2999 sin AFTER; 3000/3099=0; 3100/3199=1; 3200=2; 3999=9; 4000=10; 5000=20; 10000=70; 100000=970. Es incompatible como cambio silencioso con las divisiones Hill actuales. Se implementaron dos definiciones configurables: propuesta 3000/100 y compatibilidad Beyond >10000/1000. Todo este experimento se excluye por compilación de los players, incluso del HumanGate.

En Unity: `Hill Adventurer > QA > MMR and AFTER preview (experimental)`. El rombo usa una malla constante y texto dinámico; no genera miles de texturas. [Previsualización renderizada](../Logs/SeasonExpansion/20261018/human-gate-policy/after-experimental-preview.png). Su contrato comprueba texto, malla, CanvasRenderer y píxeles; los intentos previos defectuosos se conservaron como evidencia de fallo.

El diario experimental solo observa recibos liquidados de la temporada. Un máximo de título exige carrera finalizada, carrera clasificada y clasificación estacional; carry, importación, DNF y abandono no generan máximos. Un abandono liquidado sí actualiza la representación del MMR actual. Cada título congela temporada, regla, umbral, paso, máximo observado y recibo: `Season X AFTER N`. Una pérdida puede quitar el rombo actual y conservar el título del máximo. Exportar/recargar o cambiar la regla en otra temporada no recalcula el pasado. No sustituye MMR final ni posición final por ese máximo.

Se integraron por separado los títulos del primer documento: Top 1–10 cian claro, 11–50 amarillo, 51–100 rojo, 101–500 blanco neón, 501–1000 blanco. El título vigente depende de puesto oficial; los históricos incluyen temporada y no dicen CURRENT. ASSASSINER se gana mediante victoria Ranked válida con al menos 3 atajos y 1 aéreo; no se otorga a todos ni da MMR adicional.

## Comparación multitemporada

Se ejecutaron **{summary['experiments']} experimentos y {summary['total_settled_races']:,} carreras liquidadas**, con cero fallos de presupuesto, lobbies de 12 o recarga. Se usa el scheduler, resultado estadístico, incertidumbre, matchmaking y reset reales del núcleo, en carpetas aisladas. Ninguna simulación simplificada acredita conducción física ni Android. No hubo cuotas por rango, resultados guionizados, bonificaciones por volumen ni humano simulado.

Las alternativas muestrean cada 100 puntos una compresión descendente con umbral 2500/factor 0,85 o umbral 1000/factor 0,95, usan el interpolador monótono real y conservan el cap Hill de 10000 tras la curva. Son alternativas QA, no una nueva regla instalada. Cada entero hasta 999999 se verifica: sin aumento y con orden preservado.

{reset_table}

AFTER de la tabla es un **recuento hipotético** con umbral 3000; no un rango activado ni una cuota. Son muestras de 1200 bots, no una prueba de equilibrio de 100k durante 24 temporadas. Se conserva el reset existente: las dos alternativas mantienen medias superiores, pero no hay aprobación para modificar la economía. Al final de 24 temporadas la curva existente conserva media 2168–2225, frente a 2452–2511 y 2538–2609 de las alternativas. No se observa inflación ilimitada por carreras ni colapso permanente a cero en estas semillas; no se extrapola a tiempo infinito.

Otros escenarios: cuatro temporadas con población real de control 10000 (seed 4207), élite 1200 (4208), polarizado 1200 (4209), y doce temporadas de movilidad 1200 (4209) por cada fórmula. Los fixtures élite/polarizado/movilidad declaran su supply inicial sintético, independiente de cualquier perfil del usuario. El escenario movilidad cambia explícitamente la capacidad oculta al comenzar Q2: el principiante observado pasó 610→3982→5666→6071, y el antiguo élite 8967→259→0. No se cambiaron vencedores; el motor resolvió las carreras. El bot inactivo mantuvo cero carreras, sin clasificación ni premios, y sufrió únicamente las reducciones del reset.

El escenario élite terminó Q4 con media 7206, P95 10172, P99 10871 y máximo 11965, demostrando acumulación legítima por encima de 10000 durante una temporada dentro de ese fixture. No demuestra que una población inicial orgánica llegue a esa distribución. En la población orgánica 100k a 90 días la cola alta sigue siendo escasa, y la pirámide aspiracional del primer documento no está acreditada.

Datos completos: [resumen](../Logs/MmrLab/20261018/matrix/summary.json), [mediciones por temporada](../Logs/MmrLab/20261018/matrix/season-measurements.csv), [figura PNG](../Logs/MmrLab/20261018/matrix/economy-comparison.png) y [SVG exportable](../Logs/MmrLab/20261018/matrix/economy-comparison.svg). Incluyen supply, media, mediana, SD, P10/P50/P90/P95/P99, cambios por carrera, bandas de habilidad, ascensos/descensos, mayor subida/caída, volumen, AFTER, CPU, memoria y recarga.

## Comunidad 100k e historial

Default de producción **10000**. La expansión opt-in guarda una copia durable de recuperación, conserva los 10000 registros originales, IDs, nombres, ratings, habilidad, RNG, reservas, cosméticos y humano. Añade exactamente 90000 identidades a MMR cero y placement 0/3. Registro compacto de bot: 172 bytes, sin 100000 GameObjects. La presencia pasiva usa horarios persistentes, husos y perfiles; estar presente no obliga a correr ni otorga rating. Se muestra comunidad **LOCAL OFFLINE**: no son usuarios de Internet.

{sim_table}

La regresión final 10k/100 días (seed 4207) vuelve a dar **663334 carreras**, igual al control anterior, presupuesto reconciliado y recarga idéntica. Los informes conservan saves, muestras horarias y snapshots. Los benchmarks host de 100k muestran costes de guardado/catch-up y matchmaking que requieren medición móvil antes de activar. En el benchmark de consultas a 10000 MMR tras solo 30 horas hubo escasez de rivales y ampliación de bandas: no acredita matchmaking de élite maduro.

Antes de cada reset se captura un archivo inmutable comprimido por bloques de 256 filas, con offsets, SHA256, nombres y cosméticos tal como estaban. Se leen páginas de 14 filas, búsqueda, Top10/100/1000/All y My Position. No se clonan todos los perfiles. Pasados sin snapshot verificable se muestran NOT AVAILABLE; no se reconstruyen posiciones inventadas. Datos de carrera antiguos se conservan separados.

ZIP público mediante destino elegido en Windows y Android SAF: manifest, CSV y resumen de temporada; hashes, recuentos, campos, límites, cancelación y round-trip verificados antes del éxito. No incluye wallet, compras, credenciales ni dump privado de perfiles. La transferencia completa QA sigue restringida a Editor/Development. Windows y Java compilaron; la interacción real de los pickers requiere HumanGate/dispositivo.

Papelera explícita para snapshots de temporadas terminadas, con recibos alternados durables, checksum, confirmación, fecha mínima y restauración. Purga solo tras 30 días de tiempo confiable acumulado. Saltos/retrocesos de reloj o tiempo cerrado sin prueba aplazan la purga, por lo que puede retener más de 30 días naturales. No toca liga vigente, perfiles, títulos, compras, otros archivos ni exports. Tests cubren días 1/29/30, relanzamiento, rollback e interrupciones entre pasos de borrar/restaurar/purgar.

| Temporadas (100k, fixture de almacenamiento) | Snapshots nuevos medidos | Historial protegido de bots, aparte | Total aproximado mínimo |
| --- | --- | --- | --- |
| 1 | 1,62 MB | 3,80 MB | 5,42 MB |
| 10 | 16,19 MB | 38,00 MB | 54,19 MB |
| 20 | 32,39 MB | 76,00 MB | 108,39 MB |
| 40 | 64,78 MB | 152,00 MB | 216,78 MB |

El fixture repite datos observados para medir almacenamiento; **no representa 40 temporadas reales**. El total excluye cabeceras, historial humano, dos journals de liga y exports. El código muestra también los bytes protegidos. Los snapshots reales 100k medidos rondan 1,79 MB por temporada. Esta retención y el coste móvil refuerzan mantener 100k desactivado por defecto.

## Carreras físicas, rendimiento y Android

{elite_table}

{elite_status} La aspiración de 72/72 sin DNF, 0–2 recuperaciones totales y diferencias de 3–8 segundos está **{'CUMPLIDA' if elite_quality else 'NO CUMPLIDA'}**. Los déficits por sector, contacto, tráfico, items y atajos son asociaciones observadas; no se atribuyen segundos causalmente ni se cambian físicas para cerrar una cifra. [Análisis forense actual](../Logs/SeasonExpansion/20261018/elite-off-complete-analysis.json).

Se observaron {sum(r['route_completions'] for r in cases)}/{sum(r['route_entries'] for r in cases)} recorridos de atajo completados y {sum(r['samples_dropped'] for r in cases)} muestras/{sum(r['contacts_dropped'] for r in cases)} contactos descartados por capacidad. Las siete recuperaciones corresponden a 10000 MMR (4/2/1); a 9700 hubo cero en las tres semillas. Las diferencias finales siguen entre 15,20 y 27,06 s. Los datos permiten localizar sectores y eventos para futuras intervenciones; no identifican por sí solos un ajuste seguro que garantice 3–8 s.

La tanda anterior completó 72/72 con cinco recuperaciones, pero sus métricas revelaron Shadow aunque se había pedido Off. Se conservó y no acredita ML Off. Se corrigió el override QA que el spawner sobrescribía al inicializar el coche y se añadieron pruebas. La repetición parcial paralela registró 22/24 finishers, dos DNF, antes de la interrupción; es evidencia de fallo bajo esa carga, no se oculta ni se convierte en PASS. La matriz actual se repite secuencialmente sin compilar al mismo tiempo.

{perf_table}

{perf_cost_table}

Validez del par: **{'ACEPTADO COMO MEDICIÓN LOCAL' if perf_pair.get('accepted') else 'NO ACREDITADA'}**. {perf_issues} [Comparación y comprobaciones](../{perf_pair_path}). Se verifican hashes del ejecutable, UnityPlayer y assembly antes/después, plan de energía, ajustes, modelo ML02 exacto y actividad de inferencia. Un solo par aceptado es una observación local, no una estimación estadística definitiva del coste de ML. Los números de una ventana descartada no permiten afirmar que un modo sea más rápido.

La comparación de rendimiento exige mismo player, seed 4207, MMR 10000, 1600×900, calidad High, escala 1, alimentación y foco. Se usa render nativo y se conservan stalls y colas; una corrida sin foco se descarta entera. El candidato ML02 se carga solo por override QA, sin reemplazar el asset ML01 instalado. Los informes por modo incluyen CPU main/render, física, IA, ML, GC, memoria, energía y frames anteriores/posteriores a stalls. Marcadores solapados no se suman para inventar presupuesto; GPU no disponible se declara. Stalls externos no se atribuyen a ML sin evidencia.

La primera ventana `performance-off` se conserva descartada: en realidad usó Shadow, perdió el foco en 15669/15952 frames y alternó batería/corriente. En la repetición con la política corregida, `performance-off-policy` usó Off real/ML02 cargado/0 inferencias, pero sus 10951 frames estuvieron sin foco; Shadow sí mantuvo foco y corriente. Se conserva cada intento y no se deduce que Shadow sea más rápido a partir de ese par inválido. La repetición adicional Off encontró otra validación Unity de un proyecto distinto, por lo que no inició una medición con esa carga concurrente.

Las ventanas de 60 segundos interrumpen deliberadamente una carrera completa: se conserva su exit code y suite parcial, y se evalúan como medición de frames, separada de la matriz completa de 72 llegadas. No acreditan por sí solas el gate de carrera.

ML usa entrenamiento offline validado y comparación local MLP/GRU/attention GRU. Los scripts heredados de entrenamiento llaman al pipeline protegido; el YAML antiguo de 11 entradas/5 salidas no describe el contexto ML02 de 33 entradas/8 salidas. Los candidatos guardados no tienen validación de experto humano ni seguridad cerrada para autoridad. Los bots de liga no aprenden una red en vivo: usan habilidad persistente, experiencia acotada y forma estadística. Assist/Authority continúan limitados a Shadow.

Android, comparación medida con bundletool 1.17.2, ARM64/API35/420dpi/en:

| Variante forense | Entrega calculada (bytes) |
| --- | --- |
| Referencia anterior | 156897195 |
| Cambios iniciales de coches | 156021282 |
| Atlas 4x4 | 154473417 |
| Atlas 5x5 | 150422095 |
| Atlas 6x6 antes de la corrección de política ML | 147951951 |
| Atlas 6x6 con código actual | {delivery} |

El límite es 150000000 **bytes decimales de entrega**, no tamaño del `.aab`. El AAB con código actual ocupa {bundle_bytes} bytes; la entrega {'actual' if delivery_current else 'del candidato anterior; la actual está pendiente'} es {delivery} bytes, con margen {150000000-delivery} bytes. No es una medida de Play Console para todos los dispositivos. Se conservaron dimensiones/píxeles fuente y geometría/pivotes de 17 sprites decorativos; fuentes excluidas. Los dos cambios de mundo que empeoraban el tamaño se restauraron exactamente; se conservaron dos mejoras de compresión de coches. Atlas Standalone BC7 mantiene resolución original. No se comprimió una fuente por error ni se bajó resolución para alcanzar el objetivo.

Paquete `com.matias.hilladventurer`, versión 1.2, versionCode 20 intacto, IL2CPP ARM64. El manifiesto Android compilado resuelve mínima API25 y objetivo API36; la API35 anterior corresponde al dispositivo de referencia de bundletool. AABs de depuración forenses, no firma de producción. Configuración AdMob de producción presente; Editor/QA usa unidades de muestra y no interactúa con anuncios reales. No hay dispositivo ADB conectado: quedan sin probar gameplay Android, AdMob real, GPGS, IAP, picker SAF, calidad ASTC y rendimiento/temperatura de 100k. La consulta pública no permitió identificar una ficha oficial y website autoritativo para verificar app-ads.txt; no se declara PASS. No se consultó Play Console ni el último versionCode usado allí.

## Tests y HumanGate

| Evidencia | Resultado |
| --- | --- |
| CPPS existentes | {cpp['passed']} PASS / {cpp['failed']} FAIL |
| Expansión/temporadas/historial/recuperación | {season['passed']} PASS / {season['failed']} FAIL |
| Experimento MMR/AFTER/reset exhaustivo | {after['passed']} PASS / {after['failed']} FAIL |
| Unity 354 comprobaciones previas + 2 de política ML | {'PASS' if rc.get('failed') == 0 else 'PENDIENTE/FAIL'} |
| Unity UI experimental con render | {ui.get('passed','PENDIENTE')} PASS / {ui.get('failed','PENDIENTE')} FAIL |
| Pipeline ML existente | 35 PASS |
| Matriz multitemporada | 12 escenarios, {summary['total_settled_races']:,} carreras, cero fallos |
| Android físico / anuncios / servicios | NO PROBADO, sin dispositivo |
| Aprobación humana | PENDIENTE |

HumanGate actual: [ejecutable](../Builds/HumanGateMmrValidated/HillAdventurer-HumanGate.exe), [lanzador con ML Off](../Builds/HumanGateMmrValidated/Jugar-HumanGate.cmd). Build local `{gate.get('result','PENDIENTE')}`. QA tiene compañía/persistencia separada y los recorridos automáticos usan saves aislados. No hay aprobación humana automática. Abrir y comprobar controles/cámara, llegada y reintento, carretera, legibilidad de MMR/placement/títulos, historial y exportación/cancelación real del diálogo. Android y producción requieren sus propios gates después. [Hashes de los artefactos actuales](../Logs/SeasonExpansion/20261018/provenance/final-artifact-hashes.json).

Para repetir rendimiento desde una sesión interactiva: cerrar otras cargas Unity y abrir [Medir-Rendimiento.cmd](../Builds/HumanGateMmrValidated/Medir-Rendimiento.cmd), mantener corriente y cada ventana del juego al frente durante 60 s. Usa carpetas nuevas `manual-*`, conserva todas las anteriores y valida automáticamente el par. El script no cambia la política global de PowerShell ni inicia una carrera de producción. La ejecución de otro proyecto Unity bloquea la medición antes de comenzar; no acredita un fallo del juego ni detiene procesos ajenos.

## Reproducir y revisar

```powershell
dotnet run --project Tools/CompetitiveQA -c Release -- mmr-contracts Logs/MmrLab/review-contracts
dotnet run --project Tools/CompetitiveQA -c Release -- seasons Logs/MmrLab/review-seasons
dotnet run --project Tools/CompetitiveQA -c Release -- mmr-lab Logs/MmrLab/review-matrix 24 1200 4207 organic existing
dotnet run --project Tools/CompetitiveQA -c Release -- mmr-lab Logs/MmrLab/review-matrix 24 1200 4207 organic threshold-2500-85
```

Elegir destinos nuevos conserva evidencia previa. CLI de experimento acepta temporadas 1–24, población, seed, escenario `organic/elite/polarized/mobility` y las tres políticas documentadas. Un fixture de 120 bots no formó lobbies de 12: se conserva como diagnóstico de escasez y se excluye de conclusiones; el harness ahora rechaza explícitamente una simulación sin carreras.

Fuentes cubiertas por la copia inicial de sesión que cambiaron durante este trabajo (separadas del diff acumulado anterior):

{changed}

También se modificaron los helpers Editor `RacingSeasonResetContracts.cs` (fixture con clasificación explícita) y `RacingRcPassContracts.cs` (ejecución componible y fixtures nuevos que conservan evidencia), y los analizadores `Tools/RC/analyze_elite.py` / `analyze_performance.py` para leer las rutas actuales y etiquetar correctamente ventanas medidas. La exclusión de AFTER experimental está respaldada por los guards de compilación, el build y un [escaneo complementario de símbolos](../Logs/SeasonExpansion/20261018/human-gate-policy/player-symbol-checks.json).

Fuentes nuevas principales: `Competitive/SeasonQualification.cs`, `ExpandedCommunitySchedule.cs`, `LeaguePopulationMigration.cs`, `SeasonHistoryStore.cs`, `SeasonHistoryTrash.cs`, `SeasonalTitleRules.cs`; `UI/RacingHistoryUI.cs`, `UI/SeasonHistoryDocumentSave.cs`; `ML/RacingMlExecutionPolicy.cs`; plugin Android `HillSeasonHistory.androidlib`; shader `RacingPrismaticRoad.shader`; atlas `AndroidUiDelivery.spriteatlas`; herramientas `SeasonExpansionContracts.cs`, `SeasonScaleBench.cs`, `MmrEconomyLab.cs`, `MmrExperimentContracts.cs`, scripts de resumen/informe; editor `Experiments/*`, y diagnósticos `Experiments/*` excluidos de players. Metas y configuración del atlas son parte de esos cambios. `Tools/RC/Measure-SeasonMmrPerformance.ps1` y `compare_performance.py` conservan comandos, hashes y condiciones de la comparación de rendimiento.

El inventario detallado y hashes están en [provenance](../Logs/SeasonExpansion/20261018/provenance/). Los cambios previos en anuncios, assets, IA y físicas se conservaron. `git diff --check` del código modificado pasó; Git sigue en main y sin commits de esta sesión.

**Decisiones pendientes:** conservar la escala Hill o autorizar la propuesta AFTER >=3000 /100; aprobar visual/controles/exportación del HumanGate; conectar Android y validar servicios, rendimiento y entrega Play; decidir la expansión de población después de esa medición. La evidencia actual justifica mantener el reset y población de producción, sin migración de MMR ni reposición nuevas. No se presenta como READY.

Referencias de API utilizadas: [cámara de escena preview Unity 6000.3](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera-scene.html), [Android SAF](https://developer.android.com/training/data-storage/shared/documents-files), [selector Windows GetSaveFileNameW](https://learn.microsoft.com/en-us/windows/win32/api/commdlg/nf-commdlg-getsavefilenamew). Estas referencias explican APIs; la evidencia de comportamiento es la del proyecto y sus pruebas, con los límites descritos.
"""
    destination = ROOT / "Docs/SeasonExpansion-MMR-20261018.md"
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(text, encoding="utf-8")
    print(str(destination))


if __name__ == "__main__":
    report()
