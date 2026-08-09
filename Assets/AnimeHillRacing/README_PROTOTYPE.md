# Climb Anime — estructura actual

Proyecto Unity `6000.3.13f1`, URP, orientado a Android landscape. No añade dependencias externas, servicios, anuncios, compras, multiplayer ni floating origin.

## Escenas definitivas

El menú **Anime Hill Racing > Build All Game Scenes** reconstruye y valida, en este orden:

1. `MainMenu`
2. `Garage`
3. `PreStart`
4. `Adventure2_5D`
5. `Adventure3D`
6. `TimeTrial3D`

El flujo principal es `MainMenu → Garage/PreStart → gameplay → Results`. El builder también actualiza los prefabs 2D/3D, las definiciones Jeep/Formula, el catálogo, Countryside y Build Settings.

## Vehículos y personalización

El catálogo contiene exactamente `JEEP` (`car-01`) y `FORMULA` (`formula-01`). Sus prefabs separan física y presentación, pero ya no usan cuerpos, ruedas ni conductor provisionales.

Garage compone en vivo tres capas independientes:

- `Vehicle Skin`: sprite de carrocería por vehículo.
- `Wheels`: una opción se aplica simultáneamente a rueda delantera y trasera.
- `Driver / Character`: conductor global con pose definida por vehículo.

Jeep tiene `Jeep_Wheel_01` y `Jeep_Wheel_02` como alternativas desbloqueadas. Formula tiene `Formula_Wheel_01` como base extensible. `Formula_Reference.png` es solamente referencia artística y no participa en preview ni gameplay.

`Stickman Black` se construye con cabeza, torso, brazos, antebrazos/manos, muslos y piernas independientes. Jeep usa pose sentada; Formula usa una pose baja y compacta con piernas ocultas. Garage y gameplay consumen la misma definición de pose. En gameplay solo existe un trigger esférico en la cabeza y una reacción amortiguada, sin ragdoll completo.

Las selecciones se guardan con claves `AHR.Customize.*`: skin y ruedas pertenecen a cada vehículo; el conductor es global. Estas claves son independientes de monedas, tuning, desbloqueos y récords existentes.

## Countryside

`CountrysideEnvironment.asset` referencia directamente:

- `Assets/Art/Backgrounds/Countryside/Countryside_Sky.png`
- `Assets/Art/Backgrounds/Countryside/Countryside_Mountains.png`

El cielo no tiene física. Las montañas usan cinco `SpriteRenderer` precreados y reciclados, sin `Instantiate/Destroy` durante el seguimiento, con parallax X `0.20` e Y `0.04`. El cálculo centra los segmentos alrededor de la cámara cada frame y añade un pequeño solape para evitar huecos incluso a la velocidad máxima de Formula. Los parámetros viven en `EnvironmentDefinition` para que futuros mapas puedan suministrar sus propios assets y valores.

## Gameplay compartido

`VehicleDefinition`, `VehicleUpgradeState`, `ResolvedVehicleStats`, `EngineController`, `TransmissionController`, `FuelSystem`, `SaveManager`, economía, input, captura y resultados son compartidos. `VehicleController` mantiene Adventure 2.5D; `RoadVehicleController` sirve Adventure 3D y Time Trial.

Formula conserva Air Control fijo como propiedad base, fuera de tuning. En el aire, GAS produce nose-up y BRAKE nose-down mediante torque inmediato, sin propulsión horizontal artificial; Downforce funciona simultáneamente. Jeep usa el mismo esquema con menor autoridad.

## Validación

El builder comprueba assets reales, opciones distintas, partes del conductor, poses, límites de parallax, ausencia de `Formula_Reference` en la skin seleccionable, rendimiento objetivo, contrato de control aéreo y conexiones de las seis escenas. **Validate Game Scenes** repite la inspección de escenas y **Validate Countryside Geometry** muestrea el terreno procedural.

La última regeneración batch terminó sin errores de compilación y con las seis escenas conectadas. Para cambios futuros del builder, vuelve a ejecutar **Build All Game Scenes** fuera de Play Mode.
