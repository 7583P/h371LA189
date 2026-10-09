using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public enum RacingTitleStyle { Plain, SubtleGlow, Neon }
    public enum RacingTitleAnimation { None, SlowPulse }
    public enum RacingTitleRequirement { Available, Races, Shortcuts, Aerials, Landings, PeakMmr, SeasonAchievement }

    [Serializable]
    public sealed class TitleDefinition
    {
        public string id, displayName;
        public RacingTitleStyle style;
        public Color primaryColor = Color.white, secondaryColor = Color.white;
        public float emissionIntensity;
        public RacingTitleAnimation animationType;
        public RacingTitleRequirement unlockRequirement;
        public int threshold;
    }

    public static class RacingTitleCatalog
    {
        public static readonly Color Emerald = new Color(.12f, 1f, .74f);
        static TitleDefinition[] definitions = {
            Define("current-season-rank", "CURRENT SEASON RANK", RacingTitleStyle.SubtleGlow, RacingTitleRequirement.Available, 0, new Color(.7f,.8f,.9f)),
            Define("night-city-veteran", "Night City Veteran", RacingTitleStyle.Plain, RacingTitleRequirement.Races, 50, new Color(.75f,.83f,.88f)),
            Define("shortcut-hunter", "Shortcut Hunter", RacingTitleStyle.Neon, RacingTitleRequirement.Shortcuts, 20, new Color(1f,.73f,.24f)),
            Define("aerial-specialist", "Aerial Specialist", RacingTitleStyle.Neon, RacingTitleRequirement.Aerials, 30, new Color(.76f,.39f,1f)),
            Define("landing-master", "Landing Master", RacingTitleStyle.SubtleGlow, RacingTitleRequirement.Landings, 50, new Color(.2f,.83f,1f)),
            Define("beyond-apex", "Beyond Apex", RacingTitleStyle.Neon, RacingTitleRequirement.PeakMmr, 10000, new Color(1f,.86f,.56f))
        };
        public static TitleDefinition[] Definitions => definitions;
        public static void Configure(TitleDefinition[] authored) { if (authored != null && authored.Length > 0) definitions = authored; }

        static TitleDefinition Define(string id, string name, RacingTitleStyle style, RacingTitleRequirement requirement, int threshold, Color color) =>
            new TitleDefinition { id = id, displayName = name, style = style, unlockRequirement = requirement, threshold = threshold,
                primaryColor = color, secondaryColor = new Color(.55f,.32f,1f), emissionIntensity = style == RacingTitleStyle.Neon ? .65f : .15f,
                animationType = id == "beyond-apex" ? RacingTitleAnimation.SlowPulse : RacingTitleAnimation.None };

        public static TitleDefinition Resolve(string id, int currentSeason = 1, int achievedMmr = 0)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id == "current-season-rank") return RankTitle(id, currentSeason, MmrTierUtility.GetTier(achievedMmr));
            if (id.StartsWith("season:", StringComparison.Ordinal)) {
                var parts = id.Split(':');
                if (parts.Length != 3 || !int.TryParse(parts[1], out int season) || !int.TryParse(parts[2], out int band) || season < 1 || band < 0 || band > 20) return null;
                return RankTitle(id, season, (MmrTier)band);
            }
            foreach (var definition in definitions) if (definition.id == id) return definition;
            return null;
        }

        static TitleDefinition RankTitle(string id, int season, MmrTier rank)
        {
            int band = (int)rank;
            string display = "Season " + season + " " + MmrTierUtility.GetTierDisplayName(rank);
            Color primary = RacingRankPalette.Primary(band);
            return new TitleDefinition { id = id, displayName = display, style = band < 6 ? RacingTitleStyle.Plain : band < 10 ? RacingTitleStyle.SubtleGlow : RacingTitleStyle.Neon,
                primaryColor = primary, secondaryColor = RacingRankPalette.Trim(band), emissionIntensity = .15f + band * .025f,
                animationType = band >= 18 ? RacingTitleAnimation.SlowPulse : RacingTitleAnimation.None, unlockRequirement = RacingTitleRequirement.SeasonAchievement };
        }

        public static bool Unlock(LeagueCompetitor competitor, string id)
        {
            if (Resolve(id) == null || competitor.unlockedTitleIds.Contains(id)) return false;
            competitor.unlockedTitleIds.Add(id); return true;
        }

        public static bool Select(LeagueCompetitor competitor, string id)
        {
            id = id ?? "";
            if (id.Length > 0 && (!competitor.unlockedTitleIds.Contains(id) || Resolve(id) == null)) return false;
            competitor.selectedTitleId = id; return true;
        }

        public static void UpdateAchievements(LeagueCompetitor competitor)
        {
            foreach (var definition in definitions) {
                if (competitor.unlockedTitleIds.Contains(definition.id)) continue;
                int value = definition.unlockRequirement == RacingTitleRequirement.Races ? competitor.races :
                    definition.unlockRequirement == RacingTitleRequirement.Shortcuts ? competitor.shortcutSuccesses :
                    definition.unlockRequirement == RacingTitleRequirement.Aerials ? competitor.aerialSuccesses :
                    definition.unlockRequirement == RacingTitleRequirement.Landings ? competitor.landingSuccesses :
                    definition.unlockRequirement == RacingTitleRequirement.PeakMmr ? competitor.peakMmr :
                    definition.unlockRequirement == RacingTitleRequirement.Available ? int.MaxValue : -1;
                if (value >= definition.threshold) Unlock(competitor, definition.id);
            }
        }

        public static void TrimSeasonTitles(LeagueCompetitor competitor, int minimumSeason)
        {
            for (int i = competitor.unlockedTitleIds.Count - 1; i >= 0; i--) {
                string id = competitor.unlockedTitleIds[i];
                if (!id.StartsWith("season:", StringComparison.Ordinal)) continue;
                var parts = id.Split(':');
                if (parts.Length == 3 && int.TryParse(parts[1], out int season) && season < minimumSeason) competitor.unlockedTitleIds.RemoveAt(i);
            }
            if (!competitor.unlockedTitleIds.Contains(competitor.selectedTitleId)) competitor.selectedTitleId = "";
        }
    }

    public static class RacingRankPalette
    {
        static readonly Color[] Colors = {
            new Color(.65f,.76f,.9f), new Color(.22f,.92f,.6f), new Color(.62f,.42f,1f),
            new Color(.16f,.75f,1f), new Color(1f,.23f,.38f), RacingTitleCatalog.Emerald,
            new Color(.76f,.38f,1f), new Color(1f,.15f,.4f), new Color(.25f,.9f,1f), new Color(.91f,.52f,1f)
        };
        public static Color Primary(int band)
        {
            // The individual reference plates take precedence for green Elite I/II.
            if (band == 1) return new Color(.84f, .55f, .36f);
            if (band == 3 || band == 5) return new Color(.18f, .82f, 1f);
            if (band == 7 || band == 17 || band == 19) return new Color(.92f, .91f, .79f);
            return Colors[Math.Max(0, Math.Min(9, band / 2))];
        }
        public static Color Trim(int band) => band < 6 ? new Color(.76f,.84f,.94f) : band == 10 || band == 11 ? new Color(.76f,.85f,.70f) : new Color(1f,.8f,.43f);
    }
}
