using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class TeleportModalUI : MonoBehaviour
    {
        private GameObject modalContainer;
        private Text distanceDisplay;
        private Text phaseDescriptionText;
        private float selectedDistance = 32768f;
        private bool isOpen;
        private float previousTimeScale = 1f;

        public bool IsOpen => isOpen;

        public static TeleportModalUI Create(Transform parent)
        {
            Canvas rootCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            Transform targetParent = rootCanvas != null ? rootCanvas.transform : parent;

            if (targetParent != null)
            {
                GraphicRaycaster raycaster = targetParent.GetComponent<GraphicRaycaster>();
                if (raycaster == null && rootCanvas != null)
                    rootCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            GameObject go = new GameObject("Teleport Modal Controller", typeof(RectTransform));
            go.transform.SetParent(targetParent, false);
            RectTransform rootRect = go.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            TeleportModalUI modal = go.AddComponent<TeleportModalUI>();
            modal.BuildUI();
            return modal;
        }

        private void BuildUI()
        {
            // Fullscreen Dimmed background blocker
            modalContainer = new GameObject("Modal Container", typeof(RectTransform), typeof(Image));
            modalContainer.transform.SetParent(transform, false);
            RectTransform blockerRect = modalContainer.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            Image blockerImage = modalContainer.GetComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0.05f, 0.78f);
            blockerImage.raycastTarget = true;

            // Window Card
            GameObject window = new GameObject("Window", typeof(RectTransform), typeof(Image));
            window.transform.SetParent(modalContainer.transform, false);
            RectTransform winRect = window.GetComponent<RectTransform>();
            winRect.anchorMin = new Vector2(0.5f, 0.5f);
            winRect.anchorMax = new Vector2(0.5f, 0.5f);
            winRect.pivot = new Vector2(0.5f, 0.5f);
            winRect.anchoredPosition = Vector2.zero;
            winRect.sizeDelta = new Vector2(500f, 520f);

            Image winImg = window.GetComponent<Image>();
            winImg.color = new Color(0.07f, 0.08f, 0.17f, 0.98f);
            winImg.raycastTarget = true;

            Outline winOutline = window.AddComponent<Outline>();
            winOutline.effectColor = new Color(0.2f, 0.85f, 1f, 0.95f);
            winOutline.effectDistance = new Vector2(2.5f, -2.5f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Header Title
            CreateText(window.transform, "Title", "🚀 MENU DE TELETRANSPORTE", 21, FontStyle.Bold,
                new Vector2(0f, 225f), new Vector2(460f, 38f), new Color(0.3f, 0.9f, 1f, 1f), font);

            // Target Distance Card
            GameObject displayCard = new GameObject("Display Card", typeof(RectTransform), typeof(Image));
            displayCard.transform.SetParent(window.transform, false);
            RectTransform cardRect = displayCard.GetComponent<RectTransform>();
            cardRect.anchoredPosition = new Vector2(0f, 160f);
            cardRect.sizeDelta = new Vector2(440f, 70f);
            Image cardImg = displayCard.GetComponent<Image>();
            cardImg.color = new Color(0.03f, 0.04f, 0.10f, 0.98f);
            cardImg.raycastTarget = false;

            Outline cardOutline = displayCard.AddComponent<Outline>();
            cardOutline.effectColor = new Color(1f, 0.85f, 0.2f, 0.85f);
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

            distanceDisplay = CreateText(displayCard.transform, "Distance Text", "32,768 m", 28, FontStyle.Bold,
                new Vector2(0f, 10f), new Vector2(420f, 36f), new Color(1f, 0.92f, 0.2f, 1f), font);

            phaseDescriptionText = CreateText(displayCard.transform, "Phase Text", "Fase 1 (32K): Pre-Glitch & Hundimiento leve", 12, FontStyle.Italic,
                new Vector2(0f, -18f), new Vector2(420f, 22f), new Color(0.7f, 0.85f, 1f, 0.9f), font);

            // Fine Adjustment Buttons (+500m / -500m / +5000m / -5000m)
            CreateButton(window.transform, "BtnMinus5000", "-5,000m", new Vector2(-165f, 95f), new Vector2(100f, 38f),
                new Color(0.18f, 0.22f, 0.38f, 0.95f), font, () => AddDistance(-5000f));
            CreateButton(window.transform, "BtnMinus500", "-500m", new Vector2(-55f, 95f), new Vector2(100f, 38f),
                new Color(0.24f, 0.30f, 0.52f, 0.95f), font, () => AddDistance(-500f));
            CreateButton(window.transform, "BtnPlus500", "+500m", new Vector2(55f, 95f), new Vector2(100f, 38f),
                new Color(0.15f, 0.52f, 0.38f, 0.95f), font, () => AddDistance(500f));
            CreateButton(window.transform, "BtnPlus5000", "+5,000m", new Vector2(165f, 95f), new Vector2(100f, 38f),
                new Color(0.18f, 0.62f, 0.44f, 0.95f), font, () => AddDistance(5000f));

            // Section Label
            CreateText(window.transform, "PresetLabel", "PREAJUSTES RAPIDOS (TECLAS 1 AL 6):", 12, FontStyle.Bold,
                new Vector2(0f, 52f), new Vector2(440f, 20f), new Color(0.8f, 0.85f, 0.95f, 0.9f), font);

            // Quick Preset Grid - Row 1
            float[] presetRow1 = { 0f, 1000f, 10000f };
            string[] labelsRow1 = { "1.  0 m", "2.  1,000 m", "3.  10,000 m" };
            for (int i = 0; i < 3; i++)
            {
                float d = presetRow1[i];
                CreateButton(window.transform, $"Preset_{d}", labelsRow1[i],
                    new Vector2(-150f + i * 150f, 18f), new Vector2(140f, 34f),
                    new Color(0.12f, 0.16f, 0.30f, 0.95f), font, () => SetDistance(d));
            }

            // Quick Preset Grid - Row 2 (Glitch Phases)
            float[] presetRow2 = { 32768f, 65536f, 131072f };
            string[] labelsRow2 = { "4. 32,768 m (32K)", "5. 65,536 m (65K)", "6. 131,072 m (131K)" };
            for (int i = 0; i < 3; i++)
            {
                float d = presetRow2[i];
                CreateButton(window.transform, $"Preset_{d}", labelsRow2[i],
                    new Vector2(-150f + i * 150f, -22f), new Vector2(140f, 34f),
                    new Color(0.28f, 0.18f, 0.42f, 0.95f), font, () => SetDistance(d));
            }

            // Controls Hint Label
            CreateText(window.transform, "HintLabel", "⌨ Teclado: Flechas [← / →] ±500m | [↑ / ↓] ±5K | [ENTER] Teletransportar", 11, FontStyle.Normal,
                new Vector2(0f, -62f), new Vector2(460f, 22f), new Color(0.6f, 0.75f, 0.9f, 0.85f), font);

            // Execute Teleport Button
            CreateButton(window.transform, "BtnTeleport", "🚀 ¡TELETRANSPORTAR AHORA! [ENTER]", new Vector2(0f, -112f), new Vector2(430f, 54f),
                new Color(0.08f, 0.70f, 0.36f, 0.98f), font, ExecuteTeleport, 18);

            // Close Button
            CreateButton(window.transform, "BtnClose", "✖ CERRAR [ESC]", new Vector2(0f, -180f), new Vector2(260f, 40f),
                new Color(0.60f, 0.12f, 0.16f, 0.95f), font, Close, 14);

            modalContainer.SetActive(false);
            UpdateDisplay();
        }

        public void Open()
        {
            if (modalContainer == null) BuildUI();
            isOpen = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            modalContainer.SetActive(true);
            modalContainer.transform.SetAsLastSibling();
            UpdateDisplay();
        }

        public void Close()
        {
            isOpen = false;
            Time.timeScale = previousTimeScale > 0.001f ? previousTimeScale : 1f;
            if (modalContainer != null) modalContainer.SetActive(false);
        }

        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        public void AddDistance(float delta)
        {
            selectedDistance = Mathf.Max(0f, selectedDistance + delta);
            UpdateDisplay();
        }

        public void SetDistance(float distance)
        {
            selectedDistance = Mathf.Max(0f, distance);
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (distanceDisplay != null)
                distanceDisplay.text = $"{selectedDistance:N0} m";

            if (phaseDescriptionText != null)
            {
                if (selectedDistance < 10000f)
                    phaseDescriptionText.text = "Zona Normal: Física y texturas 100% estables";
                else if (selectedDistance < 32768f)
                    phaseDescriptionText.text = "Zona Intermedia: Terreno lejano";
                else if (selectedDistance < 65536f)
                    phaseDescriptionText.text = "Fase 1 (32K): Inicio de Glitch, Jitter de cámara y hundimiento leve (3cm)";
                else if (selectedDistance < 131072f)
                    phaseDescriptionText.text = "Fase 2 (65K): Hundimiento moderado (8cm), arrastre y vibración 60Hz";
                else
                    phaseDescriptionText.text = "Fase 3 (131K+): Hundimiento profundo (16cm) y distorsión de texturas UV";
            }
        }

        public void ExecuteTeleport()
        {
            float target = selectedDistance;
            Close();

            TerrainManager terrain = FindFirstObjectByType<TerrainManager>();
            if (terrain != null)
                terrain.TeleportTo(target);

            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
                gm.TeleportTo(target);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.tKey.wasPressedThisFrame)
            {
                Toggle();
                return;
            }

            if (!isOpen) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            {
                ExecuteTeleport();
                return;
            }

            // Keyboard adjustments:
            // Left/Right: -500m / +500m
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                AddDistance(500f);
            else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                AddDistance(-500f);

            // Up/Down: +5,000m / -5,000m
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                AddDistance(5000f);
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                AddDistance(-5000f);

            // Number keys 1-6 for presets
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) SetDistance(0f);
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) SetDistance(1000f);
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) SetDistance(10000f);
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) SetDistance(32768f);
            if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) SetDistance(65536f);
            if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) SetDistance(131072f);
        }

        private static Text CreateText(Transform parent, string name, string content, int size, FontStyle style,
            Vector2 position, Vector2 dimensions, Color color, Font font)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;

            Text text = obj.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position,
            Vector2 dimensions, Color bgColor, Font font, UnityEngine.Events.UnityAction onClick, int fontSize = 14)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;

            Image img = obj.GetComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true;

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.3f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(obj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            Text txt = textObj.GetComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = label;
            txt.raycastTarget = false;

            Button btn = obj.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.35f;
            colors.pressedColor = bgColor * 0.65f;
            colors.selectedColor = bgColor * 1.35f;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            return btn;
        }
    }
}
