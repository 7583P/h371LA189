# HumanGate Windows pendiente

Build aislada: `Builds/ReleaseGates-20261020-02-Reviewed/HillAdventurer-HumanGate.exe`.

Comparación visual reproducida: [nueve cámaras antes/después](rainbow-build/REVIEW.html). Son PNG originales, con cámara, objetivo, FOV y resolución iguales. Navegación y colisiones coinciden por hash. La revisión humana de diseño sigue pendiente.

Desde la raíz del proyecto, abrir la build para jugar manualmente:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/RC/Open-ReleaseHumanGate.ps1 -Player Builds/ReleaseGates-20261020-02-Reviewed/HillAdventurer-HumanGate.exe -EvidenceRoot Docs/ReleaseGates/20261020-02-HumanGate
```

La excepción de política se limita a ese proceso. El lanzador verifica `main` y la identidad BlacklineQA, respalda sus preferencias y guarda en una carpeta nueva. Producción conserva su namespace y su liga. ML queda Off y la propuesta Rainbow se activa únicamente para esta revisión QA.

Abrir el ejecutable no equivale a aprobarlo. Confirmar expresamente tras jugar:

1. Bandas y bordes legibles; sentido de las curvas, saltos y shortcuts; recuperación y llegada; ausencia de parpadeos/pink shaders. Revisar High, Low y diferentes condiciones de luz.
2. Controles, cámara, doce corredores, carrera completa, retry y regreso al hub.
3. Placements iniciales 0/3, clasificación estacional 0/1, títulos, MMR del recibo y resultados coherentes; ninguna doble aplicación al volver a la vista.
4. Buscar por nombre/ID a los once rivales persistentes; repetir tras relanzar. Los nombres numéricos, como 211, identifican al corredor existente; #211 busca la posición explícita.
5. Historial, navegación, botones y exportación Windows con destino elegido. No usar compras ni anuncios reales.
6. El usuario ya confirmó conservar el DAPEX azul/Whity encontrado. Verificar que no haya alteraciones accidentales del coche, ruedas o piloto; no sustituirlo por el Formula amarillo/cyan del TXT.

Android tiene un gate separado: dispositivo físico, controles, cámara, carrera, temperatura, FPS, ANR/crashes, GPGS, compras de prueba, AdMob autorizado y SAF. Los contratos host y bundletool no sustituyen esas pruebas.

AFTER 3000/100, otro reset, activación productiva de 100k, aceptación visual, firma, subida y publicación requieren decisiones explícitas independientes. No se solicitan credenciales ni cambios en Play Console para cerrar automáticamente un gate local.
