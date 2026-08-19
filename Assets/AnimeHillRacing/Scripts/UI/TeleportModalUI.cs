using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.Vehicle;
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
        private Slider distanceSlider;
        private Text resetFuelBtnText;
        private Text unlimitedFuelBtnText;
        private Image unlimitedFuelBtnImage;
        private float selectedDistance = 32768f;
        private bool isOpen;
        private float previousTimeScale = 1f;
        private bool isUpdatingSliderInternally;

        public static bool IsUnlimitedFuel { get; private set; }

        public bool IsOpen => isOpen;

        public static TeleportModalUI Create(Transform parent)
        {
            Canvas rootCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            Transform targetParent = rootCanvas != null ? rootCanvas.transform : parent;

            GameObject go = new GameObject("Teleport Modal Controller", typeof(RectTransform));
            go.transform.SetParent(targetParent, false);
            RectTransform rootRect = go.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Dedicated Canvas + GraphicRaycaster with highest sortingOrder to ensure it is always on top and fully clickable
            Canvas modalCanvas = go.AddComponent<Canvas>();
            modalCanvas.overrideSorting = true;
            modalCanvas.sortingOrder = 100;
            go.AddComponent<GraphicRaycaster>();

            TeleportModalUI modal = go.AddComponent<TeleportModalUI>();
            modal.BuildUI();
            return modal;
        }

        private void BuildUI()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Fullscreen Dimmed Background Blocker
            modalContainer = new GameObject("Modal Container", typeof(RectTransform), typeof(Image));
            modalContainer.transform.SetParent(transform, false);
            RectTransform blockerRect = modalContainer.GetComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            Image blockerImage = modalContainer.GetComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0.05f, 0.72f);
            blockerImage.raycastTarget = true;

            // Window Card (Glassmorphic Cyber-Neon, shifted to the right so player/car remains visible on the left)
            GameObject window = new GameObject("Window", typeof(RectTransform), typeof(Image));
            window.transform.SetParent(modalContainer.transform, false);
            RectTransform winRect = window.GetComponent<RectTransform>();
            winRect.anchorMin = new Vector2(0.5f, 0.5f);
            winRect.anchorMax = new Vector2(0.5f, 0.5f);
            winRect.pivot = new Vector2(0.5f, 0.5f);
            winRect.anchoredPosition = new Vector2(250f, 0f);
            winRect.sizeDelta = new Vector2(520f, 630f);

            Image winImg = window.GetComponent<Image>();
            winImg.color = new Color(0.06f, 0.08f, 0.16f, 0.98f);
            winImg.raycastTarget = true;

            Outline winOutline = window.AddComponent<Outline>();
            winOutline.effectColor = new Color(0.2f, 0.85f, 1f, 0.95f);
            winOutline.effectDistance = new Vector2(3f, -3f);

            // Header Title
            CreateText(window.transform, "Title", "🚀 MENU DE TELETRANSPORTE", 22, FontStyle.Bold,
                new Vector2(0f, 278f), new Vector2(480f, 38f), new Color(0.3f, 0.9f, 1f, 1f), font);

            // Target Distance Card
            GameObject displayCard = new GameObject("Display Card", typeof(RectTransform), typeof(Image));
            displayCard.transform.SetParent(window.transform, false);
            RectTransform cardRect = displayCard.GetComponent<RectTransform>();
            cardRect.anchoredPosition = new Vector2(0f, 214f);
            cardRect.sizeDelta = new Vector2(460f, 70f);
            Image cardImg = displayCard.GetComponent<Image>();
            cardImg.color = new Color(0.02f, 0.03f, 0.08f, 0.98f);
            cardImg.raycastTarget = false;

            Outline cardOutline = displayCard.AddComponent<Outline>();
            cardOutline.effectColor = new Color(1f, 0.85f, 0.2f, 0.9f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            distanceDisplay = CreateText(displayCard.transform, "Distance Text", "32,768 m", 28, FontStyle.Bold,
                new Vector2(0f, 12f), new Vector2(440f, 36f), new Color(1f, 0.94f, 0.25f, 1f), font);

            phaseDescriptionText = CreateText(displayCard.transform, "Phase Text", "Fase 1 (32K): Pre-Glitch & Hundimiento leve (3cm)", 11, FontStyle.Italic,
                new Vector2(0f, -18f), new Vector2(440f, 22f), new Color(0.7f, 0.88f, 1f, 0.95f), font);

            // Touch Slider for smooth finger dragging from 0m to 150,000m
            CreateTouchSlider(window.transform, new Vector2(0f, 154f), new Vector2(450f, 28f), font);

            // Tactile Fine Adjustment Buttons (+500m / -500m / +5000m / -5000m)
            CreateButton(window.transform, "BtnMinus5000", "-5,000m", new Vector2(-170f, 108f), new Vector2(104f, 40f),
                new Color(0.18f, 0.22f, 0.40f, 0.95f), font, () => AddDistance(-5000f), 15);
            CreateButton(window.transform, "BtnMinus500", "-500m", new Vector2(-58f, 108f), new Vector2(104f, 40f),
                new Color(0.24f, 0.32f, 0.58f, 0.95f), font, () => AddDistance(-500f), 16);
            CreateButton(window.transform, "BtnPlus500", "+500m", new Vector2(58f, 108f), new Vector2(104f, 40f),
                new Color(0.14f, 0.56f, 0.40f, 0.98f), font, () => AddDistance(500f), 16);
            CreateButton(window.transform, "BtnPlus5000", "+5,000m", new Vector2(170f, 108f), new Vector2(104f, 40f),
                new Color(0.16f, 0.68f, 0.48f, 0.98f), font, () => AddDistance(5000f), 15);

            // Section Label - Presets
            CreateText(window.transform, "PresetLabel", "PREAJUSTES DIRECTOS (TOCA PARA ELEGIR):", 12, FontStyle.Bold,
                new Vector2(0f, 66f), new Vector2(460f, 20f), new Color(0.85f, 0.9f, 1f, 0.95f), font);

            // Quick Preset Grid - Row 1
            float[] presetRow1 = { 0f, 1000f, 10000f };
            string[] labelsRow1 = { "0 m", "1,000 m", "10,000 m" };
            for (int i = 0; i < 3; i++)
            {
                float d = presetRow1[i];
                CreateButton(window.transform, $"Preset_{d}", labelsRow1[i],
                    new Vector2(-155f + i * 155f, 32f), new Vector2(146f, 36f),
                    new Color(0.14f, 0.18f, 0.34f, 0.95f), font, () => SetDistance(d), 14);
            }

            // Quick Preset Grid - Row 2 (Glitch Phases)
            float[] presetRow2 = { 32768f, 65536f, 131072f };
            string[] labelsRow2 = { "32,768 m (32K)", "65,536 m (65K)", "131,072 m (131K)" };
            for (int i = 0; i < 3; i++)
            {
                float d = presetRow2[i];
                CreateButton(window.transform, $"Preset_{d}", labelsRow2[i],
                    new Vector2(-155f + i * 155f, -10f), new Vector2(146f, 36f),
                    new Color(0.32f, 0.18f, 0.48f, 0.98f), font, () => SetDistance(d), 13);
            }

            // Section Label - Fuel Management
            CreateText(window.transform, "FuelLabel", "⛽ GESTIÓN DE COMBUSTIBLE (FUEL):", 12, FontStyle.Bold,
                new Vector2(0f, -52f), new Vector2(460f, 20f), new Color(1f, 0.85f, 0.3f, 0.95f), font);

            // Fuel Management Buttons Row (Reset Fuel & Limited/Unlimited Fuel Toggle)
            CreateButton(window.transform, "BtnResetFuel", "⛽ RESET FUEL (100%)", new Vector2(-118f, -92f), new Vector2(224f, 44f),
                new Color(0.16f, 0.48f, 0.36f, 0.98f), font, ResetFuel, 13, out resetFuelBtnText, out _);

            CreateButton(window.transform, "BtnUnlimitedFuel", "♾️ FUEL: ILIMITADO", new Vector2(118f, -92f), new Vector2(224f, 44f),
                new Color(0.24f, 0.28f, 0.42f, 0.95f), font, ToggleUnlimitedFuel, 13, out unlimitedFuelBtnText, out unlimitedFuelBtnImage);

            // Big Tactile Teleport Action Button
            CreateButton(window.transform, "BtnTeleport", "🚀 ¡TELETRANSPORTAR AHORA!", new Vector2(0f, -160f), new Vector2(460f, 54f),
                new Color(0.08f, 0.72f, 0.36f, 1f), font, ExecuteTeleport, 19);

            // Big Tactile Close Button
            CreateButton(window.transform, "BtnClose", "✖ CERRAR", new Vector2(0f, -228f), new Vector2(280f, 42f),
                new Color(0.65f, 0.12f, 0.16f, 0.98f), font, Close, 16);

            modalContainer.SetActive(false);
            UpdateDisplay();
            UpdateFuelUI();
        }

        private void CreateTouchSlider(Transform parent, Vector2 position, Vector2 size, Font font)
        {
            GameObject sliderObj = new GameObject("Distance Slider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(parent, false);
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchoredPosition = position;
            sliderRect.sizeDelta = size;

            distanceSlider = sliderObj.GetComponent<Slider>();
            distanceSlider.minValue = 0f;
            distanceSlider.maxValue = 150000f;
            distanceSlider.wholeNumbers = true;
            distanceSlider.value = selectedDistance;

            // Background track
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.12f, 0.15f, 0.25f, 1f);
            bgImg.raycastTarget = true;

            // Fill area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-5f, 0f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0.2f, 0.85f, 1f, 0.95f);
            fillImg.raycastTarget = false;

            // Handle slide area
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(15f, 0f);
            handleAreaRect.offsetMax = new Vector2(-15f, 0f);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(30f, 30f);
            Image handleImg = handle.GetComponent<Image>();
            handleImg.color = new Color(1f, 0.92f, 0.2f, 1f);
            handleImg.raycastTarget = true;

            Outline handleOutline = handle.AddComponent<Outline>();
            handleOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            handleOutline.effectDistance = new Vector2(1.5f, -1.5f);

            distanceSlider.fillRect = fillRect;
            distanceSlider.handleRect = handleRect;
            distanceSlider.targetGraphic = handleImg;

            distanceSlider.onValueChanged.AddListener((val) =>
            {
                if (!isUpdatingSliderInternally)
                {
                    selectedDistance = Mathf.Round(val / 50f) * 50f;
                    UpdateDisplay(false);
                }
            });
        }

        public void Open()
        {
            if (modalContainer == null) BuildUI();
            isOpen = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            modalContainer.SetActive(true);
            transform.SetAsLastSibling();
            modalContainer.transform.SetAsLastSibling();
            UpdateDisplay();
            UpdateFuelUI();
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

        public void ResetFuel()
        {
            VehicleRuntimeBase vehicle = GetActiveVehicle();
            GameManager gm = FindFirstObjectByType<GameManager>();

            if (vehicle != null && vehicle.Fuel != null)
            {
                vehicle.Fuel.RefillFull();
                if (IsUnlimitedFuel)
                {
                    vehicle.Fuel.SetConsumptionEnabled(false);
                }
                FindFirstObjectByType<GameplayHUD>()?.RefreshImmediate(gm);
            }

            if (resetFuelBtnText != null)
            {
                resetFuelBtnText.text = "✅ ¡FUEL AL 100%!";
                CancelInvoke(nameof(RestoreResetFuelLabel));
                Invoke(nameof(RestoreResetFuelLabel), 1.5f);
            }
        }

        private void RestoreResetFuelLabel()
        {
            if (resetFuelBtnText != null)
                resetFuelBtnText.text = "⛽ RESET FUEL (100%)";
        }

        public void ToggleUnlimitedFuel()
        {
            SetUnlimitedFuel(!IsUnlimitedFuel);
        }

        public void SetUnlimitedFuel(bool enabled)
        {
            IsUnlimitedFuel = enabled;
            ApplyFuelSetting();
            UpdateFuelUI();
        }

        private void ApplyFuelSetting()
        {
            VehicleRuntimeBase vehicle = GetActiveVehicle();
            if (vehicle != null && vehicle.Fuel != null)
            {
                vehicle.Fuel.SetConsumptionEnabled(!IsUnlimitedFuel);
                if (IsUnlimitedFuel)
                {
                    vehicle.Fuel.RefillFull();
                }
                GameManager gm = FindFirstObjectByType<GameManager>();
                FindFirstObjectByType<GameplayHUD>()?.RefreshImmediate(gm);
            }
        }

        private void UpdateFuelUI()
        {
            if (unlimitedFuelBtnText != null && unlimitedFuelBtnImage != null)
            {
                if (IsUnlimitedFuel)
                {
                    unlimitedFuelBtnText.text = "♾️ FUEL: ILIMITADO (ON)";
                    unlimitedFuelBtnText.color = new Color(1f, 0.95f, 0.3f, 1f);
                    unlimitedFuelBtnImage.color = new Color(0.12f, 0.55f, 0.42f, 1f);
                }
                else
                {
                    unlimitedFuelBtnText.text = "⛽ FUEL: LIMITADO (OFF)";
                    unlimitedFuelBtnText.color = Color.white;
                    unlimitedFuelBtnImage.color = new Color(0.24f, 0.28f, 0.42f, 0.95f);
                }
            }
        }

        private VehicleRuntimeBase GetActiveVehicle()
        {
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null && gm.Vehicle != null)
                return gm.Vehicle;
            return FindFirstObjectByType<VehicleRuntimeBase>();
        }

        private void UpdateDisplay(bool syncSlider = true)
        {
            if (distanceDisplay != null)
                distanceDisplay.text = $"{selectedDistance:N0} m";

            if (syncSlider && distanceSlider != null)
            {
                isUpdatingSliderInternally = true;
                distanceSlider.value = selectedDistance;
                isUpdatingSliderInternally = false;
            }

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

            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.TeleportTo(target);
            }
            else
            {
                TerrainManager terrain = FindFirstObjectByType<TerrainManager>();
                if (terrain != null)
                    terrain.TeleportTo(target);
            }

            ApplyFuelSetting();
        }

        private void Update()
        {
            // Keep unlimited fuel active during gameplay if selected
            if (IsUnlimitedFuel && !isOpen)
            {
                VehicleRuntimeBase vehicle = GetActiveVehicle();
                if (vehicle != null && vehicle.Fuel != null)
                {
                    if (vehicle.Fuel.CanConsume)
                        vehicle.Fuel.SetConsumptionEnabled(false);
                    if (vehicle.Fuel.FuelNormalized < 0.99f)
                        vehicle.Fuel.RefillFull();
                }
            }

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

            // Keyboard shortcut to reset fuel (F) or toggle unlimited fuel (U)
            if (keyboard.fKey.wasPressedThisFrame)
            {
                ResetFuel();
                return;
            }

            if (keyboard.uKey.wasPressedThisFrame)
            {
                ToggleUnlimitedFuel();
                return;
            }

            // Keyboard adjustments:
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                AddDistance(500f);
            else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                AddDistance(-500f);

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
            return CreateButton(parent, name, label, position, dimensions, bgColor, font, onClick, fontSize, out _, out _);
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position,
            Vector2 dimensions, Color bgColor, Font font, UnityEngine.Events.UnityAction onClick, int fontSize,
            out Text labelText, out Image bgImage)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;

            Image img = obj.GetComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true;
            bgImage = img;

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.35f);
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
            labelText = txt;

            Button btn = obj.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.35f;
            colors.pressedColor = bgColor * 0.65f;
            colors.selectedColor = bgColor * 1.35f;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            // Add tactile touch scale bounce feedback
            obj.AddComponent<ButtonPressFeedback>();
            return btn;
        }
    }
}
