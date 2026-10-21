# Reproducción local

Unity `6000.3.13f1`, Windows/D3D11, GPU y configuración indicadas en los JSON. Trabajar en `main`. Elegir **destinos nuevos** en todos los comandos: los nombres siguientes describen esta ejecución y ya contienen evidencia. No volver a utilizarlos para sobrescribir resultados. Conservar un snapshot de fuentes, saves y registro antes de iniciar; `baseline-inventory.json` y `environment.json` documentan los originales de esta ejecución.

La herramienta `Tools/RC/release_gate_inventory.py <carpeta-nueva>` captura fuentes, escenas y artefactos. Los datos privados y exportaciones del registro deben quedarse en `tmp/ReleaseGates/`, separados de la evidencia versionable. Las claves se verifican por hash; no se modifican ni se copian a la evidencia pública.

## Contratos

```powershell
dotnet build Tools/CompetitiveQA/CompetitiveQA.csproj --no-restore -c Release --nologo
dotnet Tools/CompetitiveQA/bin/Release/net10.0/CompetitiveQA.dll contracts <salida-nueva>
dotnet Tools/CompetitiveQA/bin/Release/net10.0/CompetitiveQA.dll seasons <salida-nueva>
dotnet Tools/CompetitiveQA/bin/Release/net10.0/CompetitiveQA.dll mmr-contracts <salida-nueva>
dotnet Tools/CompetitiveQA/bin/Release/net10.0/CompetitiveQA.dll release-lookup <salida-nueva>
```

Los contratos de MMR/AFTER son experimentales y no activan esa propuesta. El proyecto .NET comparte el núcleo de liga de Unity; no acredita conducción física.

## Unity, render y carreras

Los métodos Editor utilizados son `AnimeHillRacing.EditorTools.RacingRainbowReviewBatch.ReviewAndBuild` para las nueve parejas visuales y primera build, y `BuildValidated` para la build con el capturador corregido. Se ejecutan con `-batchmode -quit -projectPath <este-repositorio> -force-d3d11 -executeMethod <método> --closure-output <salida-nueva> --rc-build-path <exe-nuevo> -logFile <log-nuevo>`. Mantener gráficos habilitados; `-nographics` no acredita render.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/RC/Run-ReleaseRaceMatrix.ps1 -Player <build-BlacklineQA> -Output <salida-nueva> -RainbowPreview
python Tools/RC/summarize_release_matrix.py <matriz> <analisis-nuevo>
python Tools/RC/analyze_elite_forensics.py <matriz> <forense-nuevo>
python Tools/RC/analyze_release_recoveries.py <matriz/mmr-10000> <json-nuevo>
```

La matriz oficial usa MMR 9700/10000, seeds 4207/4208/4209, High, 1600×900, escala 1 y ML Off. La identidad de humano es controlada por un script QA. El flag PASS del smoke heredado no impone 72/72 ni el gap élite; el analizador independiente sí lo hace. No deducir rendimiento a partir de estas ventanas ocultas.

La semilla independiente usa `-Ratings 10000 -Seeds 4301`. La integración Ranked usa `-Ratings 10000 -Seeds 4302 -Mode final-qa`; sus rivales, dificultad y objetivo difieren del benchmark. Cada tanda guarda sus hashes y argumentos.

El contrato PlayMode de interfaz se ejecuta mediante `AnimeHillRacing.EditorTools.RacingCompetitiveDisplayContracts.Run`, sin `-quit`: el propio harness sale al completar lógica y capturas. Sus ejemplos de rating son fixtures identificados.

Tras la carrera Ranked, `physical-readback` del CLI .NET contrasta métricas y recibo del save QA existente. `Tools/RC/Run-ReleaseNativeReadback.ps1` copia ese mismo save a una salida nueva y verifica la búsqueda en otro proceso nativo. No sustituir rivales ausentes por identidades nuevas.

## Rendimiento, laboratorio y Android

`Tools/RC/Measure-ReleasePerformance.ps1 -Player <build> -EvidenceRoot <carpeta>` requiere ausencia de otros Unity, corriente estable y foco durante ambas ventanas. Los CSV se rechazan íntegros si pierden foco. El reanálisis histórico utiliza `analyze_performance.py <datos-originales> --output <json-nuevo>` y copias de manifiestos en otra carpeta; no se cambia el par original ni se presenta como medición de la build nueva.

```powershell
dotnet Tools/CompetitiveQA/bin/Release/net10.0/CompetitiveQA.dll mmr-lab <salida-nueva> 4 10000 4207 organic existing
dotnet Tools/CompetitiveQA/bin/Release/net10.0/CompetitiveQA.dll mmr-lab <salida-nueva> 4 10000 4208 organic existing
dotnet Tools/CompetitiveQA/bin/Release/net10.0/CompetitiveQA.dll mmr-lab <salida-nueva> 4 1200 4209 mobility existing
dotnet Tools/CompetitiveQA/bin/Release/net10.0/CompetitiveQA.dll release-population <salida-nueva> 10000
dotnet Tools/CompetitiveQA/bin/Release/net10.0/CompetitiveQA.dll release-population <salida-nueva> 100000
```

El laboratorio orgánico usa el scheduler y la economía reales del simulador. Mobility modifica habilidad/rating de un fixture; debe informarse por separado. Ninguno simula una persona ni activa 100k en producción.

Android forense usa `AnimeHillRacing.EditorTools.RacingRcEvidenceBuilder.BuildAndroidSizeBatch` y un AAB nuevo. El constructor conserva la configuración y usa exclusivamente la identidad debug existente; no instala, distribuye ni usa firma productiva. `Tools/RC/Measure-AndroidDelivery.ps1 -Bundle <aab> -Apks <apks-nuevo> -Output <salida-nueva> -CreateApkSet` mide ARM64/API35/en y siete densidades con bundletool 1.17.2. Es descarga comprimida calculada, no tamaño instalado ni aprobación Play/dispositivo.

Finalizar con `verify_release_integrity.py`, exportación de logs sin credenciales y `finalize_release_evidence.py`, siempre con manifiestos nuevos. Revisar [HumanGate](MANUAL-QA.md) y conservar todos los gates que requieran dispositivo o aprobación humana como pendientes.
