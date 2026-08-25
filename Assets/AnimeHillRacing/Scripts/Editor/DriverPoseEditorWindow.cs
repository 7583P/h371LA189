using System;
using System.Collections.Generic;
using System.Linq;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    public sealed class DriverPoseEditorWindow : EditorWindow
    {
        private enum PosePart
        {
            Root,
            RearWheel,
            FrontWheel,
            LeftThruster,
            RightThruster,
            Torso,
            Head,
            LeftUpperArm,
            LeftForearm,
            RightUpperArm,
            RightForearm,
            LeftThigh,
            LeftLowerLeg,
            RightThigh,
            RightLowerLeg,
            Accessory
        }

        private static readonly PosePart[] EditableParts =
        {
            PosePart.Torso, PosePart.Head, PosePart.LeftUpperArm, PosePart.LeftForearm,
            PosePart.RightUpperArm, PosePart.RightForearm, PosePart.LeftThigh, PosePart.LeftLowerLeg,
            PosePart.RightThigh, PosePart.RightLowerLeg, PosePart.Accessory
        };

        private static readonly string[] PartNames =
        {
            "Piloto completo", "Llanta trasera", "Llanta delantera", "Thruster izquierdo", "Thruster derecho",
            "Torso", "Cabeza", "Brazo izq. superior", "Antebrazo izq.",
            "Brazo der. superior", "Antebrazo der.", "Muslo izq.", "Pierna izq.",
            "Muslo der.", "Pierna der.", "Accesorio"
        };

        private VehicleDefinition[] vehicles = Array.Empty<VehicleDefinition>();
        private DriverDefinition[] drivers = Array.Empty<DriverDefinition>();
        private int vehicleIndex;
        private int skinIndex;
        private int driverIndex;
        private int accessoryIndex;
        private int copyVehicleIndex;
        private PosePart selectedPart = PosePart.Root;
        private VehicleDefinition.DriverPose originalPose;
        private VehicleDefinition.DriverPose workingPose;
        private Vector2 originalRearWheelPosition;
        private Vector2 originalFrontWheelPosition;
        private Vector2 workingRearWheelPosition;
        private Vector2 workingFrontWheelPosition;
        private float originalRearWheelDiameter;
        private float originalFrontWheelDiameter;
        private float workingRearWheelDiameter;
        private float workingFrontWheelDiameter;
        private Vector2 originalLeftThrusterPosition;
        private Vector2 originalRightThrusterPosition;
        private Vector2 workingLeftThrusterPosition;
        private Vector2 workingRightThrusterPosition;
        private float originalThrusterVisualWidth;
        private float workingThrusterVisualWidth;
        private Vector2 inspectorScroll;
        private float previewZoom = 1f;
        private bool uniformPartScale;
        private bool draggingHandle;

        [MenuItem("Anime Hill Racing/Vehicles/Driver Pose Editor")]
        public static void Open()
        {
            DriverPoseEditorWindow window = GetWindow<DriverPoseEditorWindow>();
            window.titleContent = new GUIContent("Driver Pose");
            window.minSize = new Vector2(850f, 520f);
            window.Show();
        }

        private VehicleDefinition Vehicle => vehicleIndex >= 0 && vehicleIndex < vehicles.Length
            ? vehicles[vehicleIndex] : null;
        private DriverDefinition PreviewDriver => driverIndex >= 0 && driverIndex < drivers.Length
            ? drivers[driverIndex] : null;
        private DriverDefinition.CharacterAccessoryOption PreviewAccessory =>
            PreviewDriver != null && PreviewDriver.accessories != null &&
            accessoryIndex >= 0 && accessoryIndex < PreviewDriver.accessories.Length
                ? PreviewDriver.accessories[accessoryIndex] : null;
        private DriverDefinition CanonicalDriver => drivers.Length > 0 ? drivers[0] : null;
        private bool HasEditableThrusters => Vehicle != null && Vehicle.underbodyAttachmentSprite != null;
        private bool HasUnsavedChanges => Vehicle != null && workingPose != null &&
            (JsonUtility.ToJson(workingPose) != JsonUtility.ToJson(Vehicle.driverPose) ||
             workingRearWheelPosition != Vehicle.rearWheelVisualPosition ||
             workingFrontWheelPosition != Vehicle.frontWheelVisualPosition ||
             !Mathf.Approximately(workingRearWheelDiameter, Vehicle.rearWheelVisualDiameter) ||
             !Mathf.Approximately(workingFrontWheelDiameter, Vehicle.frontWheelVisualDiameter) ||
             HasEditableThrusters &&
             (workingLeftThrusterPosition != SavedThrusterPosition(0, new Vector2(-1.02f, -.38f)) ||
              workingRightThrusterPosition != SavedThrusterPosition(1, new Vector2(1.02f, -.38f)) ||
              !Mathf.Approximately(workingThrusterVisualWidth,
                  Mathf.Max(.05f, Vehicle.underbodyAttachmentVisualWidth))));

        private void OnEnable()
        {
            ReloadAssets();
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable() => EditorApplication.projectChanged -= OnProjectChanged;

        private void OnProjectChanged()
        {
            if (!HasUnsavedChanges) ReloadAssets();
            Repaint();
        }

        private void ReloadAssets()
        {
            VehicleDefinition previousVehicle = Vehicle;
            DriverDefinition previousDriver = PreviewDriver;
            LoadCatalogOrder(out vehicles, out drivers);
            vehicleIndex = Mathf.Max(0, Array.IndexOf(vehicles, previousVehicle));
            driverIndex = Mathf.Max(0, Array.IndexOf(drivers, previousDriver));
            copyVehicleIndex = Mathf.Clamp(copyVehicleIndex, 0, Mathf.Max(0, vehicles.Length - 1));
            LoadWorkingPose();
        }

        private static void LoadCatalogOrder(out VehicleDefinition[] vehicleDefinitions,
            out DriverDefinition[] driverDefinitions)
        {
            GameCatalog catalog = AssetDatabase.FindAssets("t:GameCatalog")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameCatalog>)
                .FirstOrDefault(item => item != null);
            vehicleDefinitions = catalog != null && catalog.vehicles != null
                ? catalog.vehicles.Where(item => item != null).ToArray()
                : FindAssets<VehicleDefinition>();
            driverDefinitions = catalog != null && catalog.drivers != null
                ? catalog.drivers.Where(item => item != null).ToArray()
                : FindAssets<DriverDefinition>();
        }

        private static T[] FindAssets<T>() where T : UnityEngine.Object => AssetDatabase.FindAssets($"t:{typeof(T).Name}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(item => item != null)
            .OrderBy(item => item.name)
            .ToArray();

        private void LoadWorkingPose()
        {
            skinIndex = 0;
            originalPose = ClonePose(Vehicle != null ? Vehicle.driverPose : null);
            workingPose = ClonePose(originalPose);
            originalRearWheelPosition = Vehicle != null ? Vehicle.rearWheelVisualPosition : Vector2.zero;
            originalFrontWheelPosition = Vehicle != null ? Vehicle.frontWheelVisualPosition : Vector2.zero;
            workingRearWheelPosition = originalRearWheelPosition;
            workingFrontWheelPosition = originalFrontWheelPosition;
            originalRearWheelDiameter = Vehicle != null ? Vehicle.rearWheelVisualDiameter : 0f;
            originalFrontWheelDiameter = Vehicle != null ? Vehicle.frontWheelVisualDiameter : 0f;
            workingRearWheelDiameter = originalRearWheelDiameter;
            workingFrontWheelDiameter = originalFrontWheelDiameter;
            originalLeftThrusterPosition = SavedThrusterPosition(0, new Vector2(-1.02f, -.38f));
            originalRightThrusterPosition = SavedThrusterPosition(1, new Vector2(1.02f, -.38f));
            workingLeftThrusterPosition = originalLeftThrusterPosition;
            workingRightThrusterPosition = originalRightThrusterPosition;
            originalThrusterVisualWidth = Vehicle != null
                ? Mathf.Max(.05f, Vehicle.underbodyAttachmentVisualWidth) : .70f;
            workingThrusterVisualWidth = originalThrusterVisualWidth;
            selectedPart = PosePart.Root;
            draggingHandle = false;
            SelectVisibleAccessory();
        }

        private void OnGUI()
        {
            if (vehicles.Length == 0)
            {
                EditorGUILayout.HelpBox("No hay VehicleDefinition disponibles en el GameCatalog.", MessageType.Warning);
                return;
            }
            DrawToolbar();
            if (Vehicle == null || workingPose == null) return;

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                    DrawPreview();
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Clamp(position.width * .36f, 310f, 430f))))
                    DrawPoseInspector();
            }
            DrawActionBar();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                string[] vehicleNames = vehicles.Select(item => item.displayName).ToArray();
                int nextVehicle = EditorGUILayout.Popup(vehicleIndex, vehicleNames, EditorStyles.toolbarPopup,
                    GUILayout.MinWidth(150f));
                if (nextVehicle != vehicleIndex && ConfirmDiscard())
                {
                    vehicleIndex = nextVehicle;
                    LoadWorkingPose();
                }

                string[] skinNames = Vehicle != null && Vehicle.skins != null
                    ? Vehicle.skins.Select(item => item != null ? item.displayName : "(vacía)").ToArray()
                    : Array.Empty<string>();
                using (new EditorGUI.DisabledScope(skinNames.Length == 0))
                    skinIndex = EditorGUILayout.Popup(Mathf.Clamp(skinIndex, 0, Mathf.Max(0, skinNames.Length - 1)),
                        skinNames, EditorStyles.toolbarPopup, GUILayout.MinWidth(130f));

                string[] driverNames = drivers.Select((item, index) => index == 0
                    ? item.displayName + " (MODELO)" : item.displayName).ToArray();
                if (driverNames.Length > 0)
                {
                    int nextDriver = EditorGUILayout.Popup(Mathf.Clamp(driverIndex, 0, driverNames.Length - 1),
                        driverNames, EditorStyles.toolbarPopup, GUILayout.MinWidth(160f));
                    if (nextDriver != driverIndex)
                    {
                        driverIndex = nextDriver;
                        SelectVisibleAccessory();
                    }
                }
                else
                    GUILayout.Label("Sin personajes", EditorStyles.toolbarButton, GUILayout.MinWidth(160f));

                DriverDefinition previewDriver = PreviewDriver;
                string[] accessoryNames = previewDriver != null && previewDriver.accessories != null
                    ? previewDriver.accessories.Select(item => item != null ? item.displayName : "(vacío)").ToArray()
                    : Array.Empty<string>();
                if (accessoryNames.Length > 0)
                    accessoryIndex = EditorGUILayout.Popup(
                        Mathf.Clamp(accessoryIndex, 0, accessoryNames.Length - 1), accessoryNames,
                        EditorStyles.toolbarPopup, GUILayout.MinWidth(145f));

                GUILayout.FlexibleSpace();
                GUILayout.Label(HasUnsavedChanges ? "CAMBIOS TEMPORALES" : "GUARDADO",
                    HasUnsavedChanges ? EditorStyles.miniBoldLabel : EditorStyles.miniLabel);
            }
        }

        private void DrawPoseInspector()
        {
            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
            EditorGUILayout.LabelField("Pose del piloto", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("La pose pertenece al vehículo. Las skins de personaje usan como modelo a " +
                (CanonicalDriver != null ? CanonicalDriver.displayName : "la skin default") + ".", MessageType.Info);

            PosePart[] selectableParts = Enum.GetValues(typeof(PosePart)).Cast<PosePart>()
                .Where(part => HasEditableThrusters ||
                               part != PosePart.LeftThruster && part != PosePart.RightThruster)
                .ToArray();
            int selectedIndex = Array.IndexOf(selectableParts, selectedPart);
            if (selectedIndex < 0)
            {
                selectedPart = PosePart.Root;
                selectedIndex = 0;
            }
            string[] selectableNames = selectableParts.Select(part => PartNames[(int)part]).ToArray();
            selectedPart = selectableParts[EditorGUILayout.Popup("Parte", selectedIndex, selectableNames)];
            EditorGUILayout.Space(5f);
            EditorGUI.BeginChangeCheck();
            if (selectedPart == PosePart.Root)
                DrawRootFields();
            else if (selectedPart == PosePart.RearWheel || selectedPart == PosePart.FrontWheel)
                DrawWheelFields();
            else if (selectedPart == PosePart.LeftThruster || selectedPart == PosePart.RightThruster)
                DrawThrusterFields();
            else
                DrawPartFields();
            if (EditorGUI.EndChangeCheck()) Repaint();

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Copiar pose", EditorStyles.boldLabel);
            string[] vehicleNames = vehicles.Select(item => item.displayName).ToArray();
            copyVehicleIndex = EditorGUILayout.Popup("Desde", copyVehicleIndex, vehicleNames);
            using (new EditorGUI.DisabledScope(copyVehicleIndex == vehicleIndex))
                if (GUILayout.Button("Copiar desde este vehículo"))
                    workingPose = ClonePose(vehicles[copyVehicleIndex].driverPose);

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Vista", EditorStyles.boldLabel);
            previewZoom = EditorGUILayout.Slider("Zoom", previewZoom, .65f, 2f);
            EditorGUILayout.HelpBox("Arrastra el círculo amarillo para el piloto, los círculos celestes para las llantas " +
                "o los círculos magenta para los thrusters. Selecciona cualquier círculo para editar esa parte.",
                MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private void DrawRootFields()
        {
            workingPose.rootPosition = EditorGUILayout.Vector2Field("Posición X/Y", workingPose.rootPosition);
            workingPose.rootScale = EditorGUILayout.Slider("Escala de la pose", workingPose.rootScale, .25f, 2.5f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-10%")) workingPose.rootScale = Mathf.Max(.05f, workingPose.rootScale * .9f);
                if (GUILayout.Button("+10%")) workingPose.rootScale = Mathf.Min(4f, workingPose.rootScale * 1.1f);
            }
            EditorGUILayout.HelpBox("La escala de la pose ajusta la separación de las partes alrededor del asiento. " +
                "El tamaño visual X/Y de cada parte permanece estable entre vehículos.", MessageType.None);
        }

        private void DrawWheelFields()
        {
            bool front = selectedPart == PosePart.FrontWheel;
            Vector2 value = front ? workingFrontWheelPosition : workingRearWheelPosition;
            value = EditorGUILayout.Vector2Field("Posición X/Y", value);
            if (front) workingFrontWheelPosition = value;
            else workingRearWheelPosition = value;

            float customDiameter = front ? workingFrontWheelDiameter : workingRearWheelDiameter;
            float effectiveDiameter = customDiameter > .001f
                ? customDiameter : Mathf.Max(.05f, Vehicle.wheelVisualDiameter);
            EditorGUI.BeginChangeCheck();
            float editedDiameter = EditorGUILayout.FloatField(
                customDiameter > .001f ? "Diámetro" : "Diámetro (general)", effectiveDiameter);
            if (EditorGUI.EndChangeCheck()) customDiameter = Mathf.Max(.05f, editedDiameter);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-10%")) customDiameter = Mathf.Max(.05f, effectiveDiameter * .9f);
                if (GUILayout.Button("+10%")) customDiameter = effectiveDiameter * 1.1f;
                using (new EditorGUI.DisabledScope(customDiameter <= .001f))
                    if (GUILayout.Button("Usar tamaño general")) customDiameter = 0f;
            }
            if (front) workingFrontWheelDiameter = customDiameter;
            else workingRearWheelDiameter = customDiameter;

            EditorGUILayout.HelpBox("La posición controla la vista y el anclaje físico de la suspensión. " +
                "El diámetro cambia el tamaño visual y físico de esta llanta. También puedes arrastrar " +
                "su círculo celeste en la vista.", MessageType.Info);
        }

        private void DrawThrusterFields()
        {
            bool left = selectedPart == PosePart.LeftThruster;
            Vector2 value = left ? workingLeftThrusterPosition : workingRightThrusterPosition;
            value = EditorGUILayout.Vector2Field("Posición X/Y", value);
            if (left) workingLeftThrusterPosition = value;
            else workingRightThrusterPosition = value;

            workingThrusterVisualWidth = Mathf.Max(.05f,
                EditorGUILayout.FloatField("Tamaño (ancho)", workingThrusterVisualWidth));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-10%"))
                    workingThrusterVisualWidth = Mathf.Max(.05f, workingThrusterVisualWidth * .9f);
                if (GUILayout.Button("+10%"))
                    workingThrusterVisualWidth = Mathf.Min(2f, workingThrusterVisualWidth * 1.1f);
            }

            EditorGUILayout.HelpBox("Esta posición mueve el thruster visible y el punto físico desde el que " +
                "aplica su fuerza. El tamaño se aplica a ambos thrusters. También puedes arrastrar su " +
                "círculo magenta en la vista.", MessageType.Info);
        }

        private void DrawPartFields()
        {
            VehicleDefinition.DriverPartPose pose = GetPart(workingPose, selectedPart);
            if (selectedPart == PosePart.Accessory)
            {
                string accessoryName = PreviewAccessory != null ? PreviewAccessory.displayName : "ninguno";
                EditorGUILayout.HelpBox($"Editando el anclaje de: {accessoryName}. " +
                    "Puedes arrastrar su punto en la vista.", MessageType.Info);
            }
            EditorGUI.BeginChangeCheck();
            pose.position = EditorGUILayout.Vector2Field("Posición X/Y", pose.position);
            pose.rotation = EditorGUILayout.Slider("Rotación", pose.rotation, -180f, 180f);
            uniformPartScale = EditorGUILayout.Toggle("Escala uniforme", uniformPartScale);
            if (uniformPartScale)
            {
                float size = Mathf.Sqrt(Mathf.Abs(pose.scale.x * pose.scale.y));
                EditorGUI.BeginChangeCheck();
                size = EditorGUILayout.FloatField("Tamaño", size);
                if (EditorGUI.EndChangeCheck()) pose.scale = Vector2.one * Mathf.Max(.0001f, size);
            }
            else
            {
                pose.scale = EditorGUILayout.Vector2Field("Tamaño X/Y", pose.scale);
                pose.scale.x = Mathf.Max(.0001f, Mathf.Abs(pose.scale.x));
                pose.scale.y = Mathf.Max(.0001f, Mathf.Abs(pose.scale.y));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-10%")) pose.scale *= .9f;
                if (GUILayout.Button("+10%")) pose.scale *= 1.1f;
            }
            pose.visible = EditorGUILayout.Toggle("Visible", pose.visible);
            if (selectedPart == PosePart.Accessory)
            {
                EditorGUI.BeginChangeCheck();
                int depth = EditorGUILayout.Popup("Profundidad", AccessoryDepthIndex(pose),
                    new[] { "Detrás del vehículo", "Delante del vehículo", "Delante de todo" });
                if (EditorGUI.EndChangeCheck())
                    pose.vehicleLayer = depth == 0 ? DriverVehicleLayer.BehindVehicle :
                        depth == 1 ? DriverVehicleLayer.InFrontOfVehicle :
                        DriverVehicleLayer.InFrontOfEverything;
            }
            else
            {
                bool inFront = IsDriverPartInFront(selectedPart, pose);
                EditorGUI.BeginChangeCheck();
                int depth = EditorGUILayout.Popup("Profundidad", inFront ? 1 : 0,
                    new[] { "Detrás del vehículo", "Delante del vehículo" });
                if (EditorGUI.EndChangeCheck())
                    pose.vehicleLayer = depth == 1
                        ? DriverVehicleLayer.InFrontOfVehicle : DriverVehicleLayer.BehindVehicle;
            }
            pose.sortingOrder = Mathf.Min(pose.sortingOrder, ArticulatedDriver2D.MaximumDriverSortingOrder);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.IntField("Orden interno del piloto", pose.sortingOrder);
            EditorGUILayout.HelpBox("La profundidad se guarda por separado para cada parte. Por defecto, cabeza " +
                "y brazos quedan delante; torso y piernas detrás. Los accesorios pueden ir detrás, delante " +
                "del vehículo o delante de todo.",
                MessageType.None);
            bool partChanged = EditorGUI.EndChangeCheck();
            if (selectedPart != PosePart.Accessory || partChanged)
                SetPart(workingPose, selectedPart, pose);
        }

        private void DrawActionBar()
        {
            EditorGUILayout.Space(3f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Restaurar pose guardada", GUILayout.Width(165f)))
                {
                    workingPose = ClonePose(Vehicle.driverPose);
                    workingRearWheelPosition = Vehicle.rearWheelVisualPosition;
                    workingFrontWheelPosition = Vehicle.frontWheelVisualPosition;
                    workingRearWheelDiameter = Vehicle.rearWheelVisualDiameter;
                    workingFrontWheelDiameter = Vehicle.frontWheelVisualDiameter;
                    workingLeftThrusterPosition = SavedThrusterPosition(0, new Vector2(-1.02f, -.38f));
                    workingRightThrusterPosition = SavedThrusterPosition(1, new Vector2(1.02f, -.38f));
                    workingThrusterVisualWidth = Mathf.Max(.05f, Vehicle.underbodyAttachmentVisualWidth);
                }
                if (GUILayout.Button("Cancelar y cerrar", GUILayout.Width(130f)))
                {
                    workingPose = ClonePose(originalPose);
                    workingRearWheelPosition = originalRearWheelPosition;
                    workingFrontWheelPosition = originalFrontWheelPosition;
                    workingRearWheelDiameter = originalRearWheelDiameter;
                    workingFrontWheelDiameter = originalFrontWheelDiameter;
                    workingLeftThrusterPosition = originalLeftThrusterPosition;
                    workingRightThrusterPosition = originalRightThrusterPosition;
                    workingThrusterVisualWidth = originalThrusterVisualWidth;
                    Close();
                }
                GUI.backgroundColor = new Color(.45f, 1f, .55f);
                if (GUILayout.Button("Guardar pose", GUILayout.Width(135f))) SaveWorkingPose();
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.Space(4f);
        }

        private void SaveWorkingPose()
        {
            if (Vehicle == null || workingPose == null) return;
            Undo.RecordObject(Vehicle, "Save Driver Pose");
            Vehicle.driverPose = ClonePose(workingPose);
            Vehicle.preserveDriverPose = true;
            Vehicle.rearWheelVisualPosition = workingRearWheelPosition;
            Vehicle.frontWheelVisualPosition = workingFrontWheelPosition;
            Vehicle.rearWheelVisualDiameter = workingRearWheelDiameter;
            Vehicle.frontWheelVisualDiameter = workingFrontWheelDiameter;
            Vehicle.preserveWheelVisualPositions = true;
            if (HasEditableThrusters)
            {
                Vehicle.underbodyAttachmentPositions = new[]
                {
                    workingLeftThrusterPosition, workingRightThrusterPosition
                };
                Vehicle.underbodyAttachmentVisualWidth = workingThrusterVisualWidth;
                Vehicle.preserveUnderbodyAttachmentPositions = true;
            }
            EditorUtility.SetDirty(Vehicle);
            AssetDatabase.SaveAssets();
            originalPose = ClonePose(Vehicle.driverPose);
            workingPose = ClonePose(originalPose);
            originalRearWheelPosition = Vehicle.rearWheelVisualPosition;
            originalFrontWheelPosition = Vehicle.frontWheelVisualPosition;
            workingRearWheelPosition = originalRearWheelPosition;
            workingFrontWheelPosition = originalFrontWheelPosition;
            originalRearWheelDiameter = Vehicle.rearWheelVisualDiameter;
            originalFrontWheelDiameter = Vehicle.frontWheelVisualDiameter;
            workingRearWheelDiameter = originalRearWheelDiameter;
            workingFrontWheelDiameter = originalFrontWheelDiameter;
            originalLeftThrusterPosition = SavedThrusterPosition(0, new Vector2(-1.02f, -.38f));
            originalRightThrusterPosition = SavedThrusterPosition(1, new Vector2(1.02f, -.38f));
            workingLeftThrusterPosition = originalLeftThrusterPosition;
            workingRightThrusterPosition = originalRightThrusterPosition;
            originalThrusterVisualWidth = Mathf.Max(.05f, Vehicle.underbodyAttachmentVisualWidth);
            workingThrusterVisualWidth = originalThrusterVisualWidth;
            ShowNotification(new GUIContent("Pose guardada permanentemente"));
        }

        private bool ConfirmDiscard()
        {
            if (!HasUnsavedChanges) return true;
            int choice = EditorUtility.DisplayDialogComplex("Pose sin guardar",
                "Hay cambios temporales. ¿Qué deseas hacer?", "Guardar", "Cancelar", "Descartar");
            if (choice == 1) return false;
            if (choice == 0) SaveWorkingPose();
            return true;
        }

        private void DrawPreview()
        {
            Rect canvas = GUILayoutUtility.GetRect(420f, 10000f, 390f, 10000f,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(canvas, new Color(.055f, .065f, .085f));
            DrawGrid(canvas);
            if (Event.current.type != EventType.Repaint && Event.current.type != EventType.MouseDown &&
                Event.current.type != EventType.MouseDrag && Event.current.type != EventType.MouseUp) return;

            VehicleDefinition vehicle = Vehicle;
            VehicleDefinition.CustomizationOption skin = SelectedSkin(vehicle);
            DriverDefinition driver = PreviewDriver != null ? PreviewDriver : CanonicalDriver;
            if (skin == null || skin.sprite == null || driver == null) return;

            float pixelsPerUnit = Mathf.Min((canvas.width - 50f) / Mathf.Max(6.2f, vehicle.visualWidth + 1f),
                (canvas.height - 50f) / 3.8f) * previewZoom;
            Vector2 origin = new Vector2(canvas.center.x, canvas.center.y + canvas.height * .13f);

            VehicleDefinition.DriverPartPose accessoryPose = GetPart(workingPose, PosePart.Accessory);
            int accessoryDepth = AccessoryDepthIndex(accessoryPose);
            DrawDriverLayer(driver, false, origin, pixelsPerUnit);
            if (accessoryDepth == 0) DrawAccessory(accessoryPose, origin, pixelsPerUnit);
            DrawBody(vehicle, skin, origin, pixelsPerUnit);
            DrawThrusters(vehicle, origin, pixelsPerUnit);
            DrawDriverLayer(driver, true, origin, pixelsPerUnit);
            if (accessoryDepth == 1) DrawAccessory(accessoryPose, origin, pixelsPerUnit);
            DrawWheels(vehicle, origin, pixelsPerUnit);
            if (accessoryDepth == 2) DrawAccessory(accessoryPose, origin, pixelsPerUnit);
            DrawHandles(origin, pixelsPerUnit, canvas);
            HandleDragging(origin, pixelsPerUnit, canvas);

            GUI.Label(new Rect(canvas.x + 10f, canvas.y + 8f, canvas.width - 20f, 20f),
                $"{vehicle.displayName} · {skin.displayName} · {driver.displayName} · " +
                $"{(PreviewAccessory != null ? PreviewAccessory.displayName : "SIN ACCESORIO")}",
                EditorStyles.whiteMiniLabel);
        }

        private void DrawGrid(Rect canvas)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color line = new Color(.16f, .19f, .24f);
            for (float x = canvas.x; x < canvas.xMax; x += 24f)
                EditorGUI.DrawRect(new Rect(x, canvas.y, 1f, canvas.height), line);
            for (float y = canvas.y; y < canvas.yMax; y += 24f)
                EditorGUI.DrawRect(new Rect(canvas.x, y, canvas.width, 1f), line);
        }

        private void DrawBody(VehicleDefinition vehicle, VehicleDefinition.CustomizationOption skin,
            Vector2 origin, float pixelsPerUnit)
        {
            Vector3 scale3 = vehicle.BodyCustomizationScale(skin);
            DrawSprite(skin.sprite, vehicle.BodyCustomizationOffset(skin), new Vector2(scale3.x, scale3.y),
                0f, vehicle.flipBodyX, origin, pixelsPerUnit, Color.white);
        }

        private void DrawWheels(VehicleDefinition vehicle, Vector2 origin, float pixelsPerUnit)
        {
            VehicleDefinition.CustomizationOption wheel = vehicle.wheels != null && vehicle.wheels.Length > 0
                ? vehicle.wheels[0] : null;
            if (wheel == null || wheel.sprite == null) return;
            float rearDiameter = workingRearWheelDiameter > .001f
                ? workingRearWheelDiameter : vehicle.wheelVisualDiameter;
            float frontDiameter = workingFrontWheelDiameter > .001f
                ? workingFrontWheelDiameter : vehicle.wheelVisualDiameter;
            float rearScale = vehicle.WheelCustomizationScale(wheel, rearDiameter);
            float frontScale = vehicle.WheelCustomizationScale(wheel, frontDiameter);
            DrawSprite(wheel.sprite, workingRearWheelPosition, Vector2.one * rearScale, 0f, false,
                origin, pixelsPerUnit, new Color(1f, 1f, 1f, .9f));
            DrawSprite(wheel.sprite, workingFrontWheelPosition, Vector2.one * frontScale, 0f, false,
                origin, pixelsPerUnit, new Color(1f, 1f, 1f, .9f));
        }

        private void DrawThrusters(VehicleDefinition vehicle, Vector2 origin, float pixelsPerUnit)
        {
            Sprite sprite = vehicle.underbodyAttachmentSprite;
            if (!HasEditableThrusters || sprite == null) return;
            float scale = VehicleDefinition.SpriteWidthScale(sprite,
                Mathf.Max(.05f, workingThrusterVisualWidth));
            DrawSprite(sprite, workingLeftThrusterPosition, Vector2.one * scale, 0f, false,
                origin, pixelsPerUnit, Color.white);
            DrawSprite(sprite, workingRightThrusterPosition, Vector2.one * scale, 0f, false,
                origin, pixelsPerUnit, Color.white);
        }

        private void DrawDriverLayer(DriverDefinition driver, bool foreground, Vector2 origin, float pixelsPerUnit)
        {
            DriverDefinition reference = driver.PoseReference;
            PosePart[] ordered = EditableParts
                .Where(part => part != PosePart.Accessory)
                .Where(part => IsDriverPartInFront(part, GetPart(workingPose, part)) == foreground)
                .OrderBy(part => GetPart(workingPose, part).sortingOrder)
                .ToArray();
            for (int i = 0; i < ordered.Length; i++)
            {
                PosePart part = ordered[i];
                VehicleDefinition.DriverPartPose pose = GetPart(workingPose, part);
                Sprite sprite = PartSprite(driver, part);
                Sprite referenceSprite = PartSprite(reference, part);
                if (!pose.visible || sprite == null) continue;
                Vector2 normalization = ReferenceScale(sprite, referenceSprite);
                float rootScale = Mathf.Max(.01f, workingPose.rootScale);
                Vector2 scale = Vector2.Scale(new Vector2(Mathf.Abs(pose.scale.x), Mathf.Abs(pose.scale.y)),
                    normalization) * ArticulatedDriver2D.CharacterVisualScaleMultiplier;
                Vector2 pivotCorrection = PivotCorrection(sprite, referenceSprite, normalization);
                pivotCorrection = Vector2.Scale(pivotCorrection,
                    new Vector2(Mathf.Abs(pose.scale.x), Mathf.Abs(pose.scale.y)) *
                    ArticulatedDriver2D.CharacterVisualScaleMultiplier);
                pivotCorrection = Rotate(pivotCorrection, pose.rotation);
                Vector2 worldPosition = workingPose.rootPosition + pose.position * rootScale + pivotCorrection;
                DrawSprite(sprite, worldPosition, scale, pose.rotation, false, origin, pixelsPerUnit, Color.white);
            }
        }

        private static bool IsDriverPartInFront(PosePart part, VehicleDefinition.DriverPartPose pose)
        {
            if (part == PosePart.Accessory) return AccessoryDepthIndex(pose) > 0;
            bool partDefault = part == PosePart.Head ||
                               part == PosePart.LeftUpperArm || part == PosePart.LeftForearm ||
                               part == PosePart.RightUpperArm || part == PosePart.RightForearm;
            return pose.RendersInFrontOfVehicle(partDefault);
        }

        private static int AccessoryDepthIndex(VehicleDefinition.DriverPartPose pose)
        {
            if (pose.vehicleLayer == DriverVehicleLayer.BehindVehicle) return 0;
            if (pose.vehicleLayer == DriverVehicleLayer.InFrontOfVehicle) return 1;
            return 2;
        }

        private void DrawAccessory(VehicleDefinition.DriverPartPose pose, Vector2 origin, float pixelsPerUnit)
        {
            DriverDefinition.CharacterAccessoryOption option = PreviewAccessory;
            Sprite sprite = option != null ? option.sprite : null;
            if (!pose.visible || sprite == null) return;
            float sourceWidth = Mathf.Max(.001f, sprite.bounds.size.x);
            float fittedScale = option.visibleWidth / sourceWidth;
            Vector2 scale = new Vector2(Mathf.Abs(pose.scale.x), Mathf.Abs(pose.scale.y)) *
                            (fittedScale * ArticulatedDriver2D.CharacterVisualScaleMultiplier);
            bool flipX = option.supportsFlipVariant
                ? SaveManager.GetCharacterAccessoryFlipX(option.accessoryId, option.flipX)
                : option.flipX;
            float rootScale = Mathf.Max(.01f, workingPose.rootScale);
            Vector2 sharedOffset = option.anchorOffset;
            if (option.supportsFlipVariant && flipX) sharedOffset.x = -sharedOffset.x;
            Vector2 localPosition = pose.position + sharedOffset;
            Vector2 worldPosition = workingPose.rootPosition + localPosition * rootScale;
            DrawSprite(sprite, worldPosition, scale, pose.rotation, flipX, origin, pixelsPerUnit, Color.white);
        }

        private void DrawHandles(Vector2 origin, float pixelsPerUnit, Rect canvas)
        {
            if (Event.current.type != EventType.Repaint) return;
            for (int i = 0; i < EditableParts.Length; i++)
            {
                PosePart part = EditableParts[i];
                VehicleDefinition.DriverPartPose pose = GetPart(workingPose, part);
                if (!pose.visible) continue;
                Vector2 screen = WorldToScreen(PartAnchorWorld(part, pose), origin, pixelsPerUnit);
                if (!canvas.Contains(screen)) continue;
                Handles.BeginGUI();
                Handles.color = selectedPart == part ? Color.yellow : new Color(1f, 1f, 1f, .55f);
                Handles.DrawSolidDisc(screen, Vector3.forward, selectedPart == part ? 5f : 3f);
                Handles.EndGUI();
            }
            Vector2 rootScreen = WorldToScreen(workingPose.rootPosition, origin, pixelsPerUnit);
            Handles.BeginGUI();
            Handles.color = selectedPart == PosePart.Root ? new Color(1f, .75f, .05f) : new Color(1f, .75f, .05f, .55f);
            Handles.DrawWireDisc(rootScreen, Vector3.forward, 7f);
            Handles.EndGUI();

            DrawWheelHandle(PosePart.RearWheel, workingRearWheelPosition, origin, pixelsPerUnit, canvas);
            DrawWheelHandle(PosePart.FrontWheel, workingFrontWheelPosition, origin, pixelsPerUnit, canvas);
            if (HasEditableThrusters)
            {
                DrawThrusterHandle(PosePart.LeftThruster, workingLeftThrusterPosition,
                    origin, pixelsPerUnit, canvas);
                DrawThrusterHandle(PosePart.RightThruster, workingRightThrusterPosition,
                    origin, pixelsPerUnit, canvas);
            }
        }

        private void DrawWheelHandle(PosePart part, Vector2 position, Vector2 origin, float pixelsPerUnit, Rect canvas)
        {
            Vector2 screen = WorldToScreen(position, origin, pixelsPerUnit);
            if (!canvas.Contains(screen)) return;
            Handles.BeginGUI();
            Handles.color = selectedPart == part ? Color.cyan : new Color(.25f, .85f, 1f, .65f);
            Handles.DrawSolidDisc(screen, Vector3.forward, selectedPart == part ? 6f : 4f);
            Handles.EndGUI();
        }

        private void DrawThrusterHandle(PosePart part, Vector2 position, Vector2 origin,
            float pixelsPerUnit, Rect canvas)
        {
            Vector2 screen = WorldToScreen(position, origin, pixelsPerUnit);
            if (!canvas.Contains(screen)) return;
            Handles.BeginGUI();
            Handles.color = selectedPart == part ? Color.magenta : new Color(1f, .25f, .9f, .7f);
            Handles.DrawSolidDisc(screen, Vector3.forward, selectedPart == part ? 6f : 4f);
            Handles.EndGUI();
        }

        private void HandleDragging(Vector2 origin, float pixelsPerUnit, Rect canvas)
        {
            Event evt = Event.current;
            if (evt.button != 0) return;
            if (evt.type == EventType.MouseDown && canvas.Contains(evt.mousePosition))
            {
                PosePart nearest = NearestHandle(evt.mousePosition, origin, pixelsPerUnit, out float distance);
                if (distance <= 13f)
                {
                    selectedPart = nearest;
                    draggingHandle = true;
                    evt.Use();
                }
            }
            else if (evt.type == EventType.MouseDrag && draggingHandle)
            {
                Vector2 worldDelta = new Vector2(evt.delta.x, -evt.delta.y) / Mathf.Max(1f, pixelsPerUnit);
                if (selectedPart == PosePart.Root)
                    workingPose.rootPosition += worldDelta;
                else if (selectedPart == PosePart.RearWheel)
                    workingRearWheelPosition += worldDelta;
                else if (selectedPart == PosePart.FrontWheel)
                    workingFrontWheelPosition += worldDelta;
                else if (selectedPart == PosePart.LeftThruster)
                    workingLeftThrusterPosition += worldDelta;
                else if (selectedPart == PosePart.RightThruster)
                    workingRightThrusterPosition += worldDelta;
                else
                {
                    VehicleDefinition.DriverPartPose pose = GetPart(workingPose, selectedPart);
                    pose.position += worldDelta / Mathf.Max(.01f, workingPose.rootScale);
                    SetPart(workingPose, selectedPart, pose);
                }
                evt.Use();
                Repaint();
            }
            else if (evt.type == EventType.MouseUp && draggingHandle)
            {
                draggingHandle = false;
                evt.Use();
            }
        }

        private PosePart NearestHandle(Vector2 mouse, Vector2 origin, float pixelsPerUnit, out float distance)
        {
            PosePart nearest = PosePart.Root;
            distance = Vector2.Distance(mouse, WorldToScreen(workingPose.rootPosition, origin, pixelsPerUnit));
            ConsiderHandle(PosePart.RearWheel, workingRearWheelPosition, mouse, origin, pixelsPerUnit,
                ref nearest, ref distance);
            ConsiderHandle(PosePart.FrontWheel, workingFrontWheelPosition, mouse, origin, pixelsPerUnit,
                ref nearest, ref distance);
            if (HasEditableThrusters)
            {
                ConsiderHandle(PosePart.LeftThruster, workingLeftThrusterPosition, mouse, origin, pixelsPerUnit,
                    ref nearest, ref distance);
                ConsiderHandle(PosePart.RightThruster, workingRightThrusterPosition, mouse, origin, pixelsPerUnit,
                    ref nearest, ref distance);
            }
            for (int i = 0; i < EditableParts.Length; i++)
            {
                PosePart part = EditableParts[i];
                VehicleDefinition.DriverPartPose pose = GetPart(workingPose, part);
                if (!pose.visible) continue;
                float next = Vector2.Distance(mouse,
                    WorldToScreen(PartAnchorWorld(part, pose), origin, pixelsPerUnit));
                if (next >= distance) continue;
                distance = next;
                nearest = part;
            }
            return nearest;
        }

        private static void ConsiderHandle(PosePart part, Vector2 worldPosition, Vector2 mouse, Vector2 origin,
            float pixelsPerUnit, ref PosePart nearest, ref float distance)
        {
            float next = Vector2.Distance(mouse, WorldToScreen(worldPosition, origin, pixelsPerUnit));
            if (next >= distance) return;
            distance = next;
            nearest = part;
        }

        private Vector2 PartAnchorWorld(PosePart part, VehicleDefinition.DriverPartPose pose)
        {
            Vector2 position = pose.position;
            if (part == PosePart.Accessory && PreviewAccessory != null)
                position += PreviewAccessory.anchorOffset;
            return workingPose.rootPosition + position * Mathf.Max(.01f, workingPose.rootScale);
        }

        private void SelectVisibleAccessory()
        {
            DriverDefinition driver = PreviewDriver;
            if (driver == null || driver.accessories == null || driver.accessories.Length == 0)
            {
                accessoryIndex = 0;
                return;
            }

            string selectedId = SaveManager.GetSelectedCharacterAccessoryId(driver.DefaultAccessoryId);
            int selected = Array.FindIndex(driver.accessories,
                item => item != null && item.accessoryId == selectedId && item.sprite != null);
            if (selected < 0)
                selected = Array.FindIndex(driver.accessories, item => item != null && item.sprite != null);
            accessoryIndex = Mathf.Max(0, selected);
        }

        private VehicleDefinition.CustomizationOption SelectedSkin(VehicleDefinition vehicle)
        {
            if (vehicle == null || vehicle.skins == null || vehicle.skins.Length == 0) return null;
            return vehicle.skins[Mathf.Clamp(skinIndex, 0, vehicle.skins.Length - 1)];
        }

        private Vector2 SavedThrusterPosition(int index, Vector2 fallback)
        {
            Vector2[] positions = Vehicle != null ? Vehicle.underbodyAttachmentPositions : null;
            return positions != null && index >= 0 && index < positions.Length ? positions[index] : fallback;
        }

        private static void DrawSprite(Sprite sprite, Vector2 pivotWorld, Vector2 scale, float rotation,
            bool flipX, Vector2 origin, float pixelsPerUnit, Color tint)
        {
            if (Event.current.type != EventType.Repaint || sprite == null || sprite.texture == null) return;
            Vector2 localCenter = Vector2.Scale((Vector2)sprite.bounds.center, scale);
            Vector2 centerWorld = pivotWorld + Rotate(localCenter, rotation);
            Vector2 size = Vector2.Scale((Vector2)sprite.bounds.size,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y))) * pixelsPerUnit;
            Vector2 center = WorldToScreen(centerWorld, origin, pixelsPerUnit);
            Rect destination = new Rect(center.x - size.x * .5f, center.y - size.y * .5f, size.x, size.y);
            Rect textureRect = sprite.textureRect;
            Rect uv = new Rect(textureRect.x / sprite.texture.width, textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width, textureRect.height / sprite.texture.height);
            if (flipX)
            {
                uv.x += uv.width;
                uv.width = -uv.width;
            }
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(-rotation, center);
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(destination, sprite.texture, uv, true);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private static Vector2 WorldToScreen(Vector2 world, Vector2 origin, float pixelsPerUnit) =>
            new Vector2(origin.x + world.x * pixelsPerUnit, origin.y - world.y * pixelsPerUnit);

        private static Vector2 Rotate(Vector2 value, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
        }

        private static Vector2 ReferenceScale(Sprite sprite, Sprite reference)
        {
            if (sprite == null || reference == null || sprite == reference) return Vector2.one;
            return new Vector2(reference.bounds.size.x / Mathf.Max(.001f, sprite.bounds.size.x),
                reference.bounds.size.y / Mathf.Max(.001f, sprite.bounds.size.y));
        }

        private static Vector2 PivotCorrection(Sprite sprite, Sprite reference, Vector2 scale)
        {
            if (sprite == null || reference == null || sprite == reference) return Vector2.zero;
            return (Vector2)reference.bounds.center - Vector2.Scale(sprite.bounds.center, scale);
        }

        private static Sprite PartSprite(DriverDefinition driver, PosePart part)
        {
            if (driver == null) return null;
            switch (part)
            {
                case PosePart.Torso: return driver.torso;
                case PosePart.Head: return driver.head;
                case PosePart.LeftUpperArm:
                case PosePart.RightUpperArm: return driver.upperArm;
                case PosePart.LeftForearm:
                case PosePart.RightForearm: return driver.forearm;
                case PosePart.LeftThigh:
                case PosePart.RightThigh: return driver.thigh;
                case PosePart.LeftLowerLeg:
                case PosePart.RightLowerLeg: return driver.lowerLeg;
                default: return null;
            }
        }

        private VehicleDefinition.DriverPartPose GetPart(VehicleDefinition.DriverPose pose, PosePart part)
        {
            switch (part)
            {
                case PosePart.Torso: return pose.torso;
                case PosePart.Head: return pose.head;
                case PosePart.LeftUpperArm: return pose.leftUpperArm;
                case PosePart.LeftForearm: return pose.leftForearm;
                case PosePart.RightUpperArm: return pose.rightUpperArm;
                case PosePart.RightForearm: return pose.rightForearm;
                case PosePart.LeftThigh: return pose.leftThigh;
                case PosePart.LeftLowerLeg: return pose.leftLowerLeg;
                case PosePart.RightThigh: return pose.rightThigh;
                case PosePart.RightLowerLeg: return pose.rightLowerLeg;
                case PosePart.Accessory:
                    if (PreviewAccessory != null &&
                        pose.TryGetAccessoryPose(PreviewAccessory.accessoryId, out VehicleDefinition.DriverPartPose accessoryPose))
                        return accessoryPose;
                    if (IsScarf(PreviewAccessory))
                    {
                        VehicleDefinition.DriverPartPose legacyScarfPose = pose.accessory;
                        legacyScarfPose.position = Vector2.Lerp(pose.torso.position, pose.head.position, .36f);
                        legacyScarfPose.sortingOrder = Mathf.Min(ArticulatedDriver2D.MaximumDriverSortingOrder,
                            Mathf.Max(pose.torso.sortingOrder, pose.head.sortingOrder) + 1);
                        return legacyScarfPose;
                    }
                    return pose.accessory;
                default: return default;
            }
        }

        private void SetPart(VehicleDefinition.DriverPose pose, PosePart part,
            VehicleDefinition.DriverPartPose value)
        {
            value.sortingOrder = Mathf.Min(value.sortingOrder, ArticulatedDriver2D.MaximumDriverSortingOrder);
            switch (part)
            {
                case PosePart.Torso: pose.torso = value; break;
                case PosePart.Head: pose.head = value; break;
                case PosePart.LeftUpperArm: pose.leftUpperArm = value; break;
                case PosePart.LeftForearm: pose.leftForearm = value; break;
                case PosePart.RightUpperArm: pose.rightUpperArm = value; break;
                case PosePart.RightForearm: pose.rightForearm = value; break;
                case PosePart.LeftThigh: pose.leftThigh = value; break;
                case PosePart.LeftLowerLeg: pose.leftLowerLeg = value; break;
                case PosePart.RightThigh: pose.rightThigh = value; break;
                case PosePart.RightLowerLeg: pose.rightLowerLeg = value; break;
                case PosePart.Accessory:
                    if (PreviewAccessory != null)
                        pose.SetAccessoryPose(PreviewAccessory.accessoryId, value);
                    else
                        pose.accessory = value;
                    break;
            }
        }

        private static bool IsScarf(DriverDefinition.CharacterAccessoryOption option) =>
            option != null && (option.accessoryId == "scarf" ||
                option.displayName.IndexOf("scarf", StringComparison.OrdinalIgnoreCase) >= 0);

        private static VehicleDefinition.DriverPose ClonePose(VehicleDefinition.DriverPose source) => source != null
            ? JsonUtility.FromJson<VehicleDefinition.DriverPose>(JsonUtility.ToJson(source))
            : new VehicleDefinition.DriverPose();
    }
}
