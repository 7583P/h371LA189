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
        private Text specHeader;
        private Text specTitle;
        private Text specDetails;
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
            RacingDesign.Panel(root, "Night Backdrop", 0, 0, 1, 1, new Color(0.035f, 0.025f, 0.075f, 1f), 0);

            // 2. Procedural Night City Neon Skyline & Atmosphere
            BuildNightCityAtmosphere(root);

            // 3. Top Header Bar (Branding, Player Profile & Economy)
            BuildTopHeader(root);

            // 4. Primary Navigation (Adventure & Racing 3D Pillars + Garage & Shop)
            BuildNavigationPillars(root);

            // 5. Right Showcase Stage (3D Vehicle Showroom with Interactive Preview)
            BuildHeroShowcaseStage(root);

            // Default focus to Adventure
            if (adventureCardBtn != null) RacingDesign.Focus(adventureCardBtn);
        }

        private void BuildTopHeader(RectTransform parent)
        {
            // Top Bar Glass Panel
            var header = RacingDesign.Panel(parent, "Top Bar Glass", 0.03f, 0.885f, 0.94f, 0.095f, new Color(0.05f, 0.06f, 0.12f, 0.92f), 8);
            RacingDesign.Panel(header.transform, "Header Bottom Glow", 0, 0, 1, 0.03f, RacingDesign.Cyan, 0);

            // Logo & Title
            RacingDesign.Label(header.transform, "HILL ADVENTURER", RacingType.Display, 0.02f, 0.40f, 0.32f, 0.52f, RacingDesign.Gold, display: true);
            RacingDesign.Label(header.transform, "PHYSICS EXPEDITION  •  3D COMPETITIVE RACING", RacingType.Caption, 0.022f, 0.12f, 0.32f, 0.30f, RacingDesign.Cyan);

            // Player Profile Pill
            var profilePill = RacingDesign.Panel(header.transform, "Profile Pill", 0.38f, 0.18f, 0.22f, 0.64f, new Color(0.08f, 0.09f, 0.18f, 0.85f), 6);
            RacingDesign.Panel(profilePill.transform, "Pill Accent", 0, 0, 0.02f, 1, RacingDesign.Apex, 0);
            RacingDesign.Label(profilePill.transform, "PILOT PROFILE", RacingType.Caption, 0.08f, 0.52f, 0.88f, 0.42f, RacingDesign.Muted);
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
            var mmrPill = RacingDesign.Panel(header.transform, "MMR Pill", 0.86f, 0.18f, 0.12f, 0.64f, new Color(0.10f, 0.06f, 0.18f, 0.85f), 6);
            RacingDesign.Label(mmrPill.transform, "RANKED MMR", RacingType.Caption, 0.10f, 0.55f, 0.80f, 0.38f, RacingDesign.Apex);
            RacingDesign.Label(mmrPill.transform, $"{RacingSaveManager.CurrentMmr:N0}", RacingType.Section, 0.10f, 0.08f, 0.80f, 0.50f, RacingDesign.Apex);
        }

        private void BuildNavigationPillars(RectTransform parent)
        {
            // Container Panel for Primary Navigation
            var navContainer = RacingDesign.Area(parent, "Primary Navigation Column", 0.03f, 0.035f, 0.39f, 0.835f);

            // ========================================================
            // PILLAR 1: ADVENTURE MODE (HERO CAMPAIGN - DEFAULT FOCUS)
            // ========================================================
            var advCard = RacingDesign.Panel(navContainer, "Adventure Main Pillar Card", 0, 0.53f, 1f, 0.47f, new Color(0.07f, 0.08f, 0.16f, 0.95f), 12);
            RacingDesign.Panel(advCard.transform, "Gold Left Accent", 0, 0, 0.016f, 1, RacingDesign.Gold, 0);

            // Badge
            var advBadge = RacingDesign.Panel(advCard.transform, "Adv Badge", 0.05f, 0.84f, 0.44f, 0.11f, new Color(0.98f, 0.78f, 0.18f, 0.18f), 4);
            RacingDesign.Label(advBadge.transform, "★ MAIN EXPERIENCE  •  SOLO", RacingType.Caption, 0, 0, 1, 1, RacingDesign.Gold, TextAnchor.MiddleCenter);

            // Title & Subtitle
            RacingDesign.Label(advCard.transform, "ADVENTURE", RacingType.Display, 0.05f, 0.54f, 0.90f, 0.28f, RacingDesign.Ink, display: true);
            RacingDesign.Label(advCard.transform, "PHYSICS EXPEDITION  •  2.5D HILLS & UNLIMITED PEAKS", RacingType.Caption, 0.055f, 0.46f, 0.89f, 0.08f, RacingDesign.Gold);

            // Bullets / Lore
            RacingDesign.Label(advCard.transform,
                "▶ Conquer vertical climbs, steep gaps and extreme terrain.\n▶ Collect coins, gems, hidden fuel tanks & reward crates.\n▶ Upgrade engine, suspension, tires & 4WD for each vehicle.",
                RacingType.Body, 0.055f, 0.22f, 0.89f, 0.22f, RacingDesign.Muted);

            // Big CTA Button
            adventureCardBtn = RacingDesign.Button(advCard.transform, "PLAY ADVENTURE", 0.05f, 0.045f, 0.90f, 0.16f, () => {
                ShowAdventureVehicle();
                Adventure();
            }, RacingDesign.Gold);

            // Alias buttons for automated validation / contracts
            var playAlias = RacingDesign.Area(advCard.transform, "PLAY", 0, 0, 0.01f, 0.01f).gameObject.AddComponent<Button>();
            playAlias.onClick.AddListener(() => {
                ShowAdventureVehicle();
            });

            var advAlias = RacingDesign.Area(advCard.transform, "ADVENTURE", 0, 0, 0.01f, 0.01f).gameObject.AddComponent<Button>();
            advAlias.onClick.AddListener(Adventure);

            // ========================================================
            // PILLAR 2: RACING 3D MODE (DIRECT START SCREEN COMPETITIVE ACCESS)
            // ========================================================
            var raceCard = RacingDesign.Panel(navContainer, "Racing 3D Pillar Card", 0, 0.17f, 1f, 0.34f, new Color(0.05f, 0.09f, 0.16f, 0.95f), 12);
            RacingDesign.Panel(raceCard.transform, "Cyan Left Accent", 0, 0, 0.016f, 1, RacingDesign.Cyan, 0);

            // Badge
            var raceBadge = RacingDesign.Panel(raceCard.transform, "Race Badge", 0.05f, 0.80f, 0.48f, 0.14f, new Color(0.08f, 0.94f, 0.98f, 0.18f), 4);
            RacingDesign.Label(raceBadge.transform, "⚡ 3D MULTIPLAYER  •  RANKED", RacingType.Caption, 0, 0, 1, 1, RacingDesign.Cyan, TextAnchor.MiddleCenter);

            // Title & Subtitle
            RacingDesign.Label(raceCard.transform, "RACING 3D", RacingType.Title, 0.05f, 0.52f, 0.90f, 0.26f, RacingDesign.Cyan);
            RacingDesign.Label(raceCard.transform, "12 RACERS  •  100,000 MMR  •  DRIFT, JUMP & STUNTS", RacingType.Caption, 0.055f, 0.40f, 0.89f, 0.11f, RacingDesign.Secondary);

            // Bullets
            RacingDesign.Label(raceCard.transform,
                "▶ Full PhysX collisions, jump button & aerial trick boosts.\n▶ Continuous MMR progression with high-tier META competition.",
                RacingType.Body, 0.055f, 0.20f, 0.89f, 0.19f, RacingDesign.Muted);

            // Big CTA Button
            racingCardBtn = RacingDesign.Button(raceCard.transform, "ENTER RACING 3D", 0.05f, 0.045f, 0.90f, 0.16f, () => {
                ShowMetaVehicle();
                EnterRacing();
            }, RacingDesign.Surface);

            // Alias button for automation
            var raceAlias = RacingDesign.Area(raceCard.transform, "RACING", 0, 0, 0.01f, 0.01f).gameObject.AddComponent<Button>();
            raceAlias.onClick.AddListener(EnterRacing);

            // ========================================================
            // SECONDARY NAVIGATION: GARAGE & SHOP
            // ========================================================
            var bottomNav = RacingDesign.Area(navContainer, "Secondary Nav Row", 0, 0, 1f, 0.15f);

            var garageBtn = RacingDesign.Button(bottomNav, "GARAGE", 0, 0.15f, 0.485f, 0.78f, () => OpenGarage(GarageSection.Vehicle));
            var shopBtn = RacingDesign.Button(bottomNav, "SHOP", 0.515f, 0.15f, 0.485f, 0.78f, () => OpenGarage(GarageSection.Store));

            // Extra alias buttons
            var tuningAlias = RacingDesign.Area(bottomNav, "TUNING", 0, 0, 0.01f, 0.01f).gameObject.AddComponent<Button>();
            tuningAlias.onClick.AddListener(() => OpenGarage(GarageSection.Tuning));

            var settingsAlias = RacingDesign.Area(bottomNav, "SETTINGS", 0, 0, 0.01f, 0.01f).gameObject.AddComponent<Button>();
            settingsAlias.onClick.AddListener(() => OpenGarage(GarageSection.Settings));
        }

        private void BuildHeroShowcaseStage(RectTransform parent)
        {
            var stageContainer = RacingDesign.Area(parent, "Hero Stage Container", 0.44f, 0.035f, 0.53f, 0.835f);

            // Studio 3D Vehicle Showroom (starts with Adventure Rig)
            showroom = RacingShowroom.Create(stageContainer, 0, 0.16f, 1f, 0.74f, "car-01", "wheel-offroad", new Color(0.95f, 0.45f, 0.10f));

            // Stage Header & Switcher Tabs
            var tabsPanel = RacingDesign.Panel(stageContainer, "Showcase Switcher Tabs", 0, 0.92f, 1f, 0.075f, new Color(0.06f, 0.07f, 0.14f, 0.90f), 6);
            RacingDesign.Label(tabsPanel.transform, "HERO SHOWCASE:", RacingType.Caption, 0.03f, 0, 0.24f, 1, RacingDesign.Muted);

            var tabAdv = RacingDesign.Button(tabsPanel.transform, "ADVENTURE RIG", 0.28f, 0.08f, 0.34f, 0.84f, ShowAdventureVehicle, RacingDesign.Surface);
            var tabMeta = RacingDesign.Button(tabsPanel.transform, "RANKED META", 0.64f, 0.08f, 0.34f, 0.84f, ShowMetaVehicle, RacingDesign.Surface);

            // Vehicle Specifications Card (under vehicle plinth)
            var specCard = RacingDesign.Panel(stageContainer, "Spec Card", 0, 0, 1f, 0.15f, new Color(0.06f, 0.07f, 0.14f, 0.94f), 8);
            RacingDesign.Panel(specCard.transform, "Spec Card Glow", 0, 0.96f, 1, 0.04f, RacingDesign.Gold, 0);

            specHeader = RacingDesign.Label(specCard.transform, "ACTIVE VEHICLE SHOWCASE", RacingType.Caption, 0.04f, 0.68f, 0.92f, 0.28f, RacingDesign.Gold);
            specTitle = RacingDesign.Label(specCard.transform, "ADVENTURE TRAIL JEEP", RacingType.Title, 0.04f, 0.34f, 0.92f, 0.34f, RacingDesign.Ink);
            specDetails = RacingDesign.Label(specCard.transform, "EXPEDITION LIVERY  •  ALL-TERRAIN TIRES  •  REINFORCED 4WD EXPEDITION CHASSIS", RacingType.Caption, 0.04f, 0.08f, 0.92f, 0.26f, RacingDesign.Muted);
        }

        public void ShowAdventureVehicle()
        {
            if (showroom != null) showroom.Show("car-01", "wheel-offroad", new Color(0.95f, 0.45f, 0.10f));
            if (specHeader != null) { specHeader.text = "ACTIVE SHOWCASE: ADVENTURE MODE"; specHeader.color = RacingDesign.Gold; }
            if (specTitle != null) { specTitle.text = "ADVENTURE TRAIL JEEP (EXPEDITION SPEC)"; specTitle.color = RacingDesign.Ink; }
            if (specDetails != null) { specDetails.text = "ICONIC ORANGE BODY  •  ALL-TERRAIN TIRES  •  REINFORCED 4WD EXPEDITION SUSPENSION"; specDetails.color = RacingDesign.Gold; }
        }

        public void ShowMetaVehicle()
        {
            // Showcase the exact META Competitive identity:
            // Yellow Body + FULLY CYAN WHEELS + Purple Pilot with Retro Helmet!
            if (showroom != null) showroom.Show("car-01", "wheel-meta-cyan", RacingVehicleFactory.MetaYellow);
            if (specHeader != null) { specHeader.text = "ACTIVE SHOWCASE: RACING 3D [RANKED META]"; specHeader.color = RacingDesign.Cyan; }
            if (specTitle != null) { specTitle.text = "RANKED META SPEEDSTER"; specTitle.color = RacingDesign.Cyan; }
            if (specDetails != null) { specDetails.text = "YELLOW VEHICLE BODY  •  100% CYAN ALLOY WHEELS  •  PURPLE SUIT & RETRO HELMET"; specDetails.color = RacingDesign.Cyan; }
        }

        private static void BuildNightCityAtmosphere(RectTransform parent)
        {
            var skyline = RacingDesign.Area(parent, "Night City Skyline Atmosphere", 0, 0, 1, 1);
            skyline.gameObject.AddComponent<Image>().color = Color.clear;

            // Distant Purple Horizon Glow
            RacingDesign.Panel(skyline, "Horizon Purple Glow", 0, 0.10f, 1, 0.35f, new Color(0.12f, 0.04f, 0.20f, 0.45f), 0);

            // Layer 1: Distant City Towers
            float[] distantWidths = new[] { .05f, .07f, .06f, .08f, .05f, .09f, .06f, .07f, .05f, .08f, .07f, .06f, .08f, .06f, .07f };
            float[] distantHeights = new[] { .45f, .62f, .40f, .70f, .52f, .78f, .48f, .65f, .55f, .72f, .46f, .68f, .58f, .74f, .50f };

            float curX = 0f;
            for (int i = 0; i < distantWidths.Length; i++)
            {
                float w = distantWidths[i];
                float h = distantHeights[i];
                Color dColor = new Color(0.045f, 0.035f, 0.09f, 0.65f);
                var tower = RacingDesign.Panel(skyline, $"DistantTower_{i}", curX, 0, w, h, dColor, 0);

                // Small window matrix
                for (int r = 1; r < 8; r++)
                {
                    float winY = r * 0.10f;
                    if (winY + 0.03f < h)
                    {
                        Color winC = ((i + r) % 2 == 0) ? new Color(0.18f, 0.91f, 0.91f, 0.10f) : new Color(0.82f, 0.55f, 1f, 0.08f);
                        RacingDesign.Panel(tower.transform, $"DWin_{r}", 0.15f, winY, 0.70f, 0.02f, winC, 0);
                    }
                }
                curX += w;
            }

            // Layer 2: Midground Neon Towers
            float[] midWidths = new[] { .07f, .09f, .06f, .10f, .07f, .08f, .06f, .11f, .08f, .07f, .09f, .06f };
            float[] midHeights = new[] { .60f, .82f, .52f, .88f, .66f, .76f, .58f, .90f, .68f, .78f, .84f, .62f };
            Color[] neonPalette = new[] { RacingDesign.Cyan, RacingDesign.Gold, RacingDesign.Apex, RacingDesign.Secondary };

            curX = 0.02f;
            for (int i = 0; i < midWidths.Length; i++)
            {
                float w = midWidths[i];
                float h = midHeights[i];
                Color mColor = (i % 2 == 0) ? new Color(0.06f, 0.05f, 0.12f, 0.85f) : new Color(0.08f, 0.06f, 0.15f, 0.90f);
                var tower = RacingDesign.Panel(skyline, $"MidTower_{i}", curX, 0, w, h, mColor, 0);

                // Neon Edge Strips on building borders
                Color edgeColor = neonPalette[i % neonPalette.Length];
                RacingDesign.Panel(tower.transform, "EdgeStripL", 0, 0, 0.025f, 1, new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.40f), 0);
                RacingDesign.Panel(tower.transform, "EdgeStripR", 0.975f, 0, 0.025f, 1, new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.40f), 0);

                // Rooftop Beacon Spire
                RacingDesign.Panel(tower.transform, "Spire", 0.46f, 0.985f, 0.08f, 0.08f, edgeColor, 0);

                // Neon Windows
                for (int winRow = 2; winRow < 11; winRow++)
                {
                    float winY = winRow * 0.08f;
                    if (winY + 0.035f < h)
                    {
                        Color winColor = new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.12f + ((i * 3 + winRow) % 4) * 0.09f);
                        RacingDesign.Panel(tower.transform, $"Win_{winRow}", 0.18f, winY, 0.64f, 0.022f, winColor, 0);
                    }
                }

                // Billboard Banner on select towers
                if (i == 2 || i == 7)
                {
                    var billboard = RacingDesign.Panel(tower.transform, "Billboard", 0.10f, 0.45f, 0.80f, 0.10f, new Color(0.02f, 0.03f, 0.08f, 0.95f), 4);
                    RacingDesign.Panel(billboard.transform, "BB Border", 0, 0, 1, 1, new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0.35f), 4);
                    RacingDesign.Label(billboard.transform, i == 2 ? "NEON APEX" : "CLIMB 2026", RacingType.Caption, 0, 0, 1, 1, edgeColor, TextAnchor.MiddleCenter);
                }

                curX += w + 0.012f;
            }

            // Upward Angled Neon Searchlight Beams
            var beam1 = RacingDesign.Panel(skyline, "Searchlight 1", 0.22f, 0.40f, 0.008f, 0.58f, new Color(0.18f, 0.91f, 0.91f, 0.22f), 0);
            beam1.transform.localRotation = Quaternion.Euler(0, 0, 15f);

            var beam2 = RacingDesign.Panel(skyline, "Searchlight 2", 0.78f, 0.35f, 0.008f, 0.62f, new Color(0.82f, 0.55f, 1f, 0.20f), 0);
            beam2.transform.localRotation = Quaternion.Euler(0, 0, -18f);

            // Radiant Cyan Horizon Strip
            RacingDesign.Panel(skyline, "Neon Horizon Strip", 0, 0.012f, 1, 0.006f, RacingDesign.Cyan, 0);
            RacingDesign.Panel(skyline, "Neon Horizon Glow", 0, 0.008f, 1, 0.025f, new Color(0.08f, 0.94f, 0.98f, 0.20f), 0);
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
            // Human or automated request to show play choices
            Adventure();
        }
    }
}
