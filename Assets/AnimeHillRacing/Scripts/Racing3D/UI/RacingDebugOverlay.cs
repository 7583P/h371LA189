using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool showDebugOverlay;

        private RaceManager raceManager;
        private Vector2 scrollPos;

        private void Start()
        {
            raceManager = FindFirstObjectByType<RaceManager>();
        }

        private void Update()
        {
            if (!Debug.isDebugBuild) return;
            var kb = Keyboard.current;
            if (kb != null && kb.f3Key.wasPressedThisFrame)
            {
                showDebugOverlay = !showDebugOverlay;
            }
        }

        public void ToggleDebug() => showDebugOverlay = !showDebugOverlay;

        private void OnGUI()
        {
            if (!Debug.isDebugBuild || !showDebugOverlay) return;

            float w = Mathf.Min(680f, Screen.width - 20f);
            float h = Mathf.Min(480f, Screen.height - 20f);
            Rect rect = new Rect(10, 10, w, h);

            GUI.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.92f);
            GUI.Box(rect, "");

            GUILayout.BeginArea(rect);
            GUILayout.Space(6);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = Color.cyan;
            GUILayout.Label("RACING 3D â€” AI OBSERVABILITY & DIAGNOSTICS [F3]", titleStyle);

            if (raceManager != null && raceManager.PlayerVehicle != null)
            {
                var p = raceManager.PlayerVehicle;
                GUILayout.Label($"PLAYER: Speed: {p.SpeedKmh:F1} km/h | Drift: {p.IsDrifting} ({p.CurrentMiniTurboStage}) | Grounded: {p.IsGrounded} | Boost: {p.IsBoosting}");
            }

            var metrics = FindFirstObjectByType<RacingRaceMetrics>();
            if (metrics != null) GUILayout.Label($"Frame {metrics.MeanFrameMs:F2} ms / CPU {metrics.CpuMs:F2} ms / GC {metrics.GcBytes:N0} B / State {raceManager?.State}");
            GUILayout.Space(6);
            GUILayout.Label("AI RACERS STATUS (11 BOTS):", titleStyle);

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(350));
            if (raceManager != null)
            {
                var spawned = raceManager.SpawnedVehicles;
                for (int i = 0; i < spawned.Count; i++)
                {
                    var v = spawned[i];
                    if (v == null) continue;
                    var ai = v.GetComponent<AiVehicleController>();
                    if (ai == null || ai.Profile == null) continue;

                    var log = ai.Logger;
                    string summary = log != null ? log.GetDebugSummary() : "No log";
                    var tier = MmrTierUtility.GetTier(ai.Profile.mmr);

                    GUIStyle botStyle = new GUIStyle(GUI.skin.box);
                    botStyle.alignment = TextAnchor.UpperLeft;
                    botStyle.normal.textColor = Color.white;

                    string botHeader = $"<b>{log?.BotName}</b> (MMR: {ai.Profile.mmr} [{tier}]) | Speed: {v.SpeedKmh:F0} km/h";
                    var ml=v.GetComponent<ML.RacingMlInferenceAdapter>();
                    string model=ml!=null?$"{ml.EffectiveMode} {ml.ModelVersion} / {ml.AvgInferenceTimeMicroseconds:F1} us":"Off";
                    var cmd=v.LastCommands;
                    string proposals=ml!=null&&ml.ModelAvailable&&ml.EffectiveMode!=ML.MlExecutionMode.Off
                        ?$"Heuristic S/T/B {cmd.Steering:F2}/{cmd.Throttle:F2}/{cmd.Brake:F2} J/D/A {cmd.Jump}/{cmd.Drift}/{cmd.AerialBoost}\nML S/T/B {ml.MlProposedSteer:F2}/{ml.MlProposedThrottle:F2}/{ml.MlProposedBrake:F2} p(J/D/A) {ml.MlJumpProbability:F2}/{ml.MlDriftProbability:F2}/{(ml.HasAerialOutput?ml.MlAerialBoostProbability.ToString("F2"):"n/a")}\n{ml.TargetAlignment} / interval {ml.LastPredictionHorizonSeconds:F2}s / {ml.ShadowComparisonTicks} comparisons / confidence uncalibrated"
                        :"ML proposals unavailable";
                    GUILayout.Box($"{botHeader}\nSkill {ai.Profile.NormalizedSkill:F3} / {ai.Profile.style} / plan {ai.PlannedActionDepth}/{ai.Profile.PlanningDepth} / {ai.CurrentIntent}\n{ai.PlanSummary}\nLanding {ai.TargetLanding} / ML {model}\n{proposals}\n{summary}", botStyle);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void OnDrawGizmos()
        {
            if (!Debug.isDebugBuild || !showDebugOverlay) return;

            // Draw waypoint connections and optimal lines
            TrackSplineGraph graph = FindFirstObjectByType<TrackSplineGraph>();
            if (graph == null || graph.NodeCount < 2) return;

            for (int i = 0; i < graph.NodeCount; i++)
            {
                TrackNode nA = graph.GetNode(i);
                TrackNode nB = graph.GetNode(i + 1);

                // Center line (white)
                Gizmos.color = Color.white;
                Gizmos.DrawLine(nA.Position, nB.Position);

                // Optimal line (green)
                Vector3 optA = nA.Position + nA.Right * (nA.OptimalLineOffset * nA.TrackWidth * 0.45f);
                Vector3 optB = nB.Position + nB.Right * (nB.OptimalLineOffset * nB.TrackWidth * 0.45f);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(optA, optB);

                if (nA.IsDriftZone)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(nA.Position, 2.5f);
                }
            }
        }
    }
}

