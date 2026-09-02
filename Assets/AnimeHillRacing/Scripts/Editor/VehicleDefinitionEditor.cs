using System;
using System.Collections.Generic;
using AnimeHillRacing.Road;
using AnimeHillRacing.UI;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    [CustomEditor(typeof(VehicleDefinition))]
    public sealed class VehicleDefinitionEditor : UnityEditor.Editor
    {
        private static readonly GUIContent OverrideLabel = new GUIContent(
            "Ajuste manual",
            "Permite cambiar el tamaño y la posición solamente para esta fase Glitch.");
        private static readonly GUIContent SizeLabel = new GUIContent(
            "Tamaño final X / Y",
            "Ancho y alto renderizados en unidades del mundo. Un valor cero conserva el tamaño automático en ese eje.");
        private static readonly GUIContent OffsetLabel = new GUIContent(
            "Desplazamiento X / Y",
            "Desplazamiento local adicional respecto a la posición predeterminada del cuerpo.");

        private bool showGlitchAdjuster = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            DrawPropertiesExcluding(serializedObject, "m_Script");

            EditorGUILayout.Space(10f);
            DrawGlitchAdjuster();

            bool changed = serializedObject.ApplyModifiedProperties();
            if (!changed) return;

            EditorUtility.SetDirty(target);
            RefreshLivePreviews();
        }

        private void DrawGlitchAdjuster()
        {
            SerializedProperty skins = serializedObject.FindProperty("skins");
            if (skins == null || !skins.isArray) return;

            List<int> glitchIndices = FindGlitchIndices(skins);
            showGlitchAdjuster = EditorGUILayout.BeginFoldoutHeaderGroup(showGlitchAdjuster,
                $"AJUSTADOR DE SKINS GLITCH PHASE ({glitchIndices.Count})");
            if (showGlitchAdjuster)
            {
                EditorGUILayout.HelpBox(
                    "El tamaño automático coincide con las dimensiones de la skin predeterminada. " +
                    "Activa el ajuste manual para modificar ancho, alto o posición de una fase. " +
                    "Durante Play Mode los cambios se reflejan inmediatamente en el garaje y el vehículo.",
                    MessageType.Info);

                if (glitchIndices.Count == 0)
                {
                    EditorGUILayout.HelpBox("Este vehículo todavía no tiene skins Glitch Phase.",
                        MessageType.Warning);
                }
                else
                {
                    VehicleDefinition vehicle = (VehicleDefinition)target;
                    Vector2 automaticSize = CalculateAutomaticSize(vehicle);
                    EditorGUILayout.LabelField("Tamaño automático del vehículo",
                        FormatVector(automaticSize), EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Posición predeterminada",
                        FormatVector(vehicle.bodyVisualOffset));

                    GUI.backgroundColor = new Color(.35f, .8f, 1f);
                    if (GUILayout.Button("ABRIR AJUSTADOR VISUAL", GUILayout.Height(28f)))
                        GlitchSkinVisualAdjusterWindow.OpenForVehicle(vehicle);
                    GUI.backgroundColor = Color.white;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Activar las 6 fases"))
                            EnableAll(skins, glitchIndices, automaticSize);
                        if (GUILayout.Button("Restaurar las 6 fases"))
                            ResetAll(skins, glitchIndices);
                    }

                    EditorGUILayout.Space(4f);
                    for (int i = 0; i < glitchIndices.Count; i++)
                        DrawPhase(skins, glitchIndices, glitchIndices[i], automaticSize,
                            vehicle.bodyVisualOffset);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawPhase(SerializedProperty skins, List<int> glitchIndices, int index,
            Vector2 automaticSize, Vector2 defaultOffset)
        {
            SerializedProperty option = skins.GetArrayElementAtIndex(index);
            SerializedProperty id = option.FindPropertyRelative("optionId");
            SerializedProperty displayName = option.FindPropertyRelative("displayName");
            SerializedProperty spriteProperty = option.FindPropertyRelative("sprite");
            SerializedProperty useManual = option.FindPropertyRelative("overrideGlitchTransform");
            SerializedProperty manualSize = option.FindPropertyRelative("glitchManualSize");
            SerializedProperty manualOffset = option.FindPropertyRelative("glitchManualOffset");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string title = PhaseTitle(id.stringValue, displayName.stringValue);
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawSpritePreview(spriteProperty.objectReferenceValue as Sprite);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.PropertyField(useManual, OverrideLabel);
                        using (new EditorGUI.DisabledScope(!useManual.boolValue))
                        {
                            EditorGUILayout.PropertyField(manualSize, SizeLabel);
                            EditorGUILayout.PropertyField(manualOffset, OffsetLabel);
                        }

                        Vector2 finalSize = ResolveFinalSize(useManual.boolValue,
                            manualSize.vector2Value, automaticSize);
                        Vector2 finalOffset = defaultOffset +
                            (useManual.boolValue ? manualOffset.vector2Value : Vector2.zero);
                        EditorGUILayout.LabelField("Resultado tamaño", FormatVector(finalSize));
                        EditorGUILayout.LabelField("Resultado posición", FormatVector(finalOffset));
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Usar tamaño automático"))
                    {
                        useManual.boolValue = true;
                        manualSize.vector2Value = automaticSize;
                    }
                    if (GUILayout.Button("Copiar a las 6 fases"))
                        CopyToAll(skins, glitchIndices, useManual.boolValue,
                            manualSize.vector2Value, manualOffset.vector2Value);
                    if (GUILayout.Button("Restaurar"))
                        ResetOption(option);
                }
            }
        }

        private void DrawSpritePreview(Sprite sprite)
        {
            Rect rect = GUILayoutUtility.GetRect(116f, 78f, GUILayout.Width(116f));
            if (sprite == null)
            {
                EditorGUI.DrawRect(rect, new Color(.12f, .12f, .12f));
                GUI.Label(rect, "SIN SPRITE", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Texture2D preview = AssetPreview.GetAssetPreview(sprite);
            if (preview == null) preview = AssetPreview.GetMiniThumbnail(sprite);
            if (preview != null) GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
            if (AssetPreview.IsLoadingAssetPreview(sprite.GetInstanceID())) Repaint();
        }

        private static List<int> FindGlitchIndices(SerializedProperty skins)
        {
            var result = new List<int>();
            for (int i = 0; i < skins.arraySize; i++)
            {
                SerializedProperty id = skins.GetArrayElementAtIndex(i).FindPropertyRelative("optionId");
                if (id != null && !string.IsNullOrEmpty(id.stringValue) &&
                    id.stringValue.StartsWith(GlitchCrystalTideEffect.SkinPrefix,
                        StringComparison.Ordinal))
                    result.Add(i);
            }
            return result;
        }

        private static Vector2 CalculateAutomaticSize(VehicleDefinition vehicle)
        {
            VehicleDefinition.CustomizationOption reference =
                vehicle.skins != null && vehicle.skins.Length > 0 ? vehicle.skins[0] : null;
            if (reference?.sprite == null) return Vector2.zero;
            float scale = VehicleDefinition.SpriteWidthScale(reference.sprite, vehicle.visualWidth);
            return reference.sprite.bounds.size * scale;
        }

        private static Vector2 ResolveFinalSize(bool useManual, Vector2 requested, Vector2 automatic)
        {
            if (!useManual) return automatic;
            return new Vector2(requested.x > .001f ? requested.x : automatic.x,
                requested.y > .001f ? requested.y : automatic.y);
        }

        private static void EnableAll(SerializedProperty skins, List<int> indices, Vector2 automaticSize)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                SerializedProperty option = skins.GetArrayElementAtIndex(indices[i]);
                option.FindPropertyRelative("overrideGlitchTransform").boolValue = true;
                option.FindPropertyRelative("glitchManualSize").vector2Value = automaticSize;
                option.FindPropertyRelative("glitchManualOffset").vector2Value = Vector2.zero;
            }
        }

        private static void ResetAll(SerializedProperty skins, List<int> indices)
        {
            for (int i = 0; i < indices.Count; i++)
                ResetOption(skins.GetArrayElementAtIndex(indices[i]));
        }

        private static void CopyToAll(SerializedProperty skins, List<int> indices, bool useManual,
            Vector2 manualSize, Vector2 manualOffset)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                SerializedProperty option = skins.GetArrayElementAtIndex(indices[i]);
                option.FindPropertyRelative("overrideGlitchTransform").boolValue = useManual;
                option.FindPropertyRelative("glitchManualSize").vector2Value = manualSize;
                option.FindPropertyRelative("glitchManualOffset").vector2Value = manualOffset;
            }
        }

        private static void ResetOption(SerializedProperty option)
        {
            option.FindPropertyRelative("overrideGlitchTransform").boolValue = false;
            option.FindPropertyRelative("glitchManualSize").vector2Value = Vector2.zero;
            option.FindPropertyRelative("glitchManualOffset").vector2Value = Vector2.zero;
        }

        private static string PhaseTitle(string optionId, string displayName)
        {
            int separator = optionId.LastIndexOf('-');
            if (separator >= 0 && separator < optionId.Length - 1 &&
                int.TryParse(optionId.Substring(separator + 1), out int distance))
                return $"{distance:N0} m  —  {displayName}";
            return string.IsNullOrEmpty(displayName) ? optionId : displayName;
        }

        private static string FormatVector(Vector2 value) => $"{value.x:0.###} × {value.y:0.###}";

        internal static void RefreshLivePreviews()
        {
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
            if (!Application.isPlaying) return;

            VehicleVisual2D[] visuals2D = UnityEngine.Object.FindObjectsByType<VehicleVisual2D>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < visuals2D.Length; i++) visuals2D[i].ApplySavedCustomization();

            VehicleVisual3D[] visuals3D = UnityEngine.Object.FindObjectsByType<VehicleVisual3D>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < visuals3D.Length; i++) visuals3D[i].ApplySavedCustomization();

            GarageUI[] garages = UnityEngine.Object.FindObjectsByType<GarageUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < garages.Length; i++) garages[i].RefreshVehicleDefinitionPreview();
        }
    }
}
