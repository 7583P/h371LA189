using AnimeHillRacing.Core;
using AnimeHillRacing.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Racing3D
{
    public sealed class RacingMainMenuUI : MonoBehaviour
    {
        private RectTransform root;
        private RacingShowroom showroom;
        private Button adventureCardBtn;
        private Button racingCardBtn;

        private void Start()
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.gameObject.SetActive(false);
            RacingFeedback.Ensure();
            gameObject.AddComponent<RacingQualityScope>();

            root = RacingDesign.Canvas("Hill Adventurer Main Menu", 30);
            root.gameObject.AddComponent<RacingScreenMotion>();

            // 1. Deep Night City Obsidian Backdrop
            RacingDesign.Panel(root, "Night Backdrop", 0, 0, 1, 1, new Color(0.025f, 0.020f, 0.055f, 1f), 0);

            // 2. Procedural Night City Neon Skyline, Starry Sky & Fog Atmosphere
            BuildNightCityAtmosphere(root);

            // 3. Top Header Bar (Branding, Player Profile & Economy)
            BuildTopHeader(root);

            // 4. Center Hero Showcase Stage (3D Formula Apex META Turntable)
            BuildHeroShowcaseStage(root);

            // 5. Primary Mode Pillars (Adventure Left & Racing / 3D Mode Right)
            BuildModePillars(root);

            // 6. Bottom Secondary Navigation (Garage & Shop)
            BuildBottomNavigation(root);

            // Default focus to Adventure
            if (adventureCardBtn != null) RacingDesign.Focus(adventureCardBtn);
        }

        private void BuildTopHeader(RectTransform parent)
        {
            // Top Bar Glass Panel
            var header = RacingDesign.Panel(parent, "Top Bar Glass", 0.025f, 0.885f, 0.95f, 0.095f, new Color(0.05f, 0.06f, 0.12f, 0.94f), 8);
            RacingDesign.Panel(header.transform, "Header Bottom Glow", 0, 0, 1, 0.028f, RacingDesign.Cyan, 0);

            // Logo & Title
            RacingDesign.Label(header.transform, "HILL ADVENTURER", RacingType.Display, 0.02f, 0.40f, 0.34f, 0.54f, RacingDesign.Gold, display: true);
            RacingDesign.Label(header.transform, "EXPEDITIONS  •  OFFLINE RANKED RACING", RacingType.Caption, 0.022f, 0.12f, 0.34f, 0.30f, RacingDesign.Cyan);

            // Player Profile Pill
            var profilePill = RacingDesign.Panel(header.transform, "Profile Pill", 0.39f, 0.18f, 0.21f, 0.64f, new Color(0.08f, 0.09f, 0.18f, 0.85f), 6);
            RacingDesign.Panel(profilePill.transform, "Pill Accent", 0, 0, 0.025f, 1, RacingDesign.Apex, 0);
            RacingDesign.Label(profilePill.transform, "PILOT STATUS", RacingType.Caption, 0.08f, 0.52f, 0.88f, 0.42f, RacingDesign.Muted);
            string rankName = RacingSaveManager.CurrentMmr >= 100000 ? "APEX CHAMPION" : RacingSaveManager.CurrentMmr >= 50000 ? "MASTER RACER" : "EXPEDITION VETERAN";
            RacingDesign.Label(profilePill.transform, rankName, RacingType.Body, 0.08f, 0.08f, 0.88f, 0.48f, RacingDesign.Ink);

            // Economy Counters
            // Coins
            var coinPill = RacingDesign.Panel(header.transform, "Coin Pill", 0.62f, 0.18f, 0.11f, 0.64f, new Color(0.12f, 0.10f, 0.05f, 0.85f), 6);
            RacingDesign.Label(coinPill.transform, "COINS", RacingType.Caption, 0.10f, 0.55f, 0.80f, 0.38f, RacingDesign.Gold);
            RacingDesign.Label(coinPill.transform, $"{SaveManager.TotalCoins:N0}", RacingType.Section, 0.10f, 0.08f, 0.80f, 0.50f, RacingDesign.Gold);

            // Gems
            var gemPill = RacingDesign.Panel(header.transform, "Gem Pill", 0.74f, 0.18f, 0.11f, 0.64f, new Color(0.04f, 0.12f, 0.14f, 0.85f), 6);
            RacingDesign.Label(gemPill.transform, "GEMS", RacingType.Caption, 0.10f, 0.55f, 0.80f, 0.38f, RacingDesign.Cyan);
            RacingDesign.Label(gemPill.transform, $"{SaveManager.TotalGems:N0}", RacingType.Section, 0.10f, 0.08f, 0.80f, 0.50f, RacingDesign.Cyan);

            // MMR
            var mmrPill = RacingDesign.Panel(header.transform, "MMR Pill", 0.86f, 0.18f, 0.125f, 0.64f, new Color(0.10f, 0.06f, 0.18f, 0.85f), 6);
            RacingDesign.Label(mmrPill.transform, "RANKED MMR", RacingType.Caption, 0.10f, 0.55f, 0.80f, 0.38f, RacingDesign.Apex);
            RacingDesign.Label(mmrPill.transform, $"{RacingSaveManager.CurrentMmr:N0}", RacingType.Section, 0.10f, 0.08f, 0.80f, 0.50f, RacingDesign.Apex);
        }

        private void BuildHeroShowcaseStage(RectTransform parent)
        {
            // Center Stage Container
            var stageContainer = RacingDesign.Area(parent, "Hero Stage Center Container", 0.315f, 0.14f, 0.37f, 0.725f);

            // Top Meta Identity Tag Pill
            var metaPill = RacingDesign.Panel(stageContainer, "Current Meta Tag", 0.05f, 0.91f, 0.90f, 0.075f, new Color(0.08f, 0.07f, 0.16f, 0.92f), 6);
            RacingDesign.Panel(metaPill.transform, "Meta Tag Left Accent", 0, 0, 0.02f, 1, RacingVehicleFactory.MetaYellow, 0);
            RacingDesign.Label(metaPill.transform, "FORMULA APEX  /  CURRENT META", RacingType.Caption, 0.04f, 0, 0.92f, 1, RacingVehicleFactory.MetaYellow, TextAnchor.MiddleCenter);

            // 3D Showroom Stage initialized with Formula Apex META: Intense Yellow body, 100% Cyan Wheels
            showroom = RacingShowroom.Create(stageContainer, 0, 0.12f, 1f, 0.77f, "formula-01", "wheel-meta-cyan", RacingVehicleFactory.MetaYellow);

            // Interactive helper label under turntable plinth
            var plinthCard = RacingDesign.Panel(stageContainer, "Plinth Card", 0.08f, 0.01f, 0.84f, 0.09f, new Color(0.06f, 0.07f, 0.14f, 0.90f), 6);
            RacingDesign.Panel(plinthCard.transform, "Plinth Cyan Top Glow", 0, 0.95f, 1, 0.05f, RacingDesign.Cyan, 0);
            RacingDesign.Label(plinthCard.transform, "FORMULA APEX  /  READY TO RACE", RacingType.Caption, 0.02f, 0.48f, 0.96f, 0.48f, RacingDesign.Cyan, TextAnchor.MiddleCenter);
            RacingDesign.Label(plinthCard.transform, "DRAG TO ROTATE  •  SCROLL TO ZOOM", RacingType.Caption, 0.02f, 0.06f, 0.96f, 0.44f, RacingDesign.Muted, TextAnchor.MiddleCenter);
        }

        private void BuildModePillars(RectTransform parent)
        {
            // ========================================================
            // LEFT PILLAR: ADVENTURE MODE (HERO CAMPAIGN - DEFAULT FOCUS)
            // ========================================================
            var advCard = RacingDesign.Panel(parent, "Adventure Main Pillar Card", 0.025f, 0.14f, 0.275f, 0.725f, new Color(0.07f, 0.08f, 0.16f, 0.96f), 12);
            RacingDesign.Panel(advCard.transform, "Gold Left Accent", 0, 0, 0.018f, 1, RacingDesign.Gold, 0);

            // Top Badge
            var advBadge = RacingDesign.Panel(advCard.transform, "Adv Badge", 0.06f, 0.89f, 0.88f, 0.065f, new Color(0.98f, 0.78f, 0.18f, 0.18f), 4);
            RacingDesign.Label(advBadge.transform, "★ EXPEDITION CAMPAIGN  •  SOLO", RacingType.Caption, 0, 0, 1, 1, RacingDesign.Gold, TextAnchor.MiddleCenter);

            // Title & Subtitle
            RacingDesign.Label(advCard.transform, "ADVENTURE", RacingType.Display, 0.06f, 0.72f, 0.88f, 0.16f, RacingDesign.Ink, display: true);
            RacingDesign.Label(advCard.transform, "ENDLESS HILL CLIMB", RacingType.Caption, 0.065f, 0.66f, 0.87f, 0.06f, RacingDesign.Gold);

            // Feature Highlights
            string advFeatures = "Explore endless hills and daring jumps.\n\n" +
                "Balance every climb. Master the landing.\n\n" +
                "Collect rewards and build your next adventure.";
            RacingDesign.Label(advCard.transform, advFeatures, RacingType.Body, 0.065f, 0.22f, 0.87f, 0.42f, RacingDesign.Muted);

            // Big Prominent Gold CTA Button
            adventureCardBtn = RacingDesign.Button(advCard.transform, "ADVENTURE", 0.06f, 0.045f, 0.88f, 0.12f, () => {
                ShowAdventureVehicle();
                Adventure();
            }, RacingDesign.Gold);

            // ========================================================
            // RIGHT PILLAR: RACING 3D MODE (DIRECT START SCREEN COMPETITIVE ACCESS)
            // ========================================================
            var raceCard = RacingDesign.Panel(parent, "Racing 3D Pillar Card", 0.70f, 0.14f, 0.275f, 0.725f, new Color(0.05f, 0.09f, 0.17f, 0.96f), 12);
            RacingDesign.Panel(raceCard.transform, "Cyan Right Accent", 0.982f, 0, 0.018f, 1, RacingDesign.Cyan, 0);

            // Top Badge
            var raceBadge = RacingDesign.Panel(raceCard.transform, "Race Badge", 0.06f, 0.89f, 0.88f, 0.065f, new Color(0.08f, 0.94f, 0.98f, 0.18f), 4);
            RacingDesign.Label(raceBadge.transform, "3D RACING  •  OFFLINE RANKED", RacingType.Caption, 0, 0, 1, 1, RacingDesign.Cyan, TextAnchor.MiddleCenter);

            // Title & Subtitle
            RacingDesign.Label(raceCard.transform, "RACING 3D", RacingType.Display, 0.06f, 0.72f, 0.88f, 0.16f, RacingDesign.Cyan, display: true);
            RacingDesign.Label(raceCard.transform, "12-RACER APEX CHAMPIONSHIP", RacingType.Caption, 0.065f, 0.66f, 0.87f, 0.06f, RacingDesign.Secondary);

            // Feature Highlights
            string raceFeatures = "Race eleven rivals through the neon city.\n\n" +
                "Chain drifts, turbos and aerial stunts.\n\n" +
                "Find faster routes. Climb the ranked ladder.";
            RacingDesign.Label(raceCard.transform, raceFeatures, RacingType.Body, 0.065f, 0.22f, 0.87f, 0.42f, RacingDesign.Muted);

            // Big Prominent Cyan CTA Button
            racingCardBtn = RacingDesign.Button(raceCard.transform, "RACING", 0.06f, 0.045f, 0.88f, 0.12f, () => {
                ShowMetaVehicle();
                EnterRacing();
            }, RacingDesign.Surface);

        }

        private void BuildBottomNavigation(RectTransform parent)
        {
            // Centered Secondary Navigation Bar underneath Showcase Plinth
            var bottomNav = RacingDesign.Area(parent, "Bottom Navigation Row", 0.32f, 0.035f, 0.36f, 0.085f);

            var garageBtn = RacingDesign.Button(bottomNav, "GARAGE", 0.02f, 0.05f, 0.46f, 0.90f, () => OpenGarage(GarageSection.Vehicle));
            var shopBtn = RacingDesign.Button(bottomNav, "SHOP", 0.52f, 0.05f, 0.46f, 0.90f, () => OpenGarage(GarageSection.Store));

        }

        public void ShowAdventureVehicle()
        {
            if (showroom != null) showroom.Show("car-01", "wheel-offroad", new Color(0.95f, 0.45f, 0.10f));
        }

        public void ShowMetaVehicle()
        {
            // Showcase the authoritative Formula Apex META competitive identity:
            // Intense Yellow Body + FULLY CYAN WHEELS + Purple Pilot with Retro Helmet!
            if (showroom != null) showroom.Show("formula-01", "wheel-meta-cyan", RacingVehicleFactory.MetaYellow);
        }

        private static void BuildNightCityAtmosphere(RectTransform parent)
        {
            var skyline = RacingDesign.Area(parent, "Night City Skyline Atmosphere", 0, 0, 1, 1);
            skyline.gameObject.AddComponent<Image>().color = Color.clear;

            // 1. Starry Sky Dome (50 glowing stars across upper sky)
            var starsContainer = RacingDesign.Area(skyline, "Starry Sky", 0, 0.45f, 1, 0.55f);
            var starRng = new DeterministicRng(44211);
            Color[] starColors = { Color.white, new Color(0.85f, 0.95f, 1f), new Color(0.18f, 0.94f, 1f), new Color(1f, 0.95f, 0.65f) };
            for (int s = 0; s < 50; s++)
            {
                float sx = starRng.NextFloat();
                float sy = starRng.NextFloat();
                float sSize = starRng.Range(0.0025f, 0.0055f);
                Color sCol = starColors[s % starColors.Length];
                sCol.a = starRng.Range(0.40f, 0.95f);
                RacingDesign.Panel(starsContainer, $"Star_{s}", sx, sy, sSize, sSize * 1.5f, sCol, 0);
            }

            // 2. Distant Deep Purple / Violet Horizon Glow
            RacingDesign.Panel(skyline, "Horizon Purple Glow", 0, 0.08f, 1, 0.42f, new Color(0.12f, 0.04f, 0.22f, 0.50f), 0);

            // 3. Layer 1: Distant City Towers & Spires (15 silhouetted spires)
            float[] distantWidths = new[] { .05f, .07f, .06f, .08f, .05f, .09f, .06f, .07f, .05f, .08f, .07f, .06f, .08f, .06f, .07f };
            float[] distantHeights = new[] { .48f, .65f, .42f, .72f, .55f, .80f, .50f, .68f, .58f, .75f, .48f, .70f, .60f, .76f, .52f };

            float curX = 0f;
            for (int i = 0; i < distantWidths.Length; i++)
            {
                float w = distantWidths[i];
                float h = distantHeights[i];
                Color dColor = new Color(0.042f, 0.032f, 0.088f, 0.72f);
                var tower = RacingDesign.Panel(skyline, $"DistantTower_{i}", curX, 0, w, h, dColor, 0);

                // Distant window matrix
                for (int r = 1; r < 8; r++)
                {
                    float winY = r * 0.10f;
                    if (winY + 0.03f < h)
                    {
                        Color winC = ((i + r) % 2 == 0) ? new Color(0.18f, 0.91f, 0.91f, 0.12f) : new Color(0.82f, 0.55f, 1f, 0.10f);
                        RacingDesign.Panel(tower.transform, $"DWin_{r}", 0.15f, winY, 0.70f, 0.02f, winC, 0);
                    }
                }
                curX += w;
            }

            // 4. Layer 2: Midground Neon Skyscrapers
            float[] midWidths = new[] { .07f, .09f, .06f, .10f, .07f, .08f, .06f, .11f, .08f, .07f, .09f, .06f };
            float[] midHeights = new[] { .62f, .84f, .54f, .90f, .68f, .78f, .60f, .92f, .70f, .80f, .86f, .64f };
            Color[] neonPalette = new[] { RacingDesign.Cyan, RacingDesign.Gold, RacingDesign.Apex, RacingDesign.Secondary };

            curX = 0.015f;
            for (int i = 0; i < midWidths.Length; i++)
            {
                float w = midWidths[i];
                float h = midHeights[i];
                Color mColor = (i % 2 == 0) ? new Color(0.055f, 0.045f, 0.11f, 0.88f) : new Color(0.075f, 0.055f, 0.14f, 0.92f);
                var tower = RacingDesign.Panel(skyline, $"MidTower_{i}", curX, 0, w, h, mColor, 0);

                // Neon Edge Strips on building borders
                Color edgeColor = neonPalette[i % neonPalette.Length];
                RacingDesign.Panel(tower.transform, "EdgeStripL", 0, 0, 0.025f, 1, new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.45f), 0);
                RacingDesign.Panel(tower.transform, "EdgeStripR", 0.975f, 0, 0.025f, 1, new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.45f), 0);

                // Rooftop Beacon Spire
                RacingDesign.Panel(tower.transform, "Spire", 0.46f, 0.985f, 0.08f, 0.08f, edgeColor, 0);

                // Neon Windows
                for (int winRow = 2; winRow < 11; winRow++)
                {
                    float winY = winRow * 0.08f;
                    if (winY + 0.035f < h)
                    {
                        Color winColor = new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.14f + ((i * 3 + winRow) % 4) * 0.10f);
                        RacingDesign.Panel(tower.transform, $"Win_{winRow}", 0.18f, winY, 0.64f, 0.022f, winColor, 0);
                    }
                }

                // Billboard Banner on select towers
                if (i == 2 || i == 7)
                {
                    var billboard = RacingDesign.Panel(tower.transform, "Billboard", 0.10f, 0.45f, 0.80f, 0.10f, new Color(0.02f, 0.03f, 0.08f, 0.95f), 4);
                    RacingDesign.Panel(billboard.transform, "BB Border", 0, 0, 1, 1, new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.40f), 4);
                    RacingDesign.Label(billboard.transform, i == 2 ? "FORMULA APEX" : "CLIMB 2026", RacingType.Caption, 0, 0, 1, 1, edgeColor, TextAnchor.MiddleCenter);
                }

                curX += w + 0.012f;
            }

            // 5. Upward Angled Neon Searchlight Beams
            var beam1 = RacingDesign.Panel(skyline, "Searchlight 1", 0.22f, 0.38f, 0.008f, 0.62f, new Color(0.18f, 0.91f, 0.91f, 0.24f), 0);
            beam1.transform.localRotation = Quaternion.Euler(0, 0, 15f);

            var beam2 = RacingDesign.Panel(skyline, "Searchlight 2", 0.78f, 0.32f, 0.008f, 0.66f, new Color(0.82f, 0.55f, 1f, 0.22f), 0);
            beam2.transform.localRotation = Quaternion.Euler(0, 0, -18f);

            // 6. Radiant Cyan & Violet Horizon Fog Strips
            RacingDesign.Panel(skyline, "Neon Horizon Strip", 0, 0.012f, 1, 0.006f, RacingDesign.Cyan, 0);
            RacingDesign.Panel(skyline, "Neon Horizon Glow", 0, 0.008f, 1, 0.030f, new Color(0.08f, 0.94f, 0.98f, 0.22f), 0);
        }

        private static void OpenGarage(GarageSection section)
        {
            GameSession.RequestDirectGarage(section);
            SceneManager.LoadScene("Garage");
        }

        public void Adventure()
        {
            GameSession.BeginAdventureTrackSelection();
            SceneManager.LoadScene("PreStart");
        }

        public void EnterRacing()
        {
            GameSession.ClearGarageContext();
            SceneManager.LoadScene("RacingSelect");
        }

        public void ShowPlayChoices()
        {
            Adventure();
        }
    }
}
