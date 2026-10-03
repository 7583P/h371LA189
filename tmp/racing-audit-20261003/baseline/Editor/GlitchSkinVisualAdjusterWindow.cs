using System;
using System.Linq;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    public sealed class GlitchSkinVisualAdjusterWindow : EditorWindow
    {
        private enum DragMode
        {
            None,
            Move,
            Resize
        }

        private enum EditTarget
        {
            Body,
            RearWheel,
            FrontWheel
        }

        private VehicleDefinition[] vehicles = Array.Empty<VehicleDefinition>();
        private int vehicleIndex;
        private int phaseIndex;
        private Vector2 workingSize;
        private Vector2 workingOffset;
        private bool workingOverride;
        private bool originalOverride;
        private Vector2 originalSize;
        private Vector2 originalOffset;
        private bool workingWheelOverride;
        private bool originalWheelOverride;
        private float workingRearWheelScale = 1f;
        private float workingFrontWheelScale = 1f;
        private float originalRearWheelScale = 1f;
        private float originalFrontWheelScale = 1f;
        private Vector2 workingRearWheelOffset;
        private Vector2 workingFrontWheelOffset;
        private Vector2 originalRearWheelOffset;
        private Vector2 originalFrontWheelOffset;
        private Vector2 inspectorScroll;
        private float previewZoom = 1f;
        private bool showDefaultReference = true;
        private bool showPhaseWheels = true;
        private bool showDefaultWheelReference = true;
        private bool preserveAspect = true;
        private DragMode dragMode;
        private Vector2 resizeSigns;
        private EditTarget selectedTarget;

        [MenuItem("Anime Hill Racing/Vehicles/Glitch Skin Visual Adjuster")]
        public static void Open()
        {
            OpenForVehicle(Selection.activeObject as VehicleDefinition);
        }

        public static void OpenForVehicle(VehicleDefinition preferredVehicle)
        {
            GlitchSkinVisualAdjusterWindow window = GetWindow<GlitchSkinVisualAdjusterWindow>();
            window.titleContent = new GUIContent("Glitch Skin Adjuster");
            window.minSize = new Vector2(900f, 560f);
            window.ReloadAssets(preferredVehicle);
            window.Show();
        }

        private VehicleDefinition Vehicle => vehicleIndex >= 0 && vehicleIndex < vehicles.Length
            ? vehicles[vehicleIndex] : null;

        private VehicleDefinition.CustomizationOption[] GlitchSkins => Vehicle?.skins == null
            ? Array.Empty<VehicleDefinition.CustomizationOption>()
            : Vehicle.skins.Where(GlitchCrystalTideEffect.IsGlitchSkin).ToArray();

        private VehicleDefinition.CustomizationOption Skin
        {
            get
            {
                VehicleDefinition.CustomizationOption[] skins = GlitchSkins;
                return phaseIndex >= 0 && phaseIndex < skins.Length ? skins[phaseIndex] : null;
            }
        }

        private VehicleDefinition.CustomizationOption PhaseWheel
        {
            get
            {
                if (Vehicle?.wheels == null) return null;
                int phaseDistance = PhaseDistance(Skin?.optionId);
                VehicleDefinition.CustomizationOption exact = Vehicle.wheels.FirstOrDefault(option =>
                    GlitchCrystalTideEffect.IsGlitchWheel(option) &&
                    PhaseDistance(option.optionId) == phaseDistance);
                return exact ?? Vehicle.wheels.FirstOrDefault(GlitchCrystalTideEffect.IsGlitchWheel);
            }
        }

        private Vector2 AutomaticSize
        {
            get
            {
                VehicleDefinition vehicle = Vehicle;
                VehicleDefinition.CustomizationOption reference =
                    vehicle?.skins != null && vehicle.skins.Length > 0 ? vehicle.skins[0] : null;
                if (vehicle == null || reference?.sprite == null) return Vector2.one;
                float scale = VehicleDefinition.SpriteWidthScale(reference.sprite, vehicle.visualWidth);
                return (Vector2)reference.sprite.bounds.size * scale;
            }
        }

        private bool HasUnsavedChanges
        {
            get
            {
                if (Skin == null) return false;
                bool bodyChanged = workingOverride != originalOverride || workingOverride &&
                    ((workingSize - originalSize).sqrMagnitude > .0000001f ||
                     (workingOffset - originalOffset).sqrMagnitude > .0000001f);
                bool wheelChanged = workingWheelOverride != originalWheelOverride ||
                    workingWheelOverride &&
                    (Mathf.Abs(workingRearWheelScale - originalRearWheelScale) > .00001f ||
                     Mathf.Abs(workingFrontWheelScale - originalFrontWheelScale) > .00001f ||
                     (workingRearWheelOffset - originalRearWheelOffset).sqrMagnitude > .0000001f ||
                     (workingFrontWheelOffset - originalFrontWheelOffset).sqrMagnitude > .0000001f);
                return bodyChanged || wheelChanged;
            }
        }

        private void OnEnable()
        {
            ReloadAssets(Selection.activeObject as VehicleDefinition);
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable() => EditorApplication.projectChanged -= OnProjectChanged;

        private void OnProjectChanged()
        {
            if (!HasUnsavedChanges) ReloadAssets(Vehicle);
            Repaint();
        }

        private void ReloadAssets(VehicleDefinition preferredVehicle)
        {
            VehicleDefinition previous = preferredVehicle != null ? preferredVehicle : Vehicle;
            GameCatalog catalog = AssetDatabase.FindAssets("t:GameCatalog")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameCatalog>)
                .FirstOrDefault(item => item != null);
            VehicleDefinition[] catalogVehicles = catalog?.vehicles != null
                ? catalog.vehicles.Where(item => item != null).ToArray()
                : FindAssets<VehicleDefinition>();
            vehicles = catalogVehicles.Where(HasGlitchSkins).ToArray();
            int preferredIndex = Array.IndexOf(vehicles, previous);
            vehicleIndex = Mathf.Clamp(preferredIndex >= 0 ? preferredIndex : vehicleIndex,
                0, Mathf.Max(0, vehicles.Length - 1));
            phaseIndex = Mathf.Clamp(phaseIndex, 0, Mathf.Max(0, GlitchSkins.Length - 1));
            LoadWorkingValues();
        }

        private static T[] FindAssets<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(item => item != null)
                .OrderBy(item => item.name)
                .ToArray();

        private static bool HasGlitchSkins(VehicleDefinition vehicle) =>
            vehicle?.skins != null && vehicle.skins.Any(GlitchCrystalTideEffect.IsGlitchSkin);

        private void LoadWorkingValues()
        {
            VehicleDefinition.CustomizationOption skin = Skin;
            VehicleDefinition.CustomizationOption wheel = PhaseWheel;
            dragMode = DragMode.None;
            if (skin == null)
            {
                workingOverride = false;
                workingSize = Vector2.one;
                workingOffset = Vector2.zero;
                LoadWheelValues(null);
                return;
            }

            Vector2 automatic = AutomaticSize;
            originalOverride = skin.overrideGlitchTransform;
            originalSize = ResolveSize(skin.glitchManualSize, automatic);
            originalOffset = skin.overrideGlitchTransform ? skin.glitchManualOffset : Vector2.zero;
            workingOverride = originalOverride;
            workingSize = originalOverride ? originalSize : automatic;
            workingOffset = originalOffset;
            LoadWheelValues(wheel);
        }

        private void LoadWheelValues(VehicleDefinition.CustomizationOption wheel)
        {
            originalWheelOverride = wheel != null && wheel.overrideGlitchWheelTransform;
            originalRearWheelScale = ValidWheelMultiplier(wheel?.glitchRearWheelScale ?? 1f);
            originalFrontWheelScale = ValidWheelMultiplier(wheel?.glitchFrontWheelScale ?? 1f);
            originalRearWheelOffset = originalWheelOverride
                ? ValidVector(wheel.glitchRearWheelOffset) : Vector2.zero;
            originalFrontWheelOffset = originalWheelOverride
                ? ValidVector(wheel.glitchFrontWheelOffset) : Vector2.zero;
            workingWheelOverride = originalWheelOverride;
            workingRearWheelScale = originalWheelOverride ? originalRearWheelScale : 1f;
            workingFrontWheelScale = originalWheelOverride ? originalFrontWheelScale : 1f;
            workingRearWheelOffset = originalRearWheelOffset;
            workingFrontWheelOffset = originalFrontWheelOffset;
        }

        private void OnGUI()
        {
            if (vehicles.Length == 0)
            {
                EditorGUILayout.HelpBox("No hay vehículos con skins Glitch Phase.", MessageType.Warning);
                return;
            }

            DrawToolbar();
            if (Vehicle == null || Skin?.sprite == null) return;

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true),
                           GUILayout.ExpandHeight(true)))
                    DrawPreview();
                using (new EditorGUILayout.VerticalScope(
                           GUILayout.Width(Mathf.Clamp(position.width * .32f, 300f, 400f))))
                    DrawInspector();
            }
            DrawActionBar();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                string[] vehicleNames = vehicles.Select(item => item.displayName).ToArray();
                int nextVehicle = EditorGUILayout.Popup(vehicleIndex, vehicleNames,
                    EditorStyles.toolbarPopup, GUILayout.MinWidth(160f));
                if (nextVehicle != vehicleIndex && ConfirmDiscard())
                {
                    vehicleIndex = nextVehicle;
                    phaseIndex = 0;
                    LoadWorkingValues();
                }

                VehicleDefinition.CustomizationOption[] skins = GlitchSkins;
                string[] phaseNames = skins.Select(PhaseTitle).ToArray();
                int nextPhase = EditorGUILayout.Popup(
                    Mathf.Clamp(phaseIndex, 0, Mathf.Max(0, phaseNames.Length - 1)), phaseNames,
                    EditorStyles.toolbarPopup, GUILayout.MinWidth(210f));
                if (nextPhase != phaseIndex && ConfirmDiscard())
                {
                    phaseIndex = nextPhase;
                    LoadWorkingValues();
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label(HasUnsavedChanges ? "CAMBIOS TEMPORALES" : "GUARDADO",
                    HasUnsavedChanges ? EditorStyles.miniBoldLabel : EditorStyles.miniLabel);
            }
        }

        private void DrawInspector()
        {
            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
            EditorGUILayout.LabelField("Ajuste visual Glitch Phase", EditorStyles.boldLabel);
            string[] targetNames = { "Carroceria", "Llanta trasera", "Llanta delantera" };
            selectedTarget = (EditTarget)EditorGUILayout.Popup("Elemento", (int)selectedTarget,
                targetNames);
            EditorGUILayout.HelpBox(
                "Arrastra la skin para moverla. Arrastra cualquiera de los cuatro cuadrados de las esquinas " +
                "para cambiar sus dimensiones. La silueta transparente es la skin default.", MessageType.Info);

            DrawThumbnail(Skin.sprite);
            EditorGUILayout.LabelField(PhaseTitle(Skin), EditorStyles.boldLabel);
            VehicleDefinition.CustomizationOption phaseWheel = PhaseWheel;
            if (phaseWheel?.sprite != null)
            {
                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField("Llantas de esta fase", PhaseTitle(phaseWheel));
                DrawWheelThumbnail(phaseWheel.sprite);
            }
            EditorGUILayout.Space(6f);

            if (selectedTarget != EditTarget.Body)
            {
                DrawWheelInspector(selectedTarget == EditTarget.FrontWheel);
                DrawViewInspector();
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUI.BeginChangeCheck();
            bool nextOverride = EditorGUILayout.Toggle("Ajuste manual", workingOverride);
            if (nextOverride != workingOverride)
            {
                workingOverride = nextOverride;
                if (!workingOverride)
                {
                    workingSize = AutomaticSize;
                    workingOffset = Vector2.zero;
                }
            }

            workingSize = EditorGUILayout.Vector2Field("Tamaño final X / Y", workingSize);
            workingSize.x = Mathf.Max(.02f, Mathf.Abs(workingSize.x));
            workingSize.y = Mathf.Max(.02f, Mathf.Abs(workingSize.y));
            workingOffset = EditorGUILayout.Vector2Field("Desplazamiento X / Y", workingOffset);
            preserveAspect = EditorGUILayout.Toggle("Conservar proporción", preserveAspect);
            if (EditorGUI.EndChangeCheck())
            {
                workingOverride = true;
                Repaint();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-10%")) ScaleWorking(.9f);
                if (GUILayout.Button("+10%")) ScaleWorking(1.1f);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("←")) Nudge(new Vector2(-.01f, 0f));
                if (GUILayout.Button("→")) Nudge(new Vector2(.01f, 0f));
                if (GUILayout.Button("↓")) Nudge(new Vector2(0f, -.01f));
                if (GUILayout.Button("↑")) Nudge(new Vector2(0f, .01f));
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Referencia automática", FormatVector(AutomaticSize));
            EditorGUILayout.LabelField("Resultado", FormatVector(workingSize));
            EditorGUILayout.LabelField("Posición final", FormatVector(Vehicle.bodyVisualOffset + workingOffset));

            if (GUILayout.Button("Encajar exactamente sobre la default"))
            {
                workingOverride = true;
                workingSize = AutomaticSize;
                workingOffset = Vector2.zero;
            }
            if (GUILayout.Button("Desactivar ajuste manual"))
            {
                workingOverride = false;
                workingSize = AutomaticSize;
                workingOffset = Vector2.zero;
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Vista", EditorStyles.boldLabel);
            previewZoom = EditorGUILayout.Slider("Zoom", previewZoom, .55f, 2.2f);
            showDefaultReference = EditorGUILayout.Toggle("Mostrar skin default", showDefaultReference);
            showPhaseWheels = EditorGUILayout.Toggle("Mostrar llantas Glitch", showPhaseWheels);
            showDefaultWheelReference = EditorGUILayout.Toggle(
                "Referencia llantas default", showDefaultWheelReference);
            EditorGUILayout.HelpBox(
                "Las llantas Glitch se eligen automáticamente usando la misma distancia de la skin. " +
                "Los círculos celestes indican los ejes reales y la línea horizontal marca el suelo. " +
                "Así puedes ajustar el cuerpo exactamente alrededor de las llantas que se usarán en el juego.",
                MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private void DrawWheelInspector(bool front)
        {
            VehicleDefinition.CustomizationOption wheel = PhaseWheel;
            EditorGUILayout.HelpBox(
                "Arrastra esta llanta en la vista para moverla. Usa sus cuadrados celestes para " +
                "escalarla. El eje fisico y la suspension no se modifican.", MessageType.Info);
            if (wheel?.sprite == null)
            {
                EditorGUILayout.HelpBox("Esta fase no tiene una llanta Glitch asociada.",
                    MessageType.Warning);
                return;
            }

            bool nextOverride = EditorGUILayout.Toggle("Ajuste manual llantas", workingWheelOverride);
            if (nextOverride != workingWheelOverride)
            {
                workingWheelOverride = nextOverride;
                if (!workingWheelOverride) ResetWorkingWheels();
            }

            EditorGUI.BeginChangeCheck();
            float multiplier = front ? workingFrontWheelScale : workingRearWheelScale;
            Vector2 offset = front ? workingFrontWheelOffset : workingRearWheelOffset;
            multiplier = Mathf.Clamp(Mathf.Abs(
                EditorGUILayout.FloatField("Escala sobre automatico", multiplier)), .05f, 10f);
            offset = EditorGUILayout.Vector2Field("Desplazamiento X / Y", offset);
            if (front)
            {
                workingFrontWheelScale = multiplier;
                workingFrontWheelOffset = offset;
            }
            else
            {
                workingRearWheelScale = multiplier;
                workingRearWheelOffset = offset;
            }
            if (EditorGUI.EndChangeCheck()) workingWheelOverride = true;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-10%")) ScaleWorking(.9f);
                if (GUILayout.Button("+10%")) ScaleWorking(1.1f);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("←")) Nudge(new Vector2(-.01f, 0f));
                if (GUILayout.Button("→")) Nudge(new Vector2(.01f, 0f));
                if (GUILayout.Button("↓")) Nudge(new Vector2(0f, -.01f));
                if (GUILayout.Button("↑")) Nudge(new Vector2(0f, .01f));
            }

            float automaticScale = AutomaticWheelScale(front);
            float finalScale = automaticScale * multiplier;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Escala automatica", automaticScale.ToString("0.###"));
            EditorGUILayout.LabelField("Tamano final",
                FormatVector((Vector2)wheel.sprite.bounds.size * finalScale));
            EditorGUILayout.LabelField("Posicion visual", FormatVector(
                WheelAuthoredPosition(front) + offset));

            if (GUILayout.Button("Restaurar esta llanta a automatico"))
            {
                workingWheelOverride = true;
                if (front)
                {
                    workingFrontWheelScale = 1f;
                    workingFrontWheelOffset = Vector2.zero;
                }
                else
                {
                    workingRearWheelScale = 1f;
                    workingRearWheelOffset = Vector2.zero;
                }
            }
            if (GUILayout.Button("Desactivar ajuste manual de llantas"))
            {
                workingWheelOverride = false;
                ResetWorkingWheels();
            }
        }

        private void DrawViewInspector()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Vista", EditorStyles.boldLabel);
            previewZoom = EditorGUILayout.Slider("Zoom", previewZoom, .55f, 2.2f);
            showDefaultReference = EditorGUILayout.Toggle("Mostrar skin default", showDefaultReference);
            showPhaseWheels = EditorGUILayout.Toggle("Mostrar llantas Glitch", showPhaseWheels);
            showDefaultWheelReference = EditorGUILayout.Toggle(
                "Referencia llantas default", showDefaultWheelReference);
            EditorGUILayout.HelpBox(
                "Haz clic directamente sobre la carroceria o cualquiera de las dos llantas para " +
                "seleccionarla. Los circulos celestes marcan los ejes fisicos originales.",
                MessageType.None);
        }

        private void DrawPreview()
        {
            Rect canvas = GUILayoutUtility.GetRect(460f, 10000f, 410f, 10000f,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(canvas, new Color(.045f, .055f, .075f));
            DrawGrid(canvas);

            EventType eventType = Event.current.type;
            if (eventType != EventType.Repaint && eventType != EventType.MouseDown &&
                eventType != EventType.MouseDrag && eventType != EventType.MouseUp) return;

            float pixelsPerUnit = Mathf.Min(
                (canvas.width - 70f) / Mathf.Max(5.5f, Vehicle.visualWidth * 1.35f),
                (canvas.height - 70f) / 4.2f) * previewZoom;
            Vector2 origin = new Vector2(canvas.center.x, canvas.center.y + canvas.height * .15f);

            DrawGround(origin, pixelsPerUnit, canvas);
            if (showDefaultReference) DrawDefaultBody(origin, pixelsPerUnit);
            DrawWorkingBody(origin, pixelsPerUnit);
            DrawWheels(origin, pixelsPerUnit);
            DrawHandles(origin, pixelsPerUnit, canvas);
            HandleDragging(origin, pixelsPerUnit, canvas);

            GUI.Label(new Rect(canvas.x + 10f, canvas.y + 8f, canvas.width - 20f, 20f),
                $"{Vehicle.displayName}  ·  {PhaseTitle(Skin)}  ·  ARRASTRA PARA AJUSTAR",
                EditorStyles.whiteMiniLabel);
        }

        private static void DrawGrid(Rect canvas)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color minor = new Color(.12f, .145f, .19f);
            Color major = new Color(.19f, .23f, .3f);
            int column = 0;
            for (float x = canvas.x; x < canvas.xMax; x += 24f, column++)
                EditorGUI.DrawRect(new Rect(x, canvas.y, 1f, canvas.height),
                    column % 5 == 0 ? major : minor);
            int row = 0;
            for (float y = canvas.y; y < canvas.yMax; y += 24f, row++)
                EditorGUI.DrawRect(new Rect(canvas.x, y, canvas.width, 1f),
                    row % 5 == 0 ? major : minor);
        }

        private void DrawGround(Vector2 origin, float pixelsPerUnit, Rect canvas)
        {
            if (Event.current.type != EventType.Repaint) return;
            float rearDiameter = Vehicle.RearWheelDiameter;
            float frontDiameter = Vehicle.FrontWheelDiameter;
            float groundY = Mathf.Min(Vehicle.rearWheelVisualPosition.y - rearDiameter * .5f,
                Vehicle.frontWheelVisualPosition.y - frontDiameter * .5f);
            float screenY = WorldToScreen(new Vector2(0f, groundY), origin, pixelsPerUnit).y;
            EditorGUI.DrawRect(new Rect(canvas.x, screenY, canvas.width, 2f),
                new Color(.25f, .9f, .55f, .7f));
        }

        private void DrawDefaultBody(Vector2 origin, float pixelsPerUnit)
        {
            VehicleDefinition.CustomizationOption reference = Vehicle.skins != null && Vehicle.skins.Length > 0
                ? Vehicle.skins[0] : null;
            if (reference?.sprite == null) return;
            Vector3 scale = Vehicle.BodyCustomizationScale(reference);
            DrawSprite(reference.sprite, Vehicle.BodyCustomizationOffset(reference),
                new Vector2(scale.x, scale.y), Vehicle.flipBodyX, origin, pixelsPerUnit,
                new Color(.45f, .85f, 1f, .23f));
        }

        private void DrawWorkingBody(Vector2 origin, float pixelsPerUnit)
        {
            Sprite sprite = Skin.sprite;
            Vector2 sourceSize = sprite.bounds.size;
            Vector2 scale = new Vector2(workingSize.x / Mathf.Max(.001f, sourceSize.x),
                workingSize.y / Mathf.Max(.001f, sourceSize.y));
            DrawSprite(sprite, Vehicle.bodyVisualOffset + workingOffset, scale, Vehicle.flipBodyX,
                origin, pixelsPerUnit, Color.white);
        }

        private void DrawWheels(Vector2 origin, float pixelsPerUnit)
        {
            VehicleDefinition.CustomizationOption defaultWheel =
                Vehicle.wheels != null && Vehicle.wheels.Length > 0
                ? Vehicle.wheels[0] : null;
            VehicleDefinition.CustomizationOption phaseWheel = PhaseWheel;

            if (showDefaultWheelReference && defaultWheel?.sprite != null && defaultWheel != phaseWheel)
                DrawWheelPair(defaultWheel, origin, pixelsPerUnit, new Color(.45f, .85f, 1f, .25f),
                    false);

            if (showPhaseWheels)
            {
                VehicleDefinition.CustomizationOption visibleWheel = phaseWheel ?? defaultWheel;
                if (visibleWheel?.sprite != null)
                    DrawWheelPair(visibleWheel, origin, pixelsPerUnit, Color.white,
                        visibleWheel == phaseWheel);
            }

            if (Event.current.type != EventType.Repaint) return;
            DrawWheelCenter(Vehicle.rearWheelVisualPosition, origin, pixelsPerUnit);
            DrawWheelCenter(Vehicle.frontWheelVisualPosition, origin, pixelsPerUnit);
        }

        private void DrawWheelPair(VehicleDefinition.CustomizationOption wheel, Vector2 origin,
            float pixelsPerUnit, Color tint, bool useWorkingValues)
        {
            float rearScale = useWorkingValues
                ? AutomaticWheelScale(false) * (workingWheelOverride ? workingRearWheelScale : 1f)
                : Vehicle.WheelCustomizationScale(wheel, Vehicle.RearWheelDiameter, false);
            float frontScale = useWorkingValues
                ? AutomaticWheelScale(true) * (workingWheelOverride ? workingFrontWheelScale : 1f)
                : Vehicle.WheelCustomizationScale(wheel, Vehicle.FrontWheelDiameter, true);
            Vector2 rearPosition = Vehicle.rearWheelVisualPosition + (useWorkingValues &&
                workingWheelOverride ? workingRearWheelOffset : Vector2.zero);
            Vector2 frontPosition = Vehicle.frontWheelVisualPosition + (useWorkingValues &&
                workingWheelOverride ? workingFrontWheelOffset : Vector2.zero);
            DrawSprite(wheel.sprite, rearPosition, Vector2.one * rearScale, false,
                origin, pixelsPerUnit, tint);
            DrawSprite(wheel.sprite, frontPosition, Vector2.one * frontScale, false,
                origin, pixelsPerUnit, tint);
        }

        private static void DrawWheelCenter(Vector2 world, Vector2 origin, float pixelsPerUnit)
        {
            Vector2 screen = WorldToScreen(world, origin, pixelsPerUnit);
            Handles.BeginGUI();
            Handles.color = new Color(.2f, .9f, 1f, .9f);
            Handles.DrawWireDisc(screen, Vector3.forward, 5f);
            Handles.EndGUI();
        }

        private void DrawHandles(Vector2 origin, float pixelsPerUnit, Rect canvas)
        {
            if (Event.current.type != EventType.Repaint) return;
            DrawWheelOutline(false, origin, pixelsPerUnit,
                selectedTarget == EditTarget.RearWheel);
            DrawWheelOutline(true, origin, pixelsPerUnit,
                selectedTarget == EditTarget.FrontWheel);
            if (selectedTarget != EditTarget.Body) return;

            Rect bodyRect = BodyScreenRect(origin, pixelsPerUnit);
            Handles.BeginGUI();
            Handles.color = new Color(1f, .78f, .12f, .95f);
            Handles.DrawAAPolyLine(2f,
                new Vector3(bodyRect.xMin, bodyRect.yMin), new Vector3(bodyRect.xMax, bodyRect.yMin),
                new Vector3(bodyRect.xMax, bodyRect.yMax), new Vector3(bodyRect.xMin, bodyRect.yMax),
                new Vector3(bodyRect.xMin, bodyRect.yMin));
            Handles.EndGUI();

            Vector2 center = bodyRect.center;
            Handles.BeginGUI();
            Handles.color = new Color(1f, .72f, .08f);
            Handles.DrawSolidDisc(center, Vector3.forward, 6f);
            Handles.EndGUI();

            Vector2[] corners = BodyCorners(bodyRect);
            for (int i = 0; i < corners.Length; i++)
            {
                Rect handle = new Rect(corners[i].x - 6f, corners[i].y - 6f, 12f, 12f);
                EditorGUI.DrawRect(handle, new Color(1f, .78f, .12f));
                EditorGUI.DrawRect(new Rect(handle.x + 3f, handle.y + 3f, 6f, 6f),
                    new Color(.12f, .12f, .12f));
            }
        }

        private void DrawWheelOutline(bool front, Vector2 origin, float pixelsPerUnit, bool selected)
        {
            Rect rect = WheelScreenRect(front, origin, pixelsPerUnit);
            if (rect.width <= 0f || rect.height <= 0f) return;
            Color color = selected ? new Color(.2f, 1f, 1f, 1f) : new Color(.2f, .8f, 1f, .5f);
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawAAPolyLine(selected ? 2f : 1f,
                new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin),
                new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMin, rect.yMax),
                new Vector3(rect.xMin, rect.yMin));
            if (selected) Handles.DrawSolidDisc(rect.center, Vector3.forward, 5f);
            Handles.EndGUI();
            if (!selected) return;

            Vector2[] corners = BodyCorners(rect);
            for (int i = 0; i < corners.Length; i++)
            {
                Rect handle = new Rect(corners[i].x - 6f, corners[i].y - 6f, 12f, 12f);
                EditorGUI.DrawRect(handle, color);
                EditorGUI.DrawRect(new Rect(handle.x + 3f, handle.y + 3f, 6f, 6f),
                    new Color(.08f, .12f, .15f));
            }
        }

        private void HandleDragging(Vector2 origin, float pixelsPerUnit, Rect canvas)
        {
            Event evt = Event.current;
            if (evt.button != 0) return;
            Rect bodyRect = BodyScreenRect(origin, pixelsPerUnit);
            Rect rearRect = WheelScreenRect(false, origin, pixelsPerUnit);
            Rect frontRect = WheelScreenRect(true, origin, pixelsPerUnit);

            if (evt.type == EventType.MouseDown && canvas.Contains(evt.mousePosition))
            {
                if (TryBeginWheelDrag(false, rearRect, evt) ||
                    TryBeginWheelDrag(true, frontRect, evt)) return;

                Vector2[] corners = BodyCorners(bodyRect);
                for (int i = 0; i < corners.Length; i++)
                {
                    if (Vector2.Distance(evt.mousePosition, corners[i]) > 14f) continue;
                    selectedTarget = EditTarget.Body;
                    dragMode = DragMode.Resize;
                    resizeSigns = CornerSigns(i);
                    workingOverride = true;
                    evt.Use();
                    Repaint();
                    return;
                }

                if (bodyRect.Contains(evt.mousePosition) ||
                    Vector2.Distance(evt.mousePosition, bodyRect.center) <= 16f)
                {
                    selectedTarget = EditTarget.Body;
                    dragMode = DragMode.Move;
                    workingOverride = true;
                    evt.Use();
                    Repaint();
                }
            }
            else if (evt.type == EventType.MouseDrag && dragMode != DragMode.None)
            {
                Vector2 worldDelta = new Vector2(evt.delta.x, -evt.delta.y) /
                                     Mathf.Max(1f, pixelsPerUnit);
                if (dragMode == DragMode.Move)
                    MoveSelected(worldDelta);
                else
                {
                    if (selectedTarget == EditTarget.Body) ResizeFromCorner(worldDelta);
                    else ResizeWheelFromCorner(selectedTarget == EditTarget.FrontWheel, worldDelta);
                }
                evt.Use();
                Repaint();
            }
            else if (evt.type == EventType.MouseUp && dragMode != DragMode.None)
            {
                dragMode = DragMode.None;
                evt.Use();
            }
        }

        private bool TryBeginWheelDrag(bool front, Rect rect, Event evt)
        {
            if (rect.width <= 0f || rect.height <= 0f) return false;
            Vector2[] corners = BodyCorners(rect);
            for (int i = 0; i < corners.Length; i++)
            {
                if (Vector2.Distance(evt.mousePosition, corners[i]) > 14f) continue;
                selectedTarget = front ? EditTarget.FrontWheel : EditTarget.RearWheel;
                dragMode = DragMode.Resize;
                resizeSigns = CornerSigns(i);
                evt.Use();
                Repaint();
                return true;
            }

            if (!PointInEllipse(evt.mousePosition, rect)) return false;
            selectedTarget = front ? EditTarget.FrontWheel : EditTarget.RearWheel;
            dragMode = DragMode.Move;
            evt.Use();
            Repaint();
            return true;
        }

        private void MoveSelected(Vector2 worldDelta)
        {
            if (selectedTarget == EditTarget.Body)
            {
                workingOverride = true;
                workingOffset += worldDelta;
            }
            else if (selectedTarget == EditTarget.FrontWheel)
            {
                workingWheelOverride = true;
                workingFrontWheelOffset += worldDelta;
            }
            else
            {
                workingWheelOverride = true;
                workingRearWheelOffset += worldDelta;
            }
        }

        private void ResizeWheelFromCorner(bool front, Vector2 worldDelta)
        {
            Sprite sprite = PhaseWheel?.sprite;
            if (sprite == null) return;
            float automaticScale = AutomaticWheelScale(front);
            float oldMultiplier = front ? workingFrontWheelScale : workingRearWheelScale;
            Vector2 oldOffset = front ? workingFrontWheelOffset : workingRearWheelOffset;
            Vector2 oldSize = (Vector2)sprite.bounds.size * (automaticScale * oldMultiplier);
            float relativeX = resizeSigns.x * worldDelta.x / Mathf.Max(.02f, oldSize.x);
            float relativeY = resizeSigns.y * worldDelta.y / Mathf.Max(.02f, oldSize.y);
            float relative = Mathf.Abs(relativeX) >= Mathf.Abs(relativeY) ? relativeX : relativeY;
            float newMultiplier = Mathf.Clamp(oldMultiplier * Mathf.Max(.05f, 1f + relative), .05f, 10f);
            float oldScale = automaticScale * oldMultiplier;
            float newScale = automaticScale * newMultiplier;
            Vector2 oldPivot = WheelAuthoredPosition(front) + oldOffset;
            Vector2 oldCenter = oldPivot + (Vector2)sprite.bounds.center * oldScale;
            Vector2 opposite = oldCenter - Vector2.Scale(resizeSigns, oldSize) * .5f;
            Vector2 newSize = (Vector2)sprite.bounds.size * newScale;
            Vector2 newCenter = opposite + Vector2.Scale(resizeSigns, newSize) * .5f;
            Vector2 newPivot = newCenter - (Vector2)sprite.bounds.center * newScale;
            Vector2 newOffset = newPivot - WheelAuthoredPosition(front);
            if (front)
            {
                workingFrontWheelScale = newMultiplier;
                workingFrontWheelOffset = newOffset;
            }
            else
            {
                workingRearWheelScale = newMultiplier;
                workingRearWheelOffset = newOffset;
            }
            workingWheelOverride = true;
        }

        private void ResizeFromCorner(Vector2 worldDelta)
        {
            Vector2 oldSize = workingSize;
            Vector2 newSize;
            if (preserveAspect)
            {
                float relativeX = resizeSigns.x * worldDelta.x / Mathf.Max(.02f, oldSize.x);
                float relativeY = resizeSigns.y * worldDelta.y / Mathf.Max(.02f, oldSize.y);
                float relative = Mathf.Abs(relativeX) >= Mathf.Abs(relativeY) ? relativeX : relativeY;
                newSize = oldSize * Mathf.Max(.05f, 1f + relative);
            }
            else
            {
                newSize = new Vector2(
                    Mathf.Max(.02f, oldSize.x + resizeSigns.x * worldDelta.x),
                    Mathf.Max(.02f, oldSize.y + resizeSigns.y * worldDelta.y));
            }

            Sprite sprite = Skin.sprite;
            Vector2 oldScale = new Vector2(oldSize.x / Mathf.Max(.001f, sprite.bounds.size.x),
                oldSize.y / Mathf.Max(.001f, sprite.bounds.size.y));
            Vector2 newScale = new Vector2(newSize.x / Mathf.Max(.001f, sprite.bounds.size.x),
                newSize.y / Mathf.Max(.001f, sprite.bounds.size.y));
            Vector2 oldPivot = Vehicle.bodyVisualOffset + workingOffset;
            Vector2 oldCenter = oldPivot + Vector2.Scale((Vector2)sprite.bounds.center, oldScale);
            Vector2 opposite = oldCenter - Vector2.Scale(resizeSigns, oldSize) * .5f;
            Vector2 newCenter = opposite + Vector2.Scale(resizeSigns, newSize) * .5f;
            Vector2 newPivot = newCenter - Vector2.Scale((Vector2)sprite.bounds.center, newScale);
            workingSize = newSize;
            workingOffset = newPivot - Vehicle.bodyVisualOffset;
            workingOverride = true;
        }

        private Rect BodyScreenRect(Vector2 origin, float pixelsPerUnit)
        {
            Sprite sprite = Skin.sprite;
            Vector2 scale = new Vector2(workingSize.x / Mathf.Max(.001f, sprite.bounds.size.x),
                workingSize.y / Mathf.Max(.001f, sprite.bounds.size.y));
            Vector2 centerWorld = Vehicle.bodyVisualOffset + workingOffset +
                                  Vector2.Scale((Vector2)sprite.bounds.center, scale);
            Vector2 center = WorldToScreen(centerWorld, origin, pixelsPerUnit);
            Vector2 screenSize = workingSize * pixelsPerUnit;
            return new Rect(center.x - screenSize.x * .5f, center.y - screenSize.y * .5f,
                screenSize.x, screenSize.y);
        }

        private Rect WheelScreenRect(bool front, Vector2 origin, float pixelsPerUnit)
        {
            Sprite sprite = PhaseWheel?.sprite;
            if (sprite == null) return Rect.zero;
            float multiplier = front ? workingFrontWheelScale : workingRearWheelScale;
            Vector2 offset = front ? workingFrontWheelOffset : workingRearWheelOffset;
            float scale = AutomaticWheelScale(front) *
                          (workingWheelOverride ? multiplier : 1f);
            Vector2 pivot = WheelAuthoredPosition(front) +
                            (workingWheelOverride ? offset : Vector2.zero);
            Vector2 centerWorld = pivot + (Vector2)sprite.bounds.center * scale;
            Vector2 center = WorldToScreen(centerWorld, origin, pixelsPerUnit);
            Vector2 screenSize = (Vector2)sprite.bounds.size * scale * pixelsPerUnit;
            return new Rect(center.x - screenSize.x * .5f, center.y - screenSize.y * .5f,
                screenSize.x, screenSize.y);
        }

        private float AutomaticWheelScale(bool front)
        {
            VehicleDefinition.CustomizationOption wheel = PhaseWheel;
            VehicleDefinition.CustomizationOption reference =
                Vehicle?.wheels != null && Vehicle.wheels.Length > 0 ? Vehicle.wheels[0] : null;
            if (wheel?.sprite == null || reference?.sprite == null) return 1f;
            float diameter = front ? Vehicle.FrontWheelDiameter : Vehicle.RearWheelDiameter;
            float referenceScale = VehicleDefinition.WheelVisualScale(reference, diameter);
            float referenceHeight = reference.sprite.bounds.size.y * referenceScale;
            return referenceHeight / Mathf.Max(.001f, wheel.sprite.bounds.size.y);
        }

        private Vector2 WheelAuthoredPosition(bool front) =>
            front ? Vehicle.frontWheelVisualPosition : Vehicle.rearWheelVisualPosition;

        private static bool PointInEllipse(Vector2 point, Rect rect)
        {
            float radiusX = Mathf.Max(1f, rect.width * .5f);
            float radiusY = Mathf.Max(1f, rect.height * .5f);
            Vector2 normalized = new Vector2((point.x - rect.center.x) / radiusX,
                (point.y - rect.center.y) / radiusY);
            return normalized.sqrMagnitude <= 1f;
        }

        private static Vector2[] BodyCorners(Rect rect) => new[]
        {
            new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin),
            new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax)
        };

        private static Vector2 CornerSigns(int index)
        {
            switch (index)
            {
                case 0: return new Vector2(-1f, 1f);
                case 1: return new Vector2(1f, 1f);
                case 2: return new Vector2(1f, -1f);
                default: return new Vector2(-1f, -1f);
            }
        }

        private void DrawActionBar()
        {
            EditorGUILayout.Space(3f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Restaurar guardado", GUILayout.Width(145f))) LoadWorkingValues();
                if (GUILayout.Button("Aplicar a las 6 fases", GUILayout.Width(165f))) SaveToAllPhases();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancelar y cerrar", GUILayout.Width(130f))) Close();
                GUI.backgroundColor = new Color(.45f, 1f, .55f);
                if (GUILayout.Button("Guardar fase", GUILayout.Width(140f))) SaveCurrentPhase();
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.Space(4f);
        }

        private void SaveCurrentPhase()
        {
            if (Vehicle == null || Skin == null) return;
            Undo.RecordObject(Vehicle, "Adjust Glitch Skin Visually");
            ApplyWorkingValues(Skin);
            ApplyWorkingWheelValues(PhaseWheel);
            EditorUtility.SetDirty(Vehicle);
            AssetDatabase.SaveAssets();
            LoadWorkingValues();
            VehicleDefinitionEditor.RefreshLivePreviews();
            ShowNotification(new GUIContent("Ajuste Glitch guardado"));
        }

        private void SaveToAllPhases()
        {
            if (Vehicle == null) return;
            Undo.RecordObject(Vehicle, "Adjust All Glitch Skins Visually");
            VehicleDefinition.CustomizationOption[] skins = GlitchSkins;
            for (int i = 0; i < skins.Length; i++) ApplyWorkingValues(skins[i]);
            VehicleDefinition.CustomizationOption[] wheels = Vehicle.wheels == null
                ? Array.Empty<VehicleDefinition.CustomizationOption>()
                : Vehicle.wheels.Where(GlitchCrystalTideEffect.IsGlitchWheel).ToArray();
            for (int i = 0; i < wheels.Length; i++) ApplyWorkingWheelValues(wheels[i]);
            EditorUtility.SetDirty(Vehicle);
            AssetDatabase.SaveAssets();
            LoadWorkingValues();
            VehicleDefinitionEditor.RefreshLivePreviews();
            ShowNotification(new GUIContent("Ajuste aplicado a las 6 fases"));
        }

        private void ApplyWorkingValues(VehicleDefinition.CustomizationOption skin)
        {
            skin.overrideGlitchTransform = workingOverride;
            skin.glitchManualSize = workingOverride ? workingSize : Vector2.zero;
            skin.glitchManualOffset = workingOverride ? workingOffset : Vector2.zero;
        }

        private void ApplyWorkingWheelValues(VehicleDefinition.CustomizationOption wheel)
        {
            if (wheel == null) return;
            wheel.overrideGlitchWheelTransform = workingWheelOverride;
            wheel.glitchRearWheelScale = workingWheelOverride ? workingRearWheelScale : 1f;
            wheel.glitchFrontWheelScale = workingWheelOverride ? workingFrontWheelScale : 1f;
            wheel.glitchRearWheelOffset = workingWheelOverride
                ? workingRearWheelOffset : Vector2.zero;
            wheel.glitchFrontWheelOffset = workingWheelOverride
                ? workingFrontWheelOffset : Vector2.zero;
        }

        private bool ConfirmDiscard()
        {
            if (!HasUnsavedChanges) return true;
            int choice = EditorUtility.DisplayDialogComplex("Ajuste Glitch sin guardar",
                "Hay cambios visuales temporales. ¿Qué deseas hacer?", "Guardar", "Cancelar", "Descartar");
            if (choice == 1) return false;
            if (choice == 0) SaveCurrentPhase();
            return true;
        }

        private void ScaleWorking(float multiplier)
        {
            if (selectedTarget == EditTarget.Body)
            {
                workingOverride = true;
                workingSize *= multiplier;
                workingSize.x = Mathf.Max(.02f, workingSize.x);
                workingSize.y = Mathf.Max(.02f, workingSize.y);
            }
            else
            {
                workingWheelOverride = true;
                if (selectedTarget == EditTarget.FrontWheel)
                    workingFrontWheelScale = Mathf.Clamp(workingFrontWheelScale * multiplier,
                        .05f, 10f);
                else
                    workingRearWheelScale = Mathf.Clamp(workingRearWheelScale * multiplier,
                        .05f, 10f);
            }
            Repaint();
        }

        private void Nudge(Vector2 amount)
        {
            if (selectedTarget == EditTarget.Body)
            {
                workingOverride = true;
                workingOffset += amount;
            }
            else if (selectedTarget == EditTarget.FrontWheel)
            {
                workingWheelOverride = true;
                workingFrontWheelOffset += amount;
            }
            else
            {
                workingWheelOverride = true;
                workingRearWheelOffset += amount;
            }
            Repaint();
        }

        private void ResetWorkingWheels()
        {
            workingRearWheelScale = 1f;
            workingFrontWheelScale = 1f;
            workingRearWheelOffset = Vector2.zero;
            workingFrontWheelOffset = Vector2.zero;
        }

        private static float ValidWheelMultiplier(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value > .01f ? value : 1f;

        private static Vector2 ValidVector(Vector2 value) =>
            float.IsNaN(value.x) || float.IsInfinity(value.x) ||
            float.IsNaN(value.y) || float.IsInfinity(value.y) ? Vector2.zero : value;

        private static Vector2 ResolveSize(Vector2 requested, Vector2 automatic) => new Vector2(
            requested.x > .001f ? requested.x : automatic.x,
            requested.y > .001f ? requested.y : automatic.y);

        private static string PhaseTitle(VehicleDefinition.CustomizationOption skin)
        {
            if (skin == null) return "(vacía)";
            string id = skin.optionId ?? string.Empty;
            int separator = id.LastIndexOf('-');
            if (separator >= 0 && separator < id.Length - 1 &&
                int.TryParse(id.Substring(separator + 1), out int distance))
                return $"{distance:N0} m — {skin.displayName}";
            return string.IsNullOrEmpty(skin.displayName) ? id : skin.displayName;
        }

        private static int PhaseDistance(string optionId)
        {
            if (string.IsNullOrEmpty(optionId)) return -1;
            int separator = optionId.LastIndexOf('-');
            return separator >= 0 && separator < optionId.Length - 1 &&
                   int.TryParse(optionId.Substring(separator + 1), out int distance)
                ? distance : -1;
        }

        private static string FormatVector(Vector2 value) => $"{value.x:0.###} × {value.y:0.###}";

        private static void DrawThumbnail(Sprite sprite)
        {
            Rect rect = GUILayoutUtility.GetRect(150f, 86f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(.08f, .09f, .12f));
            if (sprite == null) return;
            Texture2D preview = AssetPreview.GetAssetPreview(sprite);
            if (preview == null) preview = AssetPreview.GetMiniThumbnail(sprite);
            if (preview != null) GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
        }

        private static void DrawWheelThumbnail(Sprite sprite)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                Rect rect = GUILayoutUtility.GetRect(72f, 58f, GUILayout.Width(92f));
                EditorGUI.DrawRect(rect, new Color(.08f, .09f, .12f));
                Texture2D preview = AssetPreview.GetAssetPreview(sprite);
                if (preview == null) preview = AssetPreview.GetMiniThumbnail(sprite);
                if (preview != null) GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
                GUILayout.FlexibleSpace();
            }
        }

        private static void DrawSprite(Sprite sprite, Vector2 pivotWorld, Vector2 scale, bool flipX,
            Vector2 origin, float pixelsPerUnit, Color tint)
        {
            if (Event.current.type != EventType.Repaint || sprite == null || sprite.texture == null) return;
            Vector2 localCenter = Vector2.Scale((Vector2)sprite.bounds.center, scale);
            Vector2 centerWorld = pivotWorld + localCenter;
            Vector2 size = Vector2.Scale((Vector2)sprite.bounds.size,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y))) * pixelsPerUnit;
            Vector2 center = WorldToScreen(centerWorld, origin, pixelsPerUnit);
            Rect destination = new Rect(center.x - size.x * .5f, center.y - size.y * .5f,
                size.x, size.y);
            Rect textureRect = sprite.textureRect;
            Rect uv = new Rect(textureRect.x / sprite.texture.width, textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width, textureRect.height / sprite.texture.height);
            if (flipX)
            {
                uv.x += uv.width;
                uv.width = -uv.width;
            }
            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(destination, sprite.texture, uv, true);
            GUI.color = previous;
        }

        private static Vector2 WorldToScreen(Vector2 world, Vector2 origin, float pixelsPerUnit) =>
            new Vector2(origin.x + world.x * pixelsPerUnit, origin.y - world.y * pixelsPerUnit);
    }
}
