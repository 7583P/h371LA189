using System;

namespace AnimeHillRacing.Racing3D
{
    // Independent of career placement. Bot qualification uses a spare v4 bit,
    // preserving the binary record layout and every existing identity.
    public static class SeasonQualification
    {
        public const int ContractVersion = 1;
        public const byte QualifiedFlag = 2;
        public const string PendingLabel = "(Season Placement)";

        public static bool IsOfficial(LocalLeagueState state, int node) => node == state.bots.Length
            ? IsOfficial(state.human, state.currentSeasonNumber)
            : state.bots[node].Ranked && (state.bots[node].flags & QualifiedFlag) != 0;
        public static bool IsOfficial(LeagueCompetitor racer, int season) => racer.Ranked && racer.qualifiedSeason == season;
        public static string DisplayRank(LeagueCompetitor racer, int season) => !racer.Ranked
            ? MmrTierUtility.PlacementLabel(racer.placementGames)
            : IsOfficial(racer, season) ? MmrTierUtility.GetDisplayName(racer.currentMmr) : PendingLabel;
        public static bool CanQualify(LocalLeagueState s, int season, long settledUtc) =>
            season == s.currentSeasonNumber && settledUtc >= s.seasonStartTimestamp && settledUtc < s.seasonEndTimestamp;

        public static bool Migrate(LocalLeagueState s)
        {
            if (s.qualificationContract >= ContractVersion&&s.qualificationSeason==s.currentSeasonNumber) return false;
            // A current-season race count alone is insufficient: it includes DNFs
            // and abandonments. Require persisted evidence of a real finish.
            s.human.qualifiedSeason = 0;
            for (int i = 0; i < s.bots.Length; i++)
            {
                ref var b = ref s.bots[i];
                b.flags &= unchecked((byte)~QualifiedFlag);
                // At most one race per bot can straddle a boundary. Two proven
                // finishes exclude the possibility of only a stale old race.
                if (b.seasonRaces > b.dnfs+1 && b.lastRaceTicks >= s.seasonStartTimestamp && b.lastRaceTicks < s.seasonEndTimestamp)
                    b.flags |= QualifiedFlag;
            }
            foreach (var r in s.ledger.recentSettlements)
            {
                if (r.abandonment || r.partial || r.state < (byte)RaceSettlementState.RaceSettled ||
                    r.createdUtc<s.seasonStartTimestamp||!CanQualify(s, r.seasonId, r.settledUtc)) continue;
                for (int k = 0; k < r.participantIds.Length && k < r.outcomes.Length; k++)
                {
                    if (r.outcomes[k] != (byte)ParticipantOutcome.Finished) continue;
                    if (r.participantIds[k] == CompetitiveCoordinator.HumanId && r.origin == (byte)RaceOrigin.Physical)
                        s.human.qualifiedSeason = s.currentSeasonNumber;
                    else if (LeagueBotCatalog.TryIndex(s.populationSeed, r.participantIds[k], out int i) && i >= 0 && i < s.bots.Length)
                        s.bots[i].flags |= QualifiedFlag;
                }
            }
            s.qualificationContract = ContractVersion;
            s.qualificationSeason=s.currentSeasonNumber;
            return true;
        }
    }
}
