using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingResultsUI : MonoBehaviour
    {
        [SerializeField] private RaceManager raceManager;

        private RaceResults results;
        private bool isVisible;
        private Vector2 scrollPos;

        private void Start()
        {
            if (raceManager == null) raceManager = FindFirstObjectByType<RaceManager>();
            if (raceManager != null)
            {
                raceManager.OnRaceCompleted += ShowResults;
            }
        }

        private void OnDestroy()
        {
            if (raceManager != null)
            {
                raceManager.OnRaceCompleted -= ShowResults;
            }
        }

        public void ShowResults(RaceResults raceResults)
        {
            results = raceResults;
            isVisible = true;
        }

        private void OnGUI()
        {
            if (!isVisible || results == null) return;

            float panelWidth = Mathf.Min(780f, Screen.width - 40f);
            float panelHeight = Mathf.Min(560f, Screen.height - 40f);
            Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);

            GUI.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.96f);
            GUI.Box(panelRect, "");

            GUILayout.BeginArea(panelRect);
            GUILayout.Space(12);

            // Title
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUILayout.Label("RACE RESULTS — RANKED OFFLINE", headerStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            subStyle.normal.textColor = Color.gray;
            GUILayout.Label($"{results.trackDisplayName}  •  TOTAL TIME: {RacerResultEntry.FormatTime(results.totalRaceTime)}", subStyle);
            GUILayout.Space(8);

            // Table Headers
            GUILayout.BeginHorizontal();
            GUILayout.Label("POS", GUILayout.Width(45));
            GUILayout.Label("RACER", GUILayout.Width(160));
            GUILayout.Label("VEHICLE", GUILayout.Width(120));
            GUILayout.Label("TIME", GUILayout.Width(100));
            GUILayout.Label("BEST LAP", GUILayout.Width(100));
            GUILayout.Label("MMR CHANGE", GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.Box("", GUILayout.Height(2));

            // Table Rows
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(330));
            if (results.entries != null)
            {
                for (int i = 0; i < results.entries.Length; i++)
                {
                    RacerResultEntry e = results.entries[i];
                    Color rowColor = e.isHuman ? new Color(0.2f, 0.9f, 1.0f) : Color.white;
                    GUIStyle rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
                    rowStyle.normal.textColor = rowColor;

                    GUILayout.BeginHorizontal();
                    string posText = $"#{e.finishPosition:00}";
                    GUILayout.Label(posText, rowStyle, GUILayout.Width(45));

                    string nameText = e.displayName + (e.isHuman ? " [YOU]" : "");
                    GUILayout.Label(nameText, rowStyle, GUILayout.Width(160));
                    GUILayout.Label(e.vehicleId, rowStyle, GUILayout.Width(120));
                    GUILayout.Label(RacerResultEntry.FormatTime(e.totalTime), rowStyle, GUILayout.Width(100));
                    GUILayout.Label(RacerResultEntry.FormatTime(e.bestLapTime), rowStyle, GUILayout.Width(100));

                    string sign = e.mmrDelta >= 0 ? "+" : "";
                    string deltaStr = $"{e.oldMmr} -> {e.newMmr} ({sign}{e.mmrDelta})";
                    GUIStyle mmrStyle = new GUIStyle(rowStyle);
                    mmrStyle.normal.textColor = e.mmrDelta > 0 ? Color.green : (e.mmrDelta < 0 ? new Color(1f, 0.35f, 0.35f) : Color.yellow);
                    GUILayout.Label(deltaStr, mmrStyle, GUILayout.ExpandWidth(true));

                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Action Buttons
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
            if (GUILayout.Button("RETRY RACE", GUILayout.Width(180), GUILayout.Height(40)))
            {
                if (raceManager != null) raceManager.RetryRace();
                else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            GUILayout.Space(20);

            GUI.backgroundColor = new Color(0.4f, 0.4f, 0.45f);
            if (GUILayout.Button("MAIN MENU", GUILayout.Width(180), GUILayout.Height(40)))
            {
                if (raceManager != null) raceManager.ExitToMenu();
                else SceneManager.LoadScene("PreStart");
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
    }
}
