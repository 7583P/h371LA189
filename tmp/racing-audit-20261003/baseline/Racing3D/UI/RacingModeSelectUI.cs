using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingModeSelectUI:MonoBehaviour
    {
        [SerializeField] bool openOnStart;
        void Start()
        {
            RacingFeedback.Ensure();gameObject.AddComponent<RacingQualityScope>();
            var root=RacingDesign.Canvas("Racing hub");root.gameObject.AddComponent<RacingScreenMotion>();
            RacingDesign.Panel(root,"Backdrop",0,0,1,1,RacingDesign.Background,0);
            var contour=RacingDesign.Area(root,"Topography",.46f,.06f,.54f,.88f).gameObject.AddComponent<RacingEmblem>();contour.Contours=true;contour.color=new Color(.18f,.4f,.45f,.22f);contour.raycastTarget=false;
            RacingDesign.Label(root,"HILL ADVENTURER",RacingType.Section,.05f,.86f,.48f,.08f,RacingDesign.Ink,display:true);
            RacingDesign.Label(root,"RACING",RacingType.Display,.05f,.75f,.52f,.12f,RacingDesign.Cyan,display:true);
            RacingDesign.Label(root,"THE CANYON IS CALLING.",RacingType.Caption,.05f,.70f,.46f,.04f,RacingDesign.Muted);
            int mmr=RacingSaveManager.CurrentMmr;
            var badge=RacingDesign.Area(root,"Rank badge",.82f,.83f,.1f,.12f).gameObject.AddComponent<RacingEmblem>();badge.Tier=(int)MmrTierUtility.GetTier(mmr);badge.color=RacingDesign.TierColor(mmr);
            RacingDesign.Label(root,mmr.ToString("N0")+" MMR",RacingType.Stat,.61f,.89f,.2f,.055f,RacingDesign.Ink,TextAnchor.MiddleRight);
            RacingDesign.Label(root,MmrTierUtility.GetTierDisplayName(MmrTierUtility.GetTier(mmr)),RacingType.Caption,.62f,.845f,.19f,.04f,RacingDesign.TierColor(mmr),TextAnchor.MiddleRight);
            var ranked=RacingDesign.Button(root,"RANKED",.05f,.34f,.45f,.31f,()=>SceneManager.LoadScene("RacingGarage"),RacingDesign.Surface);
            RacingDesign.Label(ranked.transform,"12 RACERS / 3 LAPS",RacingType.Caption,.08f,.15f,.75f,.14f,RacingDesign.Cyan);
            RacingDesign.Label(ranked.transform,"BUILD YOUR LINE. EARN YOUR RANK.",RacingType.Caption,.08f,.02f,.79f,.15f,RacingDesign.Muted);
            RacingDesign.Panel(ranked.transform,"Gold edge",0,0,.01f,1,RacingDesign.Gold,0);
            RacingDesign.Label(root,"CANYON RUSH",RacingType.Title,.55f,.67f,.4f,.07f,RacingDesign.Ink);
            var preview=RacingShowroom.Create(root,.53f,.31f,.43f,.35f,RacingSaveManager.SelectedVehicleId,RacingSaveManager.SelectedWheelId);
            RacingDesign.Label(root,RacingDesign.VehicleName(RacingSaveManager.SelectedVehicleId),RacingType.Section,.55f,.25f,.37f,.05f,RacingDesign.Ink);
            RacingDesign.Label(root,$"BEST {RacingSaveManager.HighestMmr:N0}  /  {RacingSaveManager.RacesCount} RACES  /  {RacingSaveManager.WinsCount} WINS",RacingType.Caption,.55f,.20f,.39f,.04f,RacingDesign.Muted);
            RacingDesign.Label(root,"RECENT FORM  "+RacingSaveManager.RecentPerformance,RacingType.Caption,.55f,.15f,.39f,.04f,RacingDesign.Muted);
            string[] modes={"GRAND PRIX","TIME TRIAL","PRACTICE"};
            for(int i=0;i<3;i++)
            {
                var card=RacingDesign.Button(root,modes[i],.05f+i*.155f,.17f,.145f,.125f,null,RacingDesign.Surface);card.interactable=false;
                RacingDesign.Label(card.transform,"IN DEVELOPMENT",RacingType.Caption,.08f,.02f,.86f,.22f,RacingDesign.Muted);
            }
            RacingDesign.Button(root,"MAIN MENU",.05f,.045f,.18f,.075f,()=>SceneManager.LoadScene("MainMenu"));
            RacingDesign.Button(root,"TEST MMR",.24f,.045f,.16f,.075f,()=>OpenMmrTestTool(root));
            RacingDesign.Button(root,"LOW",.58f,.04f,.1f,.065f,()=>RacingQualityScope.Select(RacingQuality.Low));
            RacingDesign.Button(root,"MEDIUM",.69f,.04f,.13f,.065f,()=>RacingQualityScope.Select(RacingQuality.Medium));
            RacingDesign.Button(root,"HIGH",.83f,.04f,.11f,.065f,()=>RacingQualityScope.Select(RacingQuality.High));
            RacingDesign.Focus(ranked);
        }

        private GameObject mmrToolPanel;
        private int testMmr = 1000;
        private int testSeed = 42;

        private void OpenMmrTestTool(RectTransform root)
        {
            if (mmrToolPanel != null) { Destroy(mmrToolPanel); mmrToolPanel = null; return; }
            testMmr = RacingSaveManager.CurrentMmr;

            var panel = RacingDesign.Area(root, "Test MMR Panel", .04f, .06f, .92f, .88f);
            mmrToolPanel = panel.gameObject;
            panel.gameObject.AddComponent<RacingScreenMotion>();
            RacingDesign.Panel(panel, "Modal backdrop", 0, 0, 1, 1, new Color(0.04f, 0.05f, 0.09f, 0.97f), 0);

            RefreshMmrToolView(panel);
        }

        private void RefreshMmrToolView(RectTransform panel)
        {
            foreach (Transform child in panel)
            {
                if (child.name != "Modal backdrop") Destroy(child.gameObject);
            }

            RacingDesign.Label(panel, "TEST MMR — LOBBY & AI PROGRESSION INSPECTOR", RacingType.Title, .03f, .92f, .75f, .06f, RacingDesign.Cyan, display: true);
            var closeBtn = RacingDesign.Button(panel, "CLOSE [X]", .84f, .92f, .13f, .055f, () => { Destroy(mmrToolPanel); mmrToolPanel = null; });

            // MMR Stepper and Presets Bar
            var topBar = RacingDesign.Panel(panel, "Controls Bar", .03f, .82f, .94f, .085f, RacingDesign.Surface);
            var tier = MmrTierUtility.GetTier(testMmr);
            string tierName = MmrTierUtility.GetTierDisplayName(tier);
            RacingDesign.Label(topBar.transform, $"SIMULATED MMR: {testMmr:N0}  [{tierName}]", RacingType.Section, .02f, .2f, .38f, .6f, RacingDesign.TierColor(testMmr));

            RacingDesign.Button(topBar.transform, "-10K", .41f, .18f, .07f, .64f, () => { testMmr = Mathf.Max(0, testMmr - 10000); RefreshMmrToolView(panel); });
            RacingDesign.Button(topBar.transform, "-1K", .49f, .18f, .06f, .64f, () => { testMmr = Mathf.Max(0, testMmr - 1000); RefreshMmrToolView(panel); });
            RacingDesign.Button(topBar.transform, "+1K", .56f, .18f, .06f, .64f, () => { testMmr = Mathf.Min(150000, testMmr + 1000); RefreshMmrToolView(panel); });
            RacingDesign.Button(topBar.transform, "+10K", .63f, .18f, .07f, .64f, () => { testMmr = Mathf.Min(150000, testMmr + 10000); RefreshMmrToolView(panel); });
            RacingDesign.Button(topBar.transform, "REROLL", .71f, .18f, .09f, .64f, () => { testSeed += 17; RefreshMmrToolView(panel); });
            RacingDesign.Button(topBar.transform, "SET AS PROFILE", .81f, .18f, .17f, .64f, () => {
                RacingSaveManager.CurrentMmr = testMmr;
                SceneManager.LoadScene("RacingSelect");
            }, RacingDesign.Highlight);

            // Presets row
            int[] presets = new[] { 1000, 5000, 15000, 35000, 55000, 75000, 100000, 125000 };
            for (int p = 0; p < presets.Length; p++)
            {
                int val = presets[p];
                string label = val >= 1000 ? $"{val / 1000}K" : val.ToString();
                RacingDesign.Button(panel, label, .03f + p * .118f, .75f, .11f, .05f, () => { testMmr = val; RefreshMmrToolView(panel); });
            }

            // Generate Simulated Lobby
            var lobby = MatchmakingService.GenerateRankedLobby(testMmr, RacingSaveManager.SelectedVehicleId, RacingSaveManager.SelectedWheelId, "TEST PLAYER", testSeed);

            // Compute Lobby Statistics
            // Compute Lobby Statistics & Meta Counts
            int minMmr = int.MaxValue, maxMmr = int.MinValue, sumMmr = 0;
            int metaCount = 0, nearCount = 0, specCount = 0, stdCount = 0;
            for (int i = 0; i < lobby.Length; i++)
            {
                int m = lobby[i].mmr;
                if (m < minMmr) minMmr = m;
                if (m > maxMmr) maxMmr = m;
                sumMmr += m;
                if (!lobby[i].isHuman)
                {
                    if (lobby[i].buildCategory == MetaBuildCategory.Meta) metaCount++;
                    else if (lobby[i].buildCategory == MetaBuildCategory.NearMeta) nearCount++;
                    else if (lobby[i].buildCategory == MetaBuildCategory.Specialist) specCount++;
                    else stdCount++;
                }
            }
            float avgMmr = sumMmr / 12f;

            // Stats Summary Panel
            var statsPanel = RacingDesign.Panel(panel, "Stats Summary", .03f, .66f, .94f, .07f, RacingDesign.Surface);
            RacingDesign.Label(statsPanel.transform,
                $"SPREAD: {minMmr:N0} - {maxMmr:N0} (AVG {avgMmr:F0})  |  BUILDS (11 AI): {metaCount} META  •  {nearCount} NEAR-META  •  {specCount} SPECIALIST  •  {stdCount} STANDARD",
                RacingType.Caption, .02f, .2f, .96f, .6f, RacingDesign.Ink);

            // Table of 12 Racers
            var gridPanel = RacingDesign.Panel(panel, "Racers Grid", .03f, .03f, .94f, .61f, RacingDesign.Surface);
            RacingDesign.Label(gridPanel.transform, "GRID", RacingType.Caption, .02f, .92f, .03f, .06f, RacingDesign.Muted);
            RacingDesign.Label(gridPanel.transform, "RACER NAME", RacingType.Caption, .06f, .92f, .13f, .06f, RacingDesign.Muted);
            RacingDesign.Label(gridPanel.transform, "MMR / TIER", RacingType.Caption, .20f, .92f, .12f, .06f, RacingDesign.Muted);
            RacingDesign.Label(gridPanel.transform, "BUILD CATEGORY & PROB", RacingType.Caption, .33f, .92f, .21f, .06f, RacingDesign.Muted);
            RacingDesign.Label(gridPanel.transform, "FINAL VEHICLE", RacingType.Caption, .55f, .92f, .16f, .06f, RacingDesign.Muted);
            RacingDesign.Label(gridPanel.transform, "FINAL WHEELS", RacingType.Caption, .72f, .92f, .14f, .06f, RacingDesign.Muted);
            RacingDesign.Label(gridPanel.transform, "AI LINE", RacingType.Caption, .87f, .92f, .11f, .06f, RacingDesign.Muted);

            for (int i = 0; i < lobby.Length; i++)
            {
                var r = lobby[i];
                float y = .83f - i * .075f;
                Color rowColor = r.isHuman ? RacingDesign.Cyan : RacingDesign.Ink;
                if (r.isHuman)
                {
                    RacingDesign.Panel(gridPanel.transform, "Highlight", .01f, y - .01f, .98f, .065f, RacingDesign.Highlight, 4);
                }

                RacingDesign.Label(gridPanel.transform, $"#{r.gridPosition:00}", RacingType.Caption, .02f, y, .03f, .06f, rowColor);
                RacingDesign.Label(gridPanel.transform, r.displayName + (r.isHuman ? " (YOU)" : ""), RacingType.Body, .06f, y, .13f, .06f, rowColor);
                RacingDesign.Label(gridPanel.transform, $"{r.mmr:N0} [{MmrTierUtility.GetTierDisplayName(MmrTierUtility.GetTier(r.mmr))}]", RacingType.Caption, .20f, y, .12f, .06f, RacingDesign.TierColor(r.mmr));

                // Build category & probability
                string catName = r.buildCategory switch
                {
                    MetaBuildCategory.Meta => "META",
                    MetaBuildCategory.NearMeta => "NEAR-META",
                    MetaBuildCategory.Specialist => "SPECIALIST",
                    _ => "STANDARD"
                };
                Color catColor = r.buildCategory switch
                {
                    MetaBuildCategory.Meta => RacingDesign.Highlight,
                    MetaBuildCategory.NearMeta => RacingDesign.Cyan,
                    MetaBuildCategory.Specialist => RacingDesign.Secondary,
                    _ => RacingDesign.Muted
                };
                string catText = $"[{catName}]  (P: {r.metaProbabilityUsed * 100f:F1}%)";
                RacingDesign.Label(gridPanel.transform, catText, RacingType.Caption, .33f, y, .21f, .06f, catColor);

                // Vehicle and Wheels
                RacingDesign.Label(gridPanel.transform, RacingDesign.VehicleName(r.vehicleId), RacingType.Caption, .55f, y, .16f, .06f, rowColor);
                RacingDesign.Label(gridPanel.transform, r.wheelId.Replace("wheel-", "").ToUpperInvariant(), RacingType.Caption, .72f, y, .14f, .06f, rowColor);

                if (r.isHuman)
                {
                    RacingDesign.Label(gridPanel.transform, "HUMAN", RacingType.Caption, .87f, y, .11f, .06f, RacingDesign.Highlight);
                }
                else
                {
                    var profile = AiSkillModel.CreateProfile(r.mmr, r.personalitySeed);
                    RacingDesign.Label(gridPanel.transform, $"{profile.racingLineSkill * 100:F0}%", RacingType.Caption, .87f, y, .11f, .06f, rowColor);
                }
            }
        }
    }
}
