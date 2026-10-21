# Inventario Racing 3D — 19 octubre 2026

Estado inicial: `main`, HEAD `152279fb5a4cf443a90d31654ad7a90f51e5abb3`, árbol limpio antes de crear esta evidencia. No se ha cambiado de rama ni ejecutado commit/push/pull/reset/clean. El informe anterior parte de `3be7666`; sus ocho artefactos publicados localmente conservan sus hashes. Eso acredita identidad de archivos, no ejecución ni aprobación nuevas.

| Subsistema | Fuente y dependencia | Estado comprobado inicialmente |
| --- | --- | --- |
| Entrada y escenas | EditorBuildSettings: PreStart → RacingSelect → RacingGarage → Racing3D; RacingSceneBootstrap y RaceManager | 9 escenas habilitadas; escena Racing3D genera el circuito en runtime |
| Ruta física | NightCityCircuit → TrackSplineGraph → RacingTrackGeometry; CanyonRushBuilder | 192 nodos, 24 puntos de control interpolados, 8 checkpoints, 2 shortcuts, 12 plazas. Contratos Unity actuales sin fallos |
| Presentación | Built-in Render Pipeline; CanyonRushBuilder, RacingSkyTrack, RacingNightSky, Resources/RacingPrismaticRoad.shader | Material prismático y ciudad bajo cielo estrellado; no existe aún aprobación humana de Rainbow Road |
| Calidad | RacingQualityScope Low/Medium/High | High usa MSAA 4, 2 luces, sombras duras 85 m; Medium MSAA 2; Low sin sombras. Ninguna prueba host equivale a Android |
| Conducción | AiVehicleController, AiSkillModel, AiItemAgent; RacingVehiclePhysics y VehicleRecovery compartidos | Evidencia anterior: 72/72, 0 DNF, 7 recuperaciones; gap 15,20–27,06 s. Gate élite anterior FAIL |
| ML | RacingMlExecutionPolicy / RacingMlInferenceAdapter; Resources/RacingMlWeights.bytes | ML01 y candidato ML02 con hashes preservados; producción Off; override permitido solo en Development BlacklineQA; autoridad limitada a Shadow |
| Competitivo | CompetitiveCoordinator → HA-PW2 → Ledger → LocalLeagueEngine / LeagueSimulator | 29 CPPS y 142 contratos de expansión repetidos: cero fallos. Habilidad y rating separados; 1 humano + 11 bots reales reservados |
| Temporadas | GlobalSeasonCalendar, LocalSeasonEngine, SeasonQualification, títulos y SeasonHistoryStore | Época 2026-10-01 UTC; trimestres; placements 0/3 y 0/1 separados; reset Hermite existente |
| Persistencia | RacingProgressionService, LocalLeagueStore, journals/recibos, PlayerPrefs | Save productivo respaldado y con hashes en environment.json; QA league redirigida mediante --closure-output; binario QA con compañía separada verificada en app.info |
| Leaderboard | LeagueRankIndex + RacingLeaderboardUI.Locate | Consulta por posición/ID presente; búsqueda por nombre ausente en fuente: defecto reproducible por contrato pendiente |
| Población | LocalLeagueConfiguration, CompactLeagueBot, migración opt-in | Default 10.000; 100.000 solo opt-in QA; presencia sintética LOCAL OFFLINE; no humanos online |
| Android | ARM64, min SDK 25, IL2CPP, versión 1.2/code 20; AdMob/GPGS/IAP/SAF | Configuración auditada por suites Unity; adb sin dispositivos. Servicios reales BLOCKED |
| Distribución | AAB forense anterior y bundletool | AAB 151.231.753 B conservado; entrega histórica 147.953.167 B para ARM64/API35/densidad420/en. Pendiente recalcular, no aprobación Play |

Los snapshots originales de 905 fuentes/configuraciones y del save productivo están fuera de la evidencia versionable, en `tmp/ReleaseGates/20261019-01/`. El manifiesto versionable contiene hashes y rutas, sin copiar credenciales. Se conservaron todos los builds y logs anteriores.

La medición de rendimiento requiere ausencia de otros Unity bajo carga. Hay otro proyecto abierto (`C:/Peru3124/31NuevoPeru3D/n2wrsy31fa`), que esta sesión no cierra ni modifica. No hay dispositivo Android ni aprobación humana expresa. Estos límites no convierten en PASS los gates correspondientes.
