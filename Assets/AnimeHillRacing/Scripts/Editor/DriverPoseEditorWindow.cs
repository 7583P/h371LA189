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

        private static readonly PosePart[] LimbParts =
        {
            PosePart.Torso, PosePart.Head, PosePart.LeftUpperArm, PosePart.LeftForearm,
            PosePart.RightUpperArm, PosePart.RightForearm, PosePart.LeftThigh, PosePart.LeftLowerLeg,
            PosePart.RightThigh, PosePart.RightLowerLeg
        };

        private static readonly string[] PartNames =
        {
            "Piloto completo", "Torso", "Cabeza", "Brazo izq. superior", "Antebrazo izq.",
            "Brazo der. superior", "Antebrazo der.", "Muslo izq.", "Pierna izq.",
            "Muslo der.", "Pierna der.", "Accesorio"
        };

        private VehicleDefinition[] vehicles = Array.Empty<VehicleDefinition>();
        private DriverDefinition[] drivers = Array.Empty<DriverDefinition>();
        private int vehicleIndex;
        private int skinIndex;
        private int driverIndex;
        private int copyVehicleIndex;
        private PosePart selectedPart = PosePart.Root;
        private VehicleDefinition.DriverPose originalPose;
        private VehicleDefinition.DriverPose workingPose;
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
        private DriverDefinition CanonicalDriver => drivers.Length > 0 ? drivers[0] : null;
        private bool HasUnsavedChanges => Vehicle != null && workingPose != null &&
            JsonUtility.ToJson(workingPose) != JsonUtility.ToJson(Vehicle.driverPose);

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
            selectedPart = PosePart.Root;
            draggingHandle = false;
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
                    driverIndex = EditorGUILayout.Popup(Mathf.Clamp(driverIndex, 0, driverNames.Length - 1),
                        driverNames, EditorStyles.toolbarPopup, GUILayout.MinWidth(160f));
                else
                    GUILayout.Label("Sin personajes", EditorStyles.toolbarButton, GUILayout.MinWidth(160f));

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

            selectedPart = (PosePart)EditorGUILayout.Popup("Parte", (int)selectedPart, PartNames);
            EditorGUILayout.Space(5f);
            EditorGUI.BeginChangeCheck();
            if (selectedPart == PosePart.Root)
                DrawRootFields();
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
            EditorGUILayout.HelpBox("Arrastra el círculo amarillo en la vista. Selecciona otro círculo para editar esa parte.",
                MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private void DrawRootFields()
        {
            workingPose.rootPosition = EditorGUILayout.Vector2Field("Posición X/Y", workingPose.rootPosition);
            workingPose.rootScale = EditorGUILayout.Slider("Tamaño general", workingPose.rootScale, .25f, 2.5f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("-10%")) workingPose.rootScale = Mathf.Max(.05f, workingPose.rootScale * .9f);
                if (GUILayout.Button("+10%")) workingPose.rootScale = Mathf.Min(4f, workingPose.rootScale * 1.1f);
            }
        }

        private void DrawPartFields()
        {
            VehicleDefinition.DriverPartPose pose = GetPart(workingPose, selectedPart);
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
            pose.sortingOrder = EditorGUILayout.IntField("Orden de dibujo", pose.sortingOrder);
            SetPart(workingPose, selectedPart, pose);
        }

        private void DrawActionBar()
        {
            EditorGUILayout.Space(3f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Restaurar pose guardada", GUILayout.Width(165f)))
                    workingPose = ClonePose(Vehicle.driverPose);
                if (GUILayout.Button("Cancelar y cerrar", GUILayout.Width(130f)))
                {
                    workingPose = ClonePose(originalPose);
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
            EditorUtility.SetDirty(Vehicle);
            AssetDatabase.SaveAssets();
            originalPose = ClonePose(Vehicle.driverPose);
            workingPose = ClonePose(originalPose);
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

            DrawWheels(vehicle, origin, pixelsPerUnit);
            DrawDriver(driver, origin, pixelsPerUnit, false);
            DrawBody(vehicle, skin, origin, pixelsPerUnit);
            DrawDriver(driver, origin, pixelsPerUnit, true);
            DrawHandles(origin, pixelsPerUnit, canvas);
            HandleDragging(origin, pixelsPerUnit, canvas);

            GUI.Label(new Rect(canvas.x + 10f, canvas.y + 8f, canvas.width - 20f, 20f),
                $"{vehicle.displayName} · {skin.displayName} · {driver.displayName}", EditorStyles.whiteMiniLabel);
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
            float rearScale = vehicle.WheelCustomizationScale(wheel, vehicle.RearWheelDiameter);
            float frontScale = vehicle.WheelCustomizationScale(wheel, vehicle.FrontWheelDiameter);
            DrawSprite(wheel.sprite, vehicle.rearWheelVisualPosition, Vector2.one * rearScale, 0f, false,
                origin, pixelsPerUnit, new Color(1f, 1f, 1f, .9f));
            DrawSprite(wheel.sprite, vehicle.frontWheelVisualPosition, Vector2.one * frontScale, 0f, false,
                origin, pixelsPerUnit, new Color(1f, 1f, 1f, .9f));
        }

        private void DrawDriver(DriverDefinition driver, Vector2 origin, float pixelsPerUnit, bool foreground)
        {
            DriverDefinition reference = driver.PoseReference;
            PosePart[] ordered = LimbParts.OrderBy(part => GetPart(workingPose, part).sortingOrder).ToArray();
            for (int i = 0; i < ordered.Length; i++)
            {
                PosePart part = ordered[i];
                VehicleDefinition.DriverPartPose pose = GetPart(workingPose, part);
                if ((pose.sortingOrder >= 10) != foreground) continue;
                Sprite sprite = PartSprite(driver, part);
                Sprite referenceSprite = PartSprite(reference, part);
                if (!pose.visible || sprite == null) continue;
                Vector2 normalization = ReferenceScale(sprite, referenceSprite);
                float rootScale = Mathf.Max(.01f, workingPose.rootScale);
                Vector2 scale = Vector2.Scale(new Vector2(Mathf.Abs(pose.scale.x), Mathf.Abs(pose.scale.y)),
                    normalization) * (ArticulatedDriver2D.CharacterVisualScaleMultiplier * rootScale);
                Vector2 pivotCorrection = PivotCorrection(sprite, referenceSprite, normalization);
                pivotCorrection = Vector2.Scale(pivotCorrection,
                    new Vector2(Mathf.Abs(pose.scale.x), Mathf.Abs(pose.scale.y)) *
                    ArticulatedDriver2D.CharacterVisualScaleMultiplier);
                pivotCorrection = Rotate(pivotCorrection, pose.rotation) * rootScale;
                Vector2 worldPosition = workingPose.rootPosition + pose.position * rootScale + pivotCorrection;
                DrawSprite(sprite, worldPosition, scale, pose.rotation, false, origin, pixelsPerUnit, Color.white);
            }
        }

        private void DrawHandles(Vector2 origin, float pixelsPerUnit, Rect canvas)
        {
            if (Event.current.type != EventType.Repaint) return;
            for (int i = 0; i < LimbParts.Length; i++)
            {
                PosePart part = LimbParts[i];
                VehicleDefinition.DriverPartPose pose = GetPart(workingPose, part);
                if (!pose.visible) continue;
                Vector2 screen = WorldToScreen(PartAnchorWorld(pose), origin, pixelsPerUnit);
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
            for (int i = 0; i < LimbParts.Length; i++)
            {
                VehicleDefinition.DriverPartPose pose = GetPart(workingPose, LimbParts[i]);
                if (!pose.visible) continue;
                float next = Vector2.Distance(mouse, WorldToScreen(PartAnchorWorld(pose), origin, pixelsPerUnit));
                if (next >= distance) continue;
                distance = next;
                nearest = LimbParts[i];
            }
            return nearest;
        }

        private Vector2 PartAnchorWorld(VehicleDefinition.DriverPartPose pose) =>
            workingPose.rootPosition + pose.position * Mathf.Max(.01f, workingPose.rootScale);

        private VehicleDefinition.CustomizationOption SelectedSkin(VehicleDefinition vehicle)
        {
            if (vehicle == null || vehicle.skins == null || vehicle.skins.Length == 0) return null;
            return vehicle.skins[Mathf.Clamp(skinIndex, 0, vehicle.skins.Length - 1)];
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

        private static VehicleDefinition.DriverPartPose GetPart(VehicleDefinition.DriverPose pose, PosePart part)
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
                case PosePart.Accessory: return pose.accessory;
                default: return default;
            }
        }

        private static void SetPart(VehicleDefinition.DriverPose pose, PosePart part,
            VehicleDefinition.DriverPartPose value)
        {
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
                case PosePart.Accessory: pose.accessory = value; break;
            }
        }

        private static VehicleDefinition.DriverPose ClonePose(VehicleDefinition.DriverPose source) => source != null
            ? JsonUtility.FromJson<VehicleDefinition.DriverPose>(JsonUtility.ToJson(source))
            : new VehicleDefinition.DriverPose();
    }
}
