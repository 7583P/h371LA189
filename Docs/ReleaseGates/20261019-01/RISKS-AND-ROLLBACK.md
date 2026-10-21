# Cambios, riesgo y rollback

Los originales están en `tmp/ReleaseGates/20261019-01/before/`, con rutas relativas al repositorio. `baseline-inventory.json` contiene los hashes iniciales. Los snapshots intermedios tienen nombres propios en la misma carpeta `tmp`; no sustituyen al original. `changed-files.json` y `integrity-final.json` identificarán la revisión entregada. No ejecutar git reset/checkout/clean para restaurar estos cambios.

| Cambio | Archivos originales / nuevos | Impacto y verificación |
| --- | --- | --- |
| Reconstruir mallas nativas destruidas | `Presentation/RacingVehicleVisuals.cs` | Siete guards `cached != null`; reutiliza los mismos generadores. Reproducción tras recarga de escena: cuatro fallos de producto antes; producto 52/0 y RC después. Sin cambios de diseño, física o loadout |
| Mostrar el settlement definitivo | `UI/RacingResultsUI.cs` | Tabla, badges y animación usan el recibo confirmado. Sin recibo se indica UNSETTLED. PlayMode compara datos y widgets reales; repetir la vista no aplica rating |
| Buscar identidades existentes | `UI/RacingLeaderboardUI.cs`; nuevo `Competitive/LeagueIdentityLookup.cs` | Índice perezoso de nombre exacto/ID; nombres duplicados requieren desambiguar por ID. No crea perfiles. 145 contratos, reservas y recarga en 10k/100k; 14 filas reutilizadas |
| Preparar la liga antes de reservar Ranked | `Core/RaceManager.cs`, `MMR/RacingProgressionService.cs` | Catch-up limitado al presupuesto normal, antes de Preparing; reserva conserva el instante pausado. Se preservan calendario, journal, reglas y pausa durante carrera. Reproducción nativa sin recibo y contrato de reloj rojo/verde; comprobar también el readback físico final |
| Propuesta orbital original | `Environment/CanyonRushBuilder.cs`; nuevos `RacingRainbowScenery.cs`, `RacingRainbowRoad.shader`, `RacingRainbowStructure.shader` | Mallas visuales, bandas, señales, arcos y cielo. Solo Editor/Development con `--rainbow-preview`. Navegación y 31 colliders idénticos; nueve vistas A/B. Aprobación humana pendiente |
| Capturas de eventos reales | `Diagnostics/RacingClosureProbe.cs` | Solo QA explícito: recuperación, ruta comprometida y aterrizaje. No cambia comandos ni cámara de juego. El primer capturador omitía recuperaciones al usar Camera.main con render oculto; conservar sus resultados como anteriores a la corrección. La evidencia debe indicar el binario exacto |
| Builds forenses seguros | `Editor/RacingRcEvidenceBuilder.cs` | Windows rechaza sobrescribir EXE. Android forense usa debug, sin leer contraseñas productivas; restaura backend/arquitecturas. No instala ni publica |
| Contratos/render QA | Nuevos `Editor/RacingRainbowReviewBatch.cs`, `Editor/RacingCompetitiveDisplayContracts.cs` | Herramientas Editor; no entran en Player. Antes/después, fixtures claramente sintéticos, capturas después de frames reales |
| Laboratorios y evidencia | `Tools/CompetitiveQA/Program.cs`, nuevos contratos/benches/readbacks y scripts `Tools/RC` | Datos nuevos en workspace, fuentes de evidencia identificadas, sin activación productiva. CLI .NET comparte el núcleo existente de liga |

Antes de restaurar cualquier fuente, copiar la revisión actual a una carpeta nueva y comprobar que no contiene trabajo posterior del usuario. Restaurar únicamente los archivos del cambio elegido desde el snapshot, sin borrar archivos nuevos ni evidencias. Restaurar `RaceManager.cs` y `RacingProgressionService.cs` juntos; restaurar además el harness Editor desde `RacingCompetitiveDisplayContracts-before-reservation-contract.cs` si se elimina la nueva API. Recompilar y repetir los contratos afectados. Un rollback de fuente no requiere restaurar, migrar o borrar el save productivo.

La propuesta visual se desactiva simplemente omitiendo `--rainbow-preview`. La versión previa sigue en el mismo Player QA. No cambiar escenas, nodos, rutas, checkpoints, colliders o IDs para volver al aspecto anterior.

No reutilizar el constructor forense original con credenciales de producción cargadas automáticamente; el guard nuevo evita ese riesgo. Conservar los dos keystores y verificar sus hashes. El AAB marcado FORENSIC-NOT-FOR-INSTALL es únicamente para análisis de tamaño.

Los archivos productivos inventariados se comprueban por hash. No se dispone de una exportación inicial completa del registro nativo productivo: esa limitación impide afirmar una comparación binaria de todo PlayerPrefs. Los procesos QA usan identidad nativa BlacklineQA y sus snapshots propios. Los valores de fixtures de esa identidad no representan rating ganado por una persona.

Los riesgos abiertos de release son conducción élite, aprobación de diseño/HumanGate, Android físico, servicios y dominio app-ads.txt. Un contrato automatizado PASS no cierra ninguno de esos gates humanos o de dispositivo.
