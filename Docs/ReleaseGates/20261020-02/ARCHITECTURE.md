# Inventario verificado — 20 octubre 2026

Rama `main`; HEAD inicial `152279fb5a4cf443a90d31654ad7a90f51e5abb3`. El punto de partida de esta ejecución contiene trabajo local anterior: diez archivos modificados y fuentes/herramientas/evidencia sin seguimiento. No se considera trabajo nuevo ni una aprobación del usuario lo que afirme un informe previo. El TXT recibido es idéntico por SHA-256 al original del 19 de octubre.

Antes de ejecutar Unity se guardaron 935 fuentes/configuraciones y sus hashes, el inventario de 32 escenas/prefabs, 20 archivos de persistencia productiva y una exportación privada de PlayerPrefs productivo. Las copias privadas están en `tmp/ReleaseGates/20261020-02/`; los manifiestos públicos no contienen sus datos ni claves. Los ocho artefactos/modelos referenciados por el informe inicial coinciden con su SHA-256.

| Subsistema | Arquitectura / estado real |
| --- | --- |
| Entrada | Nueve escenas habilitadas. Flujo RacingSelect → RacingGarage → Racing3D; RacingSceneBootstrap crea el circuito en ejecución |
| Circuito físico | NightCityCircuit → TrackSplineGraph → RacingTrackGeometry / CanyonRushBuilder. 192 nodos, ocho checkpoints, dos shortcuts; doce plazas |
| Presentación | Built-in Render Pipeline; RacingRainbowScenery y dos shaders nuevos preexistentes generan bandas, marcos, refuerzos, flechas y cielo orbital. Propuesta original restringida a `--rainbow-preview` en Editor/Development |
| Colisiones / navegación | A/B D3D nuevo: hashes idénticos, 31 colliders. Se preserva el trazado físico: no hay una prueba causal que justifique cambiarlo ni invalidar récords |
| Calidad | RacingQualityScope Low/Medium/High. Capturas High nocturnas y simulación diurna/Low en host. Low de Windows no acredita Android |
| IA / vehículos | AiVehicleController / AiSkillModel / AiItemAgent; RacingVehiclePhysics y VehicleRecovery compartidos. No se modifican decisiones, capacidades, tiempos, colisiones ni standings en esta ejecución |
| ML | Recursos ML01 y candidato ML02 preservados. Producción Off; el override QA convierte cualquier modo no Off en Shadow. Ningún entrenamiento ni promoción |
| Rating / temporadas | CompetitiveCoordinator, HA-PW2, ledger y recibos persistentes. Un humano + once bots en Ranked físico; doce bots en el simulador. Placements iniciales 0/3 y estacionales 0/1 separados. Época 2026-10-01 UTC, trimestres naturales, reset Hermite vigente |
| Persistencia | RacingProgressionService → LocalLeagueStore / journals / historial. Las builds QA llevan compañía nativa HillAdventurer.BlacklineQA; la liga de pruebas se redirige a `--closure-output/isolated-product-save` |
| Resultados / búsqueda | Cambios preexistentes muestran el recibo confirmado e indexan IDs/nombres. Se corrige la prioridad de nombres numéricos frente a posiciones: 36 contratos PlayMode y once rivales reales buscables tras relanzar. No se fabrican rivales ni rating |
| Población | Default productivo 10.000. 100.000 opt-in QA; presencia sintética LOCAL OFFLINE. Los benchmarks usan procesos y saves separados |
| Android | Versión 1.2 / code 20, IL2CPP ARM64; servicios AdMob, GPGS, IAP y SAF requieren dispositivo y cuentas de prueba. Sin publicación ni firma de producción |

La propuesta visual no implica aprobación humana. El coche encontrado es META DAPEX azul/Whity, distinto del Formula amarillo/cyan descrito en el TXT. El usuario confirmó expresamente «Conservar el coche actual (DAPEX azul/Whity)» durante esta ejecución; se conserva. Esta decisión no aprueba la pista ni una release; véase `user-decisions.json`.

Hay un Unity abierto de otro proyecto. No se cierra ni modifica. Las carreras ocultas de QA no acreditan rendimiento con foco ni una comparación ML controlada. El informe final debe distinguir las mediciones históricas reanalizadas de las nuevas.
