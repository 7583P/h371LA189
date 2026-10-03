using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingModeSelectUI : MonoBehaviour
    {
        [SerializeField] private bool openOnStart;

        private void OnGUI()
        {
            float panelWidth = Mathf.Min(560f, Screen.width - 40f);
            float panelHeight = Mathf.Min(480f, Screen.height - 40f);
            Rect rect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);

            GUI.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.96f);
            GUI.Box(rect, "");

            GUILayout.BeginArea(rect);
            GUILayout.Space(16);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUILayout.Label("HILL ADVENTURER — RACING 3D", titleStyle);

            int mmr = RacingSaveManager.CurrentMmr;
            var tier = MmrTierUtility.GetTier(mmr);
            GUIStyle subStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            subStyle.normal.textColor = Color.cyan;
            GUILayout.Label($"PILAR RACING  •  RANK: {MmrTierUtility.GetTierDisplayName(tier)} ({mmr} MMR)", subStyle);
            GUILayout.Space(18);

            // 1. Ranked Offline (Active)
            GUI.backgroundColor = new Color(0.1f, 0.65f, 1.0f);
            if (GUILayout.Button("RANKED OFFLINE (12 RACERS)\nConfigure Kart, Wheels & Compete for MMR", GUILayout.Height(50)))
            {
                SceneManager.LoadScene("RacingGarage");
            }
            GUILayout.Space(8);

            // Direct Race Launch
            GUI.backgroundColor = new Color(0.15f, 0.85f, 0.35f);
            if (GUILayout.Button("START RACE DIRECTLY (12 RACERS)\nJump Straight into 3D Technical Circuit", GUILayout.Height(50)))
            {
                SceneManager.LoadScene("Racing3D");
            }
            GUILayout.Space(8);

            // 2. Grand Prix (Coming Soon)
            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
            GUI.enabled = false;
            GUILayout.Button("GRAND PRIX  [COMING SOON]\nChampionship Cup Series", GUILayout.Height(40));
            GUILayout.Space(6);

            // 3. Time Trial (Foundation / Coming Soon)
            GUILayout.Button("TIME TRIAL  [COMING SOON]\nSolo Leaderboard Ghost Attacks", GUILayout.Height(45));
            GUILayout.Space(8);

            // 4. Practice (Coming Soon)
            GUILayout.Button("PRACTICE  [COMING SOON]\nTrack Exploration & Mastery", GUILayout.Height(45));
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            // Back to Adventure Mode
            GUI.backgroundColor = new Color(0.85f, 0.3f, 0.2f);
            if (GUILayout.Button("RETURN TO ADVENTURE", GUILayout.Height(44)))
            {
                SceneManager.LoadScene("PreStart");
            }
            GUILayout.Space(12);

            GUILayout.EndArea();
        }
    }
}
