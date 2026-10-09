"""Create the final 20-section audit from measured, immutable continuation evidence."""
import argparse
import csv
import html
import json
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / 'Logs/MMR10KMigration/RC20261006'
RATINGS = [100, 1000, 2500, 5000, 7500, 9000, 9700, 10000]


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def link(path, label=None):
    path = Path(path).resolve()
    return f'[{label or path.name}]({Path(os.path.relpath(path, BASE)).as_posix()})'


def number(value, digits=3):
    return '—' if value is None else f'{value:,.{digits}f}'


def table(headers, rows):
    return '\n'.join(['| ' + ' | '.join(headers) + ' |',
                       '| ' + ' | '.join(['---'] * len(headers)) + ' |'] +
                      ['| ' + ' | '.join(map(str, row)) + ' |' for row in rows])


def plots(core, matrix, gate, curve_path):
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    plt.rcParams.update({'font.size': 10, 'axes.grid': True, 'grid.alpha': .18})
    target = BASE / 'charts'
    target.mkdir(exist_ok=True)
    with Path(curve_path).open(encoding='utf-8', newline='') as stream:
        curve = list(csv.DictReader(stream))
    fig, axes = plt.subplots(1, 2, figsize=(12, 5), layout='constrained')
    x = [int(r['ending_mmr']) for r in curve]
    y = [int(r['starting_mmr']) for r in curve]
    axes[0].plot(x, y, color='#098b82', label='Production monotone cubic reset')
    axes[0].plot(x, x, '--', color='#888888', label='Unchanged rating')
    axes[0].set(xlabel='Season ending MMR', ylabel='Next season starting MMR', title='Installation-local season reset')
    axes[0].legend(fontsize=8)
    bands = core['beforeReset']['bands']
    axes[1].bar(range(len(bands)), [r['count'] for r in bands], color=['#128db8']*20+['#b329c9'])
    axes[1].set_xticks(range(len(bands)), [r['rank'].replace('APEX BEYOND 10K', 'BEYOND') for r in bands], rotation=90, fontsize=8)
    axes[1].set(ylabel='Data bots', title='Measured 90-day statistical population / 10,000 bots')
    fig.savefig(target / 'reset-and-population.png', dpi=180)
    fig.savefig(target / 'reset-and-population.svg')
    plt.close(fig)
    fig, axes = plt.subplots(1, 2, figsize=(12, 5), layout='constrained')
    rows = matrix['matrix']
    axes[0].plot([r['mmr'] for r in rows], [r['mean_legal_lap'] for r in rows], 'o-', label='Mean validated AI lap')
    axes[0].plot([r['mmr'] for r in rows], [r['fastest_legal_lap'] for r in rows], 's--', label='Best validated AI lap')
    axes[0].set(xlabel='Current MMR / 10K scale', ylabel='Lap seconds', title='Real Unity physics / DAPEX + Whity / 3 seeds')
    axes[0].legend(fontsize=8)
    for group in gate['rendered']:
        axes[1].plot([r['seed'] for r in group['rows']], [r['one_percent_low_fps'] for r in group['rows']], 'o-', label=group['mode'])
    axes[1].axhline(60, color='#cc3333', linestyle='--', label='60 FPS')
    axes[1].set(xlabel='Seed', ylabel='Measured 1% low FPS', title='Final standalone render / all measured frames')
    axes[1].set_xticks([4207, 4211, 4219])
    axes[1].legend(fontsize=8)
    fig.savefig(target / 'native-pace-and-frame-pacing.png', dpi=180)
    fig.savefig(target / 'native-pace-and-frame-pacing.svg')
    plt.close(fig)


def run(args):
    target = BASE / 'AUDIT.md'
    if target.exists():
        raise FileExistsError('Existing final report is immutable: ' + str(target))
    core = read(args.core / 'benchmark.json')
    core_tests = read(args.core / 'contracts.json')
    matrix, gate = read(args.matrix), read(args.gate)
    menu = read(args.product / 'native-menu-performance.json')
    native_product = read(args.product / 'native-product-tests.json')
    baseline = read(BASE / 'continuation-baseline.json')
    manifest = read(BASE / 'final-source-build-manifest.json')
    integrity = read(BASE / 'final-integrity-checks.json')
    historical = read(BASE / 'existing-telemetry-comparison.json')
    adventure = read(BASE / 'inherited-adventure-matrix.json')
    test_files = [p for p in args.contracts.glob('*.json') if p.name.endswith(('tests.json', 'contracts.json'))]
    native = [read(p) for p in test_files]
    native_passed, native_failed = sum(r.get('passed', 0) for r in native), sum(r.get('failed', 0) for r in native)
    assert core['population'] == 10000 and core_tests['failed'] == 0
    assert native_failed == 0 and native_product['failed'] == 0 and integrity['failed'] == 0
    assert [r['mmr'] for r in matrix['matrix']] == RATINGS
    assert len(matrix['races']) == 24
    assert all(sorted(r['seeds']) == [4207, 4211, 4219] for r in matrix['matrix'])
    assert all(r['builds'] == [['formula-01', 'wheel-whity']] for r in matrix['races'])
    ram = gate['retained_ram']
    prestige = core['beforeReset']
    sections = []
    def section(title, content):
        sections.append('# ' + title + '\n\n' + content.strip() + '\n')

    section('CONTINUATION BASELINE', f'''
**VEREDICTO: FAIL para release candidate.** La migración y los contratos están validados; los gates de conducción élite, frame pacing, rendimiento móvil y comprobación humana siguen abiertos.

Se continuó el árbol sucio de `racing-climb-golden`, HEAD `{baseline['head']}`, sin reset, limpieza, commit ni push. Se preservaron las temporadas, títulos, anuncios, doce objetos, arte nativo, ML02 Shadow y correcciones anteriores de Adventure. El baseline congeló {len(baseline['files'])} archivos de Assets y sus hashes. La auditoría original permanece intacta.

Resultado final: **{native_passed}/{native_passed} contratos nativos**, **{core_tests['passed']}/{core_tests['passed']} contratos del núcleo**, **35/35 tests Python**, y **{native_product['passed']}/{native_product['passed']} comprobaciones del producto nativo**. Los ensayos de ajustes de IA rechazados y sus fallos se conservan como evidencia; no están en el código seleccionado.

{link(BASE / 'continuation-baseline.json')}; {link(BASE / 'FILES-MODIFIED.md', 'archivos y hashes antes/después')}; {link(args.contracts / 'suite.json', 'suite nativa final')}; {link(args.core / 'contracts.json', 'contratos del núcleo')}; {link(BASE / 'final-source-build-manifest.json', 'fuentes y build congelados')}; {link(BASE / 'final-integrity-checks.json', 'integridad final')}.
''')
    section('10K MMR MIGRATION', '''
La única escala normal actual es **0–10.000**; el prestigio comienza en 10.000. La migración V2→V3 comprime una sola vez MMR actual, picos, comienzos/finales de temporada y contadores históricos. Conserva identidades y época de instalación, mantiene los journals V2 intactos y añade los nuevos bots con cero MMR. El marcador de versión impide volver a dividir al reiniciar.

Se recalibraron deltas, incertidumbre, ventanas de matchmaking, curva estacional, IA continua, telemetría V3, presets, UI, títulos, historial y simulación. Los presets son **100, 1.000, 2.500, 5.000, 7.500, 9.000, 9.700 y 10.000**. Los valores 100K que aparecen en evidencia o fixtures de exportación antiguos representan archivos históricos, no una segunda escala activa.
''')
    names = ['ROOKIE', 'SCOUT', 'CHALLENGER', 'PRO', 'EXPERT', 'ELITE', 'MASTER', 'GRANDMASTER', 'TITAN', 'APEX']
    section('NEW RANKS', table(['MMR inclusivo', 'Rango'], [(f'{i*500:,}–{i*500+499:,}', names[i//2]+' '+('I' if i%2==0 else 'II')) for i in range(20)]) + '\n\nElite I/II conservan la dirección verde/turquesa de las dos referencias individuales. Cada rango usa presentación vectorial nativa; el badge y sus textos responden al MMR actual.')
    section('BEYOND 10K', '''
**APEX BEYOND 10K**, con texto dinámico `floor((MMR − 10000) / 1000)`: 10.000→◇0, 11.000→◇1, 12.000→◇2, 15.000→◇5 y 20.000→◇10. El número no se integra en una textura. Los picos históricos y títulos Beyond sobreviven a las temporadas y a la migración.
''')
    section('SEASON RESET', f'''
Cada instalación conserva su época y límites `epoch.AddMonths(3 × n)`. Jan 31→Apr 30→Jul 31 no deriva a Jul 30. Se procesa una vez cada límite omitido, con cursor compacto recuperable tras interrupción, UTC monotónica, journals dobles y hasta veinte temporadas de historia.

Para ≤10K, la curva de datos usa interpolación cúbica monótona entre estos puntos:

{table(['MMR final', 'MMR inicial siguiente'], [(0,0),(1000,980),(2500,2350),(5000,4500),(7500,6500),(9000,8200),(9500,9200),(10000,10000)])}

**Todo humano o bot >10.000 vuelve a exactamente 10.000.** La curva llega a ese límite sin salto grande: 9.999 y 10.001 se separan como máximo dos puntos después del reinicio. Conserva final, pico, rango, Beyond, resultados, títulos y combo. Los contratos recorren todos los enteros 0–10.000, comprueban migración única, reloj hacia atrás, cursor parcial, corrupción de archive y veinticinco límites offline.

![Curva de reinicio y distribución medida](charts/reset-and-population.png)
''')
    section('MMR ALGORITHM', f'''
En doce participantes, **P1–P5 ganan y P6–P12 pierden antes del suelo cero**. La expectativa se calcula contra cada uno de los once rivales con una logística inspirada en Elo/Glicko; rating e incertidumbre rival, desempeño esperado e incertidumbre propia ajustan la magnitud. No se usa únicamente la media del lobby.

El presupuesto compartido de ganancias/pérdidas se reparte con redondeo entero conservativo. Mil lobbies con outliers e incertidumbres diferentes conservaron suma cero y los dominios de signo. Un corredor de 100 que queda sexto frente a rivales de 500 pierde menos que un sexto de igual rating; no obtiene ganancias.

El suelo cero puede añadir masa de rating y se registra: {core['floorRatingCreated']:,} puntos en los 2.592.000 eventos estadísticos de esta prueba. Los contadores de actuaciones con ganancia y pérdida fueron {core['gains']:,} y {core['losses']:,}; no son sumas de puntos. La distribución emergente y la cola Beyond se muestran abajo; no se interpreta conservación por evento como ausencia automática de toda inflación a largo plazo.
''')
    section('LEADERBOARD COMBO', f'''
Cada fila muestra **vehículo + ruedas + personaje + accesorio** mediante una miniatura 3D cacheada de 192?108 por combinaci?n. Una c?mara de captura compartida genera solo las vistas visibles en men?s; una cach? LRU de 96 entradas reutiliza bitmaps entre identidades y suspende capturas durante Racing. El perfil permite consultar nombres, títulos y temporadas. El garage guarda personaje y accesorio; la identidad persistente también se aplica al corredor real y al showroom sin cambiar estadísticas, colliders ni masa.

Solo se mantienen **14 filas visibles**, con Top, vecindad, páginas y búsqueda por ID/posición. No se crean 10.000 filas. El combo sobrevive a reinicio, temporada y simulación, y solo cambia ocasionalmente por adaptación. En el Top100 observado había {len(prestige['top100Loadouts'])} combos completos distintos; {sum(r['count'] for r in prestige['top100Loadouts'] if r['combo'].startswith('3/5/'))} usaban DAPEX/Whity y los demás conservaron otros vehículos/ruedas.

![Leaderboard nativo con una miniatura del combo 3D por corredor]({Path(os.path.relpath(args.product / '04-evolved-leaderboard.png', BASE)).as_posix()})

{link(args.product / '08-native-selected-loadout.png', 'combo seleccionado')}; {link(BASE / 'visual-evidence.html', 'galería de capturas nativas')}.

Las siete referencias adjuntas guiaron objetos, Beyond, Night City, las dos láminas de rangos y Elite I/II. Se conservaron los doce modelos nativos y tres grupos de objetos por posición, insignias procedurales, asfalto húmedo, neón, skyline y exactamente Skyline Link/Apex Link. La pista real mide 3.050,17 m, tiene 192 nodos y tres sectores; el concepto de 3,8 km/cuatro sectores no se presenta como geometría implementada. La fidelidad visual premium requiere revisión humana.

{table(['Referencia adjunta', 'Aplicación'], [('Objetos / tres posiciones','Doce modelos y pickups nativos; grupos de primer bloque, medio y remontada'),('APEX superior a 100K','Presentación prestige adaptada a Beyond 10K; contador textual dinámico'),('Night City Circuit','Neón, asfalto húmedo, skyline y señalización alrededor de la topología existente'),('Rangos 0–50K','Primeros diez badges, ahora bandas 0–4.999'),('Rangos 50–100K','Últimos diez badges, ahora bandas 5.000–9.999'),('Elite I individual verde','Esmeralda/turquesa en Elite I, 5.000–5.499'),('Elite II individual verde','Esmeralda/turquesa en Elite II, 5.500–5.999')])}
''')
    section('BOT POPULATION ARCHITECTURE', f'''
**108 bytes/bot**, archivo de temporada **38 bytes/bot**; identidad, índices de catálogo, personalidad, actividad, habilidades, incertidumbre, forma, ratings y estadísticas empaquetados. Las cadenas repetidas se derivan de IDs y catálogos; solo se materializan perfiles ricos de oponentes o filas consultadas.

El índice de orden usa treap en cinco arrays de enteros, **20 bytes/entidad**, con actualización/rango en O(log N). El motor estadístico usa scratch buffers y RNG determinista, actividad diversa y batches de menú; no crea GameObjects, MonoBehaviours ni carreras físicas por bot. La carrera real instancia únicamente al jugador y once rivales. La liga se congela durante Racing.

Snapshot inmutable en el hilo principal y serialización, checksum, journals y archivos estacionales en worker; el cursor de cambio de temporada es persistente y procesa chunks. Los histories compactos se leen por demanda y liberan buffers archivados tras confirmación.

{table(['Población', 'Datos de bots', 'Índice', 'Una temporada', '20 temporadas'], [(f"{r['population']:,}",f"{r['raw_records']:,} B",f"{r['rank_index']:,} B",f"{r['one_season']:,} B",f"{r['twenty_retained_seasons']:,} B") for r in gate['storage_extrapolation']])}

Las filas de 100K/1M son **estimaciones aritméticas de almacenamiento**, no benchmarks ni aprobaciones móviles. RAM de snapshots, serialización, catálogos, runtime y Unity es adicional.
''')
    simulation = core['simulation']
    section('10K BOT BENCHMARK', f'''
Solo se ensayó la población **10.000**. Núcleo de producción C# ejecutado en proceso .NET Windows; rendimiento Unity de menú y carrera se mide por separado.

{table(['Medición', 'Resultado'], [
        ('Generación / índice',f"{number(core['generationMs'])} / {number(core['indexMs'])} ms"),
        ('Núcleo + índice retenidos',f"{ram['liveCoreIncreaseBytes']:,} B (proceso aislado)"),
        ('Snapshot / archive pendientes adicionales',f"{ram['snapshotAdditionalBytes']:,} / {ram['pendingSeasonArchiveAdditionalBytes']:,} B"),
        ('Dos journals maduros / metadata JSON',f"{core['twoJournalDiskBytes']:,} B / {core['metadataJsonBytes']:,} B"),
        ('Disco con una temporada',f"{core['diskWithOneSeasonBytes']:,} B"),
        ('Reload núcleo / startup Unity',f"{number(core['reloadMs'])} / {number(menu['startupMs'])} ms"),
        ('Snapshot núcleo / snapshot nativo',f"{number(core['snapshotMs'])} / {number(menu['saveMs'])} ms"),
        ('Worker save nativo',f"{number(menu['saveWorkerMs'])} ms"),
        ('Reset núcleo / escritura archive',f"{number(core['fullSeasonMs'])} / {number(core['seasonArchiveSaveMs'])} ms"),
        ('Matchmaking mediana / p99 / máximo',f"{number(core['matchmakingMsMedian'])} / {number(core['matchmakingMsP99'])} / {number(core['matchmakingMsMax'])} ms"),
        ('Top100 / vecindad 14',f"{number(core['top100MsMean'],5)} / {number(core['neighborhood14MsMean'],5)} ms"),
        ('Matchmaking GC por consulta de menú',f"{core['matchmakingAllocatedBytesMean']:,} B"),
        ('Scheduler Unity media / máximo',f"{number(menu['meanPumpMs'])} / {number(menu['maxPumpMs'])} ms")])}

{table(['Hasta día', 'Eventos acumulados', 'Eventos de este tramo', 'Tiempo tramo', 'Eventos/s tramo', 'GC batch'], [(s['throughDays'],f"{s['cumulativeEvents']:,}",f"{s['batchEvents']:,}",f"{number(s['milliseconds']/1000)} s",number(s['eventsPerSecond'],0),f"{s['allocatedBytes']:,} B") for s in simulation])}

Una semana suma los dos primeros tramos y noventa días suman los tres. Estas duraciones no son stalls de menú: el scheduler reparte trabajo con presupuesto alrededor de 1,5 ms. Los bytes de GC de batches incluyen medición, no asignaciones por cada evento. El GC global del Editor durante capturas de UI no mide GC de liga.

La fase A sigue el resultado de {link(args.gate, 'gates de población')}; startup, queries y snapshots pasan, pero los gates de FPS se evalúan abajo y Android permanece pendiente. No se usa la delta de heap post-season negativa de los primeros harnesses como ahorro: el proceso RAM aislado mide los buffers realmente retenidos.
''')
    section('100K BOT BENCHMARK', 'No ejecutado. La fase A tuvo un fallo de rendimiento medido; la instrucción de detener el escalado se respetó. No hay tiempos, RAM ni FPS medidos de 100.000 bots en esta migración.')
    section('1M BOT BENCHMARK', 'No ejecutado. La fase B no está aprobada. La arquitectura usa datos compactos e índice incremental, pero eso no acredita un millón de bots ni memoria/CPU Android. No es el default de producción.')
    section('RECOMMENDED PRODUCTION POPULATION', '**10.000 bots de datos**, como población seleccionada para el build de revisión. Es la mayor población medida en este pase; ninguna mayor está aprobada. La aprobación de release requiere resolver los gates restantes, especialmente frame pacing y Android. Mantener solo doce actores físicos evita ligar la población completa al coste de conducción.')
    variety = core['eliteVariety50Lobbies']
    section('MATCHMAKING VARIETY', f'''
Ranked usa once IDs reales de la liga, combo persistente y dificultad condicionada principalmente por MMR actual. El pool normal pondera proximidad, incertidumbre y actividad, amplía la ventana de forma controlada y reduce al 16 % el peso de los últimos 44 oponentes. Los rivales pueden volver a aparecer. Si el campo de prestigio es muy escaso, se toma la vecindad próxima de hasta 97 entidades, en lugar de muestrear toda la liga.

En cincuenta lobbies de 10K: **{variety['uniqueRivals']} rivales distintos/{variety['totalSlots']} plazas**, media de repetición consecutiva **{number(variety['meanConsecutiveRepeats'],5)}** y máximo **{variety['maximumConsecutiveRepeats']}**. La capacidad del pool depende de la distribución realmente existente; no se inventan rivales de rating alto en una instalación que acaba de comenzar en cero.
''')
    section('BOT DISTRIBUTION', f'''
Todos empiezan en cero, con talento, potencial de aprendizaje, forma, siete estilos y cuatro perfiles de actividad independientes. Las mejoras son lentas y acotadas, sin techo fijo individual de MMR. El rating puede subir, caer, estancarse y recuperarse; el talento latente no se convierte secretamente en fuerza 9.500 para un bot con 500 MMR en una carrera real.

{table(['Tiempo estadístico', 'Media', 'Mediana', 'p99', 'Máximo', 'En cero', 'Beyond'], [(f"{s['throughDays']} días",number(s['distribution']['mean'],2),s['distribution']['median'],s['distribution']['p99'],s['distribution']['max'],s['distribution']['zero'],s['distribution']['beyond']) for s in simulation])}

Antes del reinicio había **{prestige['beyond']} ({prestige['beyond']/100:.2f} %) en Beyond**, correlación talento/rating **{number(prestige['latentMmrCorrelation'],4)}**, y {prestige['zero']} todavía en cero. El máximo de {prestige['max']:,} revela una cola de prestigio amplia que debe ajustarse con uso real; no se recorta con un techo artificial ni se confunde con habilidad humana medida.

{table(['Rango', 'Bots al día 90'], [(r['rank'],r['count']) for r in prestige['bands']])}
''')
    fastest = matrix['fastest_verified_legal_ai']
    clean = matrix['fastest_verified_zero_recovery_ai']
    fastest_text = (f"{number(fastest['best_lap'])} s, MMR {fastest['mmr']:,}, seed {fastest['seed']}, {fastest['recovery_events']} eventos de recovery en su sesión" if fastest else 'ninguna sesión elegible')
    clean_text = (f"{number(clean['best_lap'])} s, MMR {clean['mmr']:,}, seed {clean['seed']}" if clean else 'ninguna sesión elegible')
    section('ML', f'''
Telemetría **3.0.0**, `ratingScaleMaximum=10000`, `mmr-0-10k-1`, 33 features/8 salidas. Runtime normaliza `clamp01(MMR / 10000)`; V2 conserva su normalización archivada explícita únicamente al leer evidencia antigua. Beyond satura el conditioning competitivo. Los grupos nuevos de objetos mantienen el encoding de familias mecánicas de los modelos instalados.

**ML02 Shadow**: el candidato local `{manifest['candidate']['sha256']}` predice y registra comparación causal al siguiente comando, sin escribir comandos físicos. ML01 y los dos .pt instalados mantienen sus hashes. No se retrenó a ciegas ni se habilitó Assist/Authority. La evaluación held-out histórica no supera todos los baselines válidos, especialmente persistencia continua y predicción de salto/aerial; paridad de inferencia no acredita mejor conducción.

Matriz actual: **{matrix['legal_sessions']}/{matrix['telemetry_sessions']} sesiones aceptadas**. Las rechazadas se excluyen del pace y permanecen intactas con motivos en {link(args.matrix)}. Vuelta AI verificada más rápida: **{fastest_text}**. Sesión AI más rápida sin recovery: **{clean_text}**. Ambas son referencias AI, nunca ground truth humano. Off y Shadow usan el mismo controlador heurístico; la inferencia Shadow no constituye una política competitiva distinta.

La mejor telemetría AI archivada se volvió a validar: **{number(historical['archived_ai']['best_lap'])} s**, NovaShift, MMR antiguo 97.000 (equivalencia de rating 9.700), seed 4211, DAPEX/Whity. La mejor vuelta actual es **{number(historical['current_vs_archived_best_percent'],3)} %** más lenta. Son versiones y corridas históricas diferentes; esa diferencia no mide el gap experto humano ni prueba por sí sola una regresión causal del controlador. Los ocho niveles se comparan entre sí, con el heurístico actual, su mejor vuelta legal y esa referencia histórica en {link(BASE / 'existing-telemetry-comparison.json', 'comparación de telemetría AI verificada')}.

Se volvió a buscar en `Tools/ML/Data` y el estado del gap experto 0–2 % es **{matrix['expert_gap_status']}**. Solo se usaría el rótulo “Best verified human telemetry currently available — provisional expert target.” para una sesión claramente humana, de pista/revisión y equipo actuales, que pase integridad y legalidad. No se inventó ghost ni marca experta.
''')
    elite = [r for r in matrix['races'] if r['mmr'] == 10000]
    rendered_pack_rows = []
    for group in gate['rendered']:
        for row in group['rows']:
            metric_path = Path(row['evidence'])
            metric = read(metric_path)
            race = read(metric_path.with_name('race.json'))
            times = [r['finishTime'] for r in metric['racers'] if r['finished']]
            full_pack = len(times) == 12
            rendered_pack_rows.append([group['mode'], row['seed'], f'{len(times)}/12',
                12-len(times), row['recoveries'], number(min(times) if times else None),
                number(max(times) if full_pack else None),
                number(max(times)-min(times) if full_pack else None), race['state'],
                race['runtimeErrors']])
    seed_time_rows = []
    for mmr in RATINGS:
        races = sorted((r for r in matrix['races'] if r['mmr']==mmr), key=lambda r:r['seed'])
        seed_time_rows.append([mmr,' / '.join(number(r['p1_finish']) for r in races),
            ' / '.join(number(r['p12_finish']) for r in races),
            ' / '.join(number(r['complete_p1_p12_spread']) for r in races),
            sum(r['collisions'] for r in races),sum(r['walls'] for r in races)])
    section('HIGH-END PACK', f'''
El baseline heredado era 35/36 finishers, 28 recoveries y un DNF, con separación excesiva. Los ajustes de ruta, lanzamiento y frenado ensayados no pasaron el gate y fueron rechazados; el controlador seleccionado conserva exactamente el archivo del baseline de migración. Las corridas de variantes rechazadas no se mezclan con esta matriz final.

{link(BASE / 'ai-experiments.json', 'resultados de las cinco variantes descartadas')}.

{table(['Seed 10K', 'Finishers', 'DNF', 'Recoveries', 'P1', 'P12', 'P1→P12'], [(r['seed'],f"{r['finished']}/12",r['dnfs'],r['recoveries'],number(r['p1_finish']),number(r['p12_finish']),number(r['complete_p1_p12_spread'])) for r in elite])}

En el ejecutable final, render real y TimeScale 1, se observaron también estos resultados independientes:

{table(['Modo', 'Seed', 'Llegadas', 'DNF', 'Recovery', 'P1', 'P12', 'Spread completo', 'Estado final', 'Errores QA'], rendered_pack_rows)}

La primera semilla Shadow no alcanzó Results: el piloto del slot humano, controlado por QA, quedó atascado; once bots llegaron. Al agotarse el plazo se registró además el error QA de botón RACING HUB ausente, sin retorno al menú. Se conserva ese fallo y todo el tramo hasta el timeout. El tiempo prolongado después de terminar los rivales cambia el trabajo renderizado; las comparaciones de FPS entre procesos no aíslan el coste causal de ML o de la población.

**Gate de pack limpio: {'PASS' if matrix['all_elite_clean_pack_gates'] else 'FAIL'}**. Se exige doce llegadas, cero recoveries y aproximadamente 3–8 s P1→P12. En una carrera con DNF, P12 y spread completo no se calculan a partir del último finisher. No se modificaron timeout, checkpoint legality, stats, fuerzas, masa ni pad físico para ocultar fallos.

{table(['MMR', 'Llegadas /36', 'DNF', 'Recovery', 'Lap medio legal', 'Best legal', 'Skyline aciertos/entradas', 'Apex aciertos/entradas', 'Saltos', 'Landing boosts', 'Cadenas', 'Objetos usados'], [(r['mmr'],f"{r['finished']}/36",r['dnfs'],r['recoveries'],number(r['mean_legal_lap']),number(r['fastest_legal_lap']),f"{r['routes'].get('crest_cut',{}).get('completions',0)}/{r['routes'].get('crest_cut',{}).get('entries',0)}",f"{r['routes'].get('return_drop',{}).get('completions',0)}/{r['routes'].get('return_drop',{}).get('entries',0)}",r['jumps'],r['landing_boosts'],r['chains'],r['items']) for r in matrix['matrix']])}

Las 24 carreras físicas usan DAPEX/Whity y tres seeds por nivel. TimeScale 6 acelera el ensayo headless; **no mide FPS**. La media de vueltas proviene de laps en telemetría aceptada, incluso cuando la sesión no termina todas las vueltas, por lo que se informa también DNF. Los CSV conservan P1/P12, colisiones, objetos y técnicas por carrera; la tabla no oculta variabilidad entre seeds.

Las medias legales descienden monótonamente de 124,661 s a 71,497 s. La fiabilidad y las recuperaciones no mejoran de forma monótona; la mejora de ritmo no aprueba ese gate.

Tiempos por seed en orden 4207 / 4211 / 4219; «—» conserva el DNF:

{table(['MMR', 'P1 segundos', 'P12 segundos', 'Spread completo segundos', 'Colisiones', 'Paredes'], seed_time_rows)}

{link(args.matrix.with_suffix('.csv'), 'CSV de las 24 carreras físicas')}; {link(args.matrix, 'sesiones aceptadas y rechazadas, rutas, técnicas y fuentes')}.
''')
    section('ADVENTURE', f'''
Matriz real Box2D de 25 combinaciones: **{adventure['passed']}/25 PASS y {adventure['failed']} FAIL**, deduplicando las líneas que emiten probe y runner. Fallan `car-01`, `rally-01` y `sport-bike-01` en Mountains, por avance/reversa o recuperación de aterrizaje. No se consideran falsos positivos resueltos ni muertes esperadas sin evidencia adicional.

Todos los rigs conservaron valores finitos y joints; el contrato del teleport real con chassis girado 65° verificó error de geometría de rueda **1,682005 m→0 m** en la corrección heredada. Este pase mantiene byte por byte ese controlador 2D y las fuerzas/estadísticas compartidas 3D. No se amplió la edición mecánica para colorear el resultado.

{link(BASE / 'inherited-adventure-matrix.json', 'matriz y fallos exactos de Adventure')}.
''')
    performance_rows = []
    for group in gate['rendered']:
        pooled, weighted = group['pooled_actual_frames'], group['weighted']
        performance_rows.append([group['mode'],f"{pooled['frames']:,}",number(pooled['average_fps'],2),number(pooled['one_percent_low_fps'],2),number(weighted['ai_cpu_ms'],5),number(weighted['physics_cpu_ms'],5),number(weighted['render_cpu_ms'],5),number(weighted['gc_bytes_per_frame'],2),number(weighted['draw_calls'],1)])
    memory_rows = []
    for mode in ('Off','Shadow'):
        path = BASE / ('final-' + mode.lower() + '-process-memory.csv')
        with path.open(encoding='utf-8-sig',newline='') as stream:
            samples = list(csv.DictReader(stream))
        assert samples
        memory_rows.append([mode,len(samples),number(max(int(r['working_set_bytes']) for r in samples)/1048576,2)+' MiB',number(max(int(r['private_bytes']) for r in samples)/1048576,2)+' MiB'])
    shadow_rows = next(group['rows'] for group in gate['rendered'] if group['mode']=='Shadow')
    ml_ticks = sum(row['ml_ticks'] for row in shadow_rows)
    ml_network_us = sum(row['ml_network_us']*row['ml_ticks'] for row in shadow_rows)/ml_ticks
    ml_tick_us = sum(row['ml_full_tick_us']*row['ml_ticks'] for row in shadow_rows)/ml_ticks
    section('PERFORMANCE', f'''
Player Windows Development real, High 1600×900, DX11, i7-11800H/RTX 3070 Laptop, cámara y HUD completos a RenderTexture, TimeScale 1, uncapped, tres seeds por modo. La prueba es offscreen, con evidencia de render nativo; no es una aprobación onscreen humana ni Android. La media y el 1 % low se calculan sobre todos los frames reales, sin eliminar pausas ni outliers.

{table(['Modo final', 'Frames', 'FPS medio', '1 % low', 'AI ms', 'Physics ms', 'Render ms', 'GC global B/frame', 'Draw calls'], performance_rows)}

{table(['Modo', 'Δ FPS frente a optimizado previo', 'Δ 1 % low'], [(r['mode'],number(r['average_fps_change_percent'],2)+' %',number(r['one_percent_low_change_percent'],2)+' %') for r in gate['comparisons']])}

ML02 realizó {ml_ticks:,} ticks nativos Shadow: media ponderada de **{number(ml_network_us)} µs** de inferencia y **{number(ml_tick_us)} µs** de tick completo. Incluye medición de su propuesta; no significa mejora de control.

En las seis carreras la liga congelada se mide con un contador separado:

{table(['Modo / seed', 'Pump ms', 'GC bytes total', 'Eventos de liga'], [(f"{r['mode']} / {r['seed']}",number(r['pump_ms'],7),r['pump_allocated_bytes'],r['simulation_events']) for r in gate['league_freeze']])}

Gate estable 60 FPS: **{gate['gates']['stable_60fps']}**. Sin regresión medida frente al optimizado previo: **{gate['gates']['no_measured_fps_regression']}**. Phase A desktop: **{gate['gates']['phase_a_desktop_pass']}**. Se mantuvo detenido el escalado después del fallo inicial de fase A. Las diferencias entre corridas incluyen variación de SO/carga/temperatura; no se atribuyen causalmente a la liga únicamente por comparar procesos distintos. Los contadores inclusivos de profiler pueden solaparse. GPU timing no disponible no significa coste cero de GPU.

En la primera semilla Off final, PlayerLoop llegó a 364,72 ms, BehaviourUpdate a 243,88 ms, RenderLoop a 218,12 ms y Semaphore.WaitForSignal a 361,51 ms; GC collection tuvo un máximo de 2,97 ms, sin Instantiate/Destroy. Es evidencia de demoras amplias en el frame, sin identificar por sí sola una causa única. Las observaciones del host están en {link(BASE / 'profile-host-snapshot-20261006-01.json')}; no se usaron para descartar frames ni aprobar el rendimiento.

![Ritmo físico y frame pacing final](charts/native-pace-and-frame-pacing.png)

RAM de liga aislada: {ram['liveCoreIncreaseBytes']:,} B; snapshot adicional {ram['snapshotAdditionalBytes']:,} B; archive estacional {ram['pendingSeasonArchiveAdditionalBytes']:,} B. Memoria del proceso Unity completo incluye render, assets y motor y no se presenta como bytes de liga. No se detectó dispositivo en ADB; CPU/RAM/FPS, banner/consentimiento y compras reales en Android siguen pendientes.

Muestreo externo del proceso cada cinco segundos; máximos **observados**, no garantía de captura del pico instantáneo. Private bytes y memoria residente no son la misma medición:

{table(['Modo', 'Muestras', 'Working set observado máximo', 'Private bytes observado máximo'],memory_rows)}

{link(BASE / 'final-off-process-memory.csv', 'RAM proceso Off')}; {link(BASE / 'final-shadow-process-memory.csv', 'RAM proceso Shadow')}.

Se preservaron los contratos de anuncios: banner elegible por defecto, VIP solo con suscripción real, compras confirmadas añaden treinta días desde max(now, fin), transacciones/restores deduplicados y reloj monotónico, independientes del reset estacional. `vipSubscriptionProductId` sigue vacío: **CONFIGURATION PENDING**; no se inventó un ID de tienda.
''')
    section('HUMAN GATE BUILD', f'''
`C:\\Peru3124\\restoredclimb\\ClimbAnime\\Builds\\HumanGateLatest\\HillAdventurer-HumanGate.exe`

Windows x86_64, Development ON, build actual del árbol seleccionado. {manifest['build_summary'].strip().replace(chr(10), '; ')}. Se congelaron los hashes de todos los archivos del build y de Assets; la verificación final confirma que permanecen iguales. Las validaciones aislaron sus journals y restauraron los 442 valores PlayerPrefs protegidos tras cada corrida.

{link(BASE / 'Launch-HumanGate.ps1', 'lanzador humano ML02 Shadow')}; {link(BASE / 'HUMAN-GATE.md', 'controles, grabación y checklist')}. El lanzador interactivo no adjunta driver QA ni inventa resultados. Se prepara para los registros expertos dedicados posteriores; no se ejecutó como playtest humano.
''')
    blockers = ['Pack élite: fiabilidad y separación limpia 3–8 s no aprobadas.',
                'Shadow seed 4207: piloto QA atascado, sin Results/retorno al menú y error QA al buscar RACING HUB.',
                'Los tres fallos de Adventure/Mountains permanecen abiertos.',
                'Gap experto 0–2 %: HUMAN GATE PENDING, sin ghost humano autoritativo.',
                'Rendimiento, memoria, controles/banner y ciclo real de tienda/consentimiento en Android sin dispositivo validado.',
                'VIP: CONFIGURATION PENDING por product ID sin configurar; no se certificaron cobros/restores reales.',
                'Acabado visual premium y legibilidad requieren el Human Gate; las capturas automatizadas no reemplazan juicio humano.']
    if not gate['gates']['stable_60fps'] or not gate['gates']['no_measured_fps_regression']:
        blockers.insert(1, 'Racing frame pacing/FPS: la fase A no conserva todos los gates del baseline optimizado.')
    section('REMAINING BLOCKERS', '\n'.join('- ' + b for b in blockers) + '\n\nNo se creó commit ni se hizo push. El ejecutable y la evidencia quedan disponibles para revisión; **FAIL** no se convierte en PASS por la cantidad de contratos aprobados.')
    plots(core, matrix, gate, args.core / 'reset-curve.csv')
    target.write_text('\n'.join(sections), encoding='utf-8')
    gallery = ['<!doctype html><html lang="es"><meta charset="utf-8"><title>Hill Adventurer / evidencia 10K</title>',
               '<style>body{background:#071421;color:#e7f4ff;font:16px system-ui;max-width:1200px;margin:30px auto;padding:20px}img{width:100%;height:auto;border:1px solid #28bbdb}figure{margin:30px 0}a{color:#66dcff}figcaption{margin:8px 0}</style>',
               '<h1>Evidencia nativa de la migración 10K</h1><p>Capturas de Unity, sin retoque. Validación automatizada; revisión humana y Android pendientes. <a href="AUDIT.md">Auditoría completa</a>.</p>']
    for filename, caption in [('02-native-rank-ladder.png','Rangos actuales 0–10K y Beyond con texto dinámico'),('04-evolved-leaderboard.png','Leaderboard virtualizado con cuatro iconos de combo persistente'),('08-native-selected-loadout.png','Personaje y accesorio del combo seleccionado'),('07-native-twelve-item-meshes.png','Doce modelos de objetos nativos')]:
        relative = Path(os.path.relpath(args.product / filename, BASE)).as_posix()
        gallery.append(f'<figure><a href="{html.escape(relative)}"><img src="{html.escape(relative)}" alt="{html.escape(caption)}"></a><figcaption>{html.escape(caption)}</figcaption></figure>')
    gallery.append('</html>')
    (BASE / 'visual-evidence.html').write_text('\n'.join(gallery), encoding='utf-8')
    print(json.dumps(dict(report=str(target), native_passed=native_passed, core_passed=core_tests['passed'], races=len(matrix['races']), verdict='FAIL')))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    for name in ('core', 'product', 'contracts', 'matrix', 'gate'):
        parser.add_argument('--' + name, type=Path, required=True)
    run(parser.parse_args())
