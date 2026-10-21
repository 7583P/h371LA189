# Carreras físicas y gate élite

**FAIL.** Matriz nueva completa: 69/72 llegadas, tres DNF, 16 recuperaciones. Los seis casos usan ML Off efectivo, High, escala 1 y física compartida; cero errores de ejecución. No se han cambiado IA, coches, boosts, cronómetros, standings ni la regla de cierre para alcanzar cifras.

| MMR | Seed | Histórico: rec. / gap (s) | Nuevo: llegadas | DNF | Rec. | Gap 1.º–12.º (s) |
| --- | --- | --- | --- | --- | --- | --- |
| 9700 | 4207 | 0 / 15.20 | 12/12 | 0 | 1 | 19.29 |
| 9700 | 4208 | 0 / 19.66 | 12/12 | 0 | 5 | 15.23 |
| 9700 | 4209 | 0 / 17.72 | 10/12 | 2 | 3 | no existe: DNF |
| 10000 | 4207 | 4 / 27.06 | 12/12 | 0 | 4 | 20.12 |
| 10000 | 4208 | 2 / 23.00 | 11/12 | 1 | 1 | no existe: DNF |
| 10000 | 4209 | 1 / 18.20 | 12/12 | 0 | 2 | 20.02 |

La tabla histórica procede de la matriz original Off (18–19 de octubre), no de otra tanda seleccionada por sus resultados. La nueva tanda no es un A/B causal de mejora de IA: no hubo una intervención de conducción. La misma seed no garantiza igualdad de la física, scheduling o orden de contactos.

Las 16 recuperaciones son Outside the course; 8 observan nodos 23–26 en la ventana final. También aparecen nodos 10, 36, 42–44, 61–63, 75–76, 104–105 y 186–187. Las ventanas completas de doce segundos y los contactos se conservan; cero incrementos de contacto en los últimos dos segundos no demuestran ausencia de contactos anteriores.

La instrumentación exportó 72 corredores y 1.728 filas de sectores, sin muestras ni contactos descartados. Son ocho divisiones de longitud de arco, distintas de los sectores/checkpoints de diseño; sus duraciones y flags se solapan. No se convierten en segundos causales perdidos.

## Cierre observado de los DNF

RaceManager.Update y HandleRacerFinished cierran la carrera a los 15 s de la llegada humana. Los tres corredores siguientes todavía no habían completado la llegada al cerrar. No se amplía esa regla ni se inventa su tiempo final. La distancia restante es sobre la spline en la última muestra, no una predicción de llegada.

| MMR / seed | Corredor | Cierre (s) | Última muestra (s) | Lap | Restan (m de spline) |
| --- | --- | --- | --- | --- | --- |
| 10000 / 4208 | SpeedDemon | 220.91 | 220.79 | 3 | 173.8 |
| 9700 / 4209 | ShadowDrift | 228.40 | 228.30 | 3 | 232.4 |
| 9700 / 4209 | StormSurge | 228.40 | 228.30 | 3 | 33.4 |

## Riesgo, causa pendiente y siguiente experimento

Severidad: bloquea el gate de calidad élite. La condición inmediata del DNF está comprobada; no está cerrada la causa de las salidas ni la contribución causal de tráfico, drift, rutas o items a los gaps. No se ha demostrado imposibilidad física del objetivo 3–8 s.

El siguiente cambio mínimo de conducción necesita un A/B aislado: anticipar margen lateral y tráfico antes de los nodos 23–26, observar liberación de drift y rumbo real, y comprobar estabilidad del rejoin de crest_cut. Repetir sin cambiar estadísticas, físicos, tiempos ni standings; revertir si empeora navegación, variedad o equidad. Estos candidatos no están activados.

La corrección nueva de capturas es exclusivamente QA. Los nombres heredados bridge-jump y tunnel eran etiquetas por intervalo de nodos, no prueba de una estructura ni de vuelo. La build de la matriz se conserva. La build posterior registró en 4301 vuelo en el nodo 172 y aterrizaje en el 175, sin recuperación intermedia, con JSON y PNG propios; no sustituye ni mejora esta matriz en silencio.
