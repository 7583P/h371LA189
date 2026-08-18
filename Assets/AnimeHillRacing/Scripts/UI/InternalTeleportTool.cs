using AnimeHillRacing.Core;
using AnimeHillRacing.Road;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AnimeHillRacing.UI
{
    /// <summary>
    /// Self-contained internal testing tool for teleporting vehicles to any distance
    /// (e.g. 1,000m, 32,768m float boundary, 50,000m), refilling fuel, and testing physics stability.
    /// Auto-loads into all gameplay scenes. Press 'T' or tap the on-screen button to open.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InternalTeleportTool : MonoBehaviour
    {
        private static InternalTeleportTool s_instance;

        private bool isOpen;
        private string customDistanceInput = "32768";
        private bool unlimitedFuel;
        private int selectedTimeScaleIndex;
        private readonly float[] timeScaleOptions = { 1f, 2f, 4f, 8f };
        private readonly string[] timeScaleLabels = { "1x", "2x", "4x", "8x" };

        private Rect windowRect = new Rect(20, 20, 340, 490);
        private GUIStyle windowStyle;
        private GUIStyle buttonStyle;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        private GUIStyle textFieldStyle;
        private bool stylesInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (s_instance != null) return;
            var host = new GameObject("[Internal Teleport Tool]");
            DontDestroyOnLoad(host);
            s_instance = host.AddComponent<InternalTeleportTool>();
        }

        private void Update()
        {
            // Toggle with 'T' key on keyboard (New Input System compatible)
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tKey.wasPressedThisFrame)
            {
                isOpen = !isOpen;
            }

            if (unlimitedFuel)
            {
                var vehicle = FindActiveVehicle();
                if (vehicle != null)
                {
                    var fuel = vehicle.GetComponent<FuelSystem>();
                    if (fuel != null && fuel.NormalizedFuel < 0.95f)
                    {
                        fuel.RefillFull();
                    }
                }
            }
        }

        private void OnGUI()
        {
            InitStyles();

            // Floating toggle button in the top-right corner
            float btnWidth = 160f;
            float btnHeight = 44f;
            Rect toggleRect = new Rect(Screen.width - btnWidth - 15f, 15f, btnWidth, btnHeight);

            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = isOpen ? new Color(0.9f, 0.2f, 0.3f, 0.95f) : new Color(0.15f, 0.55f, 0.95f, 0.90f);
            if (GUI.Button(toggleRect, isOpen ? "✕ Close Teleport" : "🚀 Teleport Tool", buttonStyle))
            {
                isOpen = !isOpen;
            }
            GUI.backgroundColor = prevColor;

            if (!isOpen) return;

            // Constrain window inside screen
            windowRect.x = Mathf.Clamp(windowRect.x, 10f, Screen.width - windowRect.width - 10f);
            windowRect.y = Mathf.Clamp(windowRect.y, 10f, Screen.height - windowRect.height - 10f);

            windowRect = GUI.Window(999888, windowRect, DrawWindowContent, "⚡ INTERNAL TELEPORT TOOL", windowStyle);
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;
            stylesInitialized = true;

            windowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                fixedHeight = 34f
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = true
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 32f
            };
        }

        private void DrawWindowContent(int windowId)
        {
            var vehicle = FindActiveVehicle();
            float currentDist = vehicle != null ? vehicle.LogicalDistance : 0f;
            float currentSpeed = vehicle != null ? vehicle.SpeedKmh : 0f;
            var fuel = vehicle != null ? vehicle.GetComponent<FuelSystem>() : null;
            float fuelPct = fuel != null ? fuel.NormalizedFuel * 100f : 100f;

            GUILayout.Space(6f);
            GUILayout.Label($"<b>Status:</b> {(vehicle != null ? "<color=#55FF55>Vehicle Active</color>" : "<color=#FF5555>No Vehicle in Scene</color>")}", labelStyle);
            GUILayout.Label($"<b>Distance:</b> {currentDist:F1} m   |   <b>Speed:</b> {currentSpeed:F0} km/h", labelStyle);
            GUILayout.Label($"<b>Fuel:</b> {fuelPct:F0}%", labelStyle);

            GUILayout.Space(8f);
            GUILayout.Label("── <b>QUICK PRESETS</b> ──", headerStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+500 m", buttonStyle)) TeleportRelatively(500f);
            if (GUILayout.Button("+1,000 m", buttonStyle)) TeleportRelatively(1000f);
            if (GUILayout.Button("+5,000 m", buttonStyle)) TeleportRelatively(5000f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1,000 m", buttonStyle)) TeleportTo(1000f);
            if (GUILayout.Button("5,000 m", buttonStyle)) TeleportTo(5000f);
            if (GUILayout.Button("10,000 m", buttonStyle)) TeleportTo(10000f);
            GUILayout.EndHorizontal();

            GUI.backgroundColor = new Color(1f, 0.85f, 0.2f, 1f);
            if (GUILayout.Button("⭐ 32,768 m (Float Boundary)", buttonStyle))
            {
                TeleportTo(32768f);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("50,000 m", buttonStyle)) TeleportTo(50000f);
            if (GUILayout.Button("100,000 m", buttonStyle)) TeleportTo(100000f);
            if (GUILayout.Button("524,288 m", buttonStyle)) TeleportTo(524288f);
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("── <b>CUSTOM DISTANCE (METERS)</b> ──", headerStyle);

            GUILayout.BeginHorizontal();
            customDistanceInput = GUILayout.TextField(customDistanceInput, textFieldStyle, GUILayout.Width(190f));
            if (GUILayout.Button("Go! 🚀", buttonStyle))
            {
                if (float.TryParse(customDistanceInput, out float parsed))
                {
                    TeleportTo(parsed);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("── <b>UTILITIES</b> ──", headerStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("⛽ Refill Fuel", buttonStyle))
            {
                fuel?.RefillFull();
            }

            GUI.backgroundColor = unlimitedFuel ? new Color(0.4f, 0.9f, 0.4f) : Color.white;
            if (GUILayout.Button(unlimitedFuel ? "🛡️ Infinite Fuel: ON" : "🛡️ Infinite Fuel: OFF", buttonStyle))
            {
                unlimitedFuel = !unlimitedFuel;
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 Respawn at 0 m", buttonStyle))
            {
                TeleportTo(0f);
            }

            // Game Speed Multiplier
            int newTimeScaleIndex = GUILayout.Toolbar(selectedTimeScaleIndex, timeScaleLabels, buttonStyle);
            if (newTimeScaleIndex != selectedTimeScaleIndex)
            {
                selectedTimeScaleIndex = newTimeScaleIndex;
                Time.timeScale = timeScaleOptions[selectedTimeScaleIndex];
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 25));
        }

        public static void TeleportRelatively(float deltaMeters)
        {
            var vehicle = FindActiveVehicle();
            float current = vehicle != null ? vehicle.ProgressPosition : 0f;
            TeleportTo(Mathf.Max(0f, current + deltaMeters));
        }

        public static void TeleportTo(float targetDistance)
        {
            var vehicle = FindActiveVehicle();
            if (vehicle == null)
            {
                Debug.LogWarning("[TeleportTool] No active vehicle found in the scene to teleport.");
                return;
            }

            float surfaceY = EstimateSurfaceHeight(targetDistance);
            Vector3 newChassisPos = new Vector3(targetDistance, surfaceY + 2.5f, vehicle.transform.position.z);

            // 1. Move vehicle root and reset rotation
            vehicle.transform.position = newChassisPos;
            vehicle.transform.rotation = Quaternion.identity;

            // 2. Reset Chassis Rigidbody
            var rb = vehicle.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = newChassisPos;
                rb.rotation = Quaternion.identity;
            }

            // 3. Reset all child wheel rigidbodies and suspension
            var suspensions = vehicle.GetComponentsInChildren<SuspensionController>(true);
            foreach (var suspension in suspensions)
            {
                if (suspension != null && suspension.WheelBody != null)
                {
                    suspension.WheelBody.linearVelocity = Vector3.zero;
                    suspension.WheelBody.angularVelocity = Vector3.zero;
                }
            }

            // 4. Fully refill fuel to prevent immediate game over
            var fuel = vehicle.GetComponent<FuelSystem>();
            if (fuel != null)
            {
                fuel.RefillFull();
            }

            // 5. Instantly align camera
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 camPos = cam.transform.position;
                camPos.x = targetDistance;
                camPos.y = surfaceY + 3.5f;
                cam.transform.position = camPos;
            }

            Debug.Log($"[TeleportTool] Teleported vehicle to {targetDistance:F1}m (Height: {surfaceY:F1}m).");
        }

        private static float EstimateSurfaceHeight(float worldX)
        {
            // Try 2D/2.5D Terrain Generator
            var terrainManager = FindFirstObjectByType<TerrainManager>();
            if (terrainManager != null && terrainManager.ActiveProfile != null)
            {
                return TerrainGenerator.SampleHeight(terrainManager.ActiveProfile, worldX);
            }

            // Try 3D Road Generator
            var roadManager = FindFirstObjectByType<RoadTrackGenerator>();
            if (roadManager != null)
            {
                return 0f;
            }

            // Physics Raycast fallback
            if (Physics.Raycast(new Vector3(worldX, 300f, 0f), Vector3.down, out RaycastHit hit, 600f))
            {
                return hit.point.y;
            }

            return 0f;
        }

        private static VehicleRuntimeBase FindActiveVehicle()
        {
            var vehicle = FindFirstObjectByType<VehicleRuntimeBase>();
            if (vehicle != null) return vehicle;

            var controller = FindFirstObjectByType<VehicleController>();
            if (controller != null) return controller;

            var roadController = FindFirstObjectByType<RoadVehicleController>();
            if (roadController != null) return roadController;

            return null;
        }
    }
}
