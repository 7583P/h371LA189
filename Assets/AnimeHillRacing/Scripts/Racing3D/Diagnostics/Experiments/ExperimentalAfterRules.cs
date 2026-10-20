#if UNITY_EDITOR || COMPETITIVE_QA
using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D.Experiments
{
    // Editor/standalone QA only. Never compiled into a player, never attached to
    // LocalLeagueState, and never used to reinterpret existing earned titles.
    [Serializable]
    public sealed class AfterScale
    {
        public string id, label;
        public int threshold, step;
        public bool exclusiveThreshold;
        public static AfterScale Unified() => new AfterScale { id="after-3000-100-v1", label="AFTER", threshold=3000, step=100 };
        public static AfterScale HillCompatible() => new AfterScale { id="hill-beyond-10000-1000-v1", label="APEX BEYOND", threshold=10000, step=1000, exclusiveThreshold=true };
        public void Validate()
        {
            if(string.IsNullOrEmpty(id)||string.IsNullOrEmpty(label)||threshold<0||threshold>CompetitiveRating.MaximumMmr||step<1)
                throw new ArgumentException("Invalid experimental prestige scale.");
        }
        public bool TryLevel(int mmr,out int level)
        {
            Validate(); level=-1;
            if(mmr<threshold||(exclusiveThreshold&&mmr==threshold))return false;
            level=(mmr-threshold)/step;return true;
        }
        public AfterScale Copy() => (AfterScale)MemberwiseClone();
    }

    [Serializable]
    public sealed class AfterSeasonTitle
    {
        public string profileId, ruleId, label, raceId;
        public int season, peakQualifiedMmr, level, threshold, step;
        public bool exclusiveThreshold;
        public string Title => "Season "+season+" "+label+" "+level;
        internal AfterSeasonTitle Copy() => (AfterSeasonTitle)MemberwiseClone();
    }

    [Serializable]
    public sealed class AfterExperimentJournal
    {
        public int version=1, season, currentMmr, peakQualifiedMmr=-1;
        public string profileId, peakRaceId="";
        public bool qualified, ranked, closed;
        public AfterScale scale;
        public List<string> observedRaceIds=new List<string>();
        public List<AfterSeasonTitle> titles=new List<AfterSeasonTitle>();

        public static AfterExperimentJournal Create(string profileId,int season,AfterScale scale)
        {
            if(string.IsNullOrEmpty(profileId)||season<1)throw new ArgumentException("Experiment identity/season required.");
            scale.Validate();return new AfterExperimentJournal{profileId=profileId,season=season,scale=scale.Copy()};
        }
        public bool CurrentLevel(out int level)
        {
            level=-1;return ranked&&qualified&&scale.TryLevel(currentMmr,out level);
        }
        // Receipts are supplied after the real coordinator/simulator settlement.
        // A replay, unfinished race, stale season or imported carry cannot award
        // a peak. Rank order continues to use the real final visible rating.
        public bool Observe(RaceSettlementRecord receipt,int qualifiedSeason)
        {
            Validate();
            if(receipt==null||closed||string.IsNullOrEmpty(receipt.raceId)||receipt.seasonId!=season||
               receipt.state<(byte)RaceSettlementState.RaceSettled||receipt.state>(byte)RaceSettlementState.ProfileSaved||
               receipt.formulaVersion!=CompetitiveRating.FormulaVersion||
               receipt.createdUtc<GlobalSeasonCalendar.Start(season)||receipt.settledUtc<receipt.createdUtc||receipt.settledUtc>=GlobalSeasonCalendar.End(season)||
               (receipt.origin!=(byte)RaceOrigin.Physical&&receipt.origin!=(byte)RaceOrigin.Simulated)||
               (profileId==CompetitiveCoordinator.HumanId&&receipt.origin!=(byte)RaceOrigin.Physical)||
               receipt.participantIds==null||receipt.participantIds.Length!=12||receipt.finalMmr==null||receipt.finalMmr.Length!=12||
               receipt.placementAfter==null||receipt.placementAfter.Length!=12||receipt.outcomes==null||receipt.outcomes.Length!=12||
               observedRaceIds.Contains(receipt.raceId))return false;
            var unique=new HashSet<string>(StringComparer.Ordinal);int own=-1;
            for(int i=0;i<12;i++){
                if(string.IsNullOrEmpty(receipt.participantIds[i])||!unique.Add(receipt.participantIds[i])||receipt.finalMmr[i]<0||receipt.finalMmr[i]>CompetitiveRating.MaximumMmr||
                   receipt.outcomes[i]>(byte)ParticipantOutcome.Abandoned||receipt.placementAfter[i]<0||receipt.placementAfter[i]>CompetitiveRating.PlacementRaces)return false;
                if(receipt.participantIds[i]==profileId)own=i;
            }
            if(own<0)return false;
            observedRaceIds.Add(receipt.raceId);currentMmr=receipt.finalMmr[own];ranked=receipt.placementAfter[own]>=CompetitiveRating.PlacementRaces;
            qualified=ranked&&qualifiedSeason==season;
            if(!receipt.partial&&!receipt.abandonment&&qualified&&receipt.outcomes[own]==(byte)ParticipantOutcome.Finished&&currentMmr>peakQualifiedMmr){peakQualifiedMmr=currentMmr;peakRaceId=receipt.raceId;}
            return true;
        }
        public AfterSeasonTitle CloseSeason()
        {
            Validate();
            if(closed){foreach(var existing in titles)if(existing.season==season)return existing.Copy();return null;}
            closed=true;
            if(peakQualifiedMmr<0||!scale.TryLevel(peakQualifiedMmr,out int level))return null;
            var title=new AfterSeasonTitle{profileId=profileId,season=season,peakQualifiedMmr=peakQualifiedMmr,level=level,raceId=peakRaceId,
                ruleId=scale.id,label=scale.label,threshold=scale.threshold,step=scale.step,exclusiveThreshold=scale.exclusiveThreshold};
            titles.Add(title);return title.Copy();
        }
        public void BeginSeason(int nextSeason,int carry,AfterScale nextScale)
        {
            if(!closed||nextSeason!=season+1||carry<0||carry>currentMmr)throw new InvalidOperationException("A closed season and downward carry are required.");
            nextScale.Validate();season=nextSeason;currentMmr=carry;peakQualifiedMmr=-1;peakRaceId="";qualified=false;closed=false;
            scale=nextScale.Copy();observedRaceIds.Clear();Validate();
        }
        public void Validate()
        {
            if(version!=1||season<1||season>=GlobalSeasonCalendar.MaximumSeason||string.IsNullOrEmpty(profileId)||scale==null||observedRaceIds==null||titles==null)
                throw new ArgumentException("Invalid experiment journal.");
            scale.Validate();var seen=new HashSet<int>();var races=new HashSet<string>(StringComparer.Ordinal);
            foreach(var id in observedRaceIds)if(string.IsNullOrEmpty(id)||!races.Add(id))throw new ArgumentException("Duplicate experiment receipt.");
            foreach(var title in titles){
                if(title.profileId!=profileId||title.season>season||title.season<1||!seen.Add(title.season)||string.IsNullOrEmpty(title.raceId))throw new ArgumentException("Invalid frozen title identity.");
                var frozen=new AfterScale{id=title.ruleId,label=title.label,threshold=title.threshold,step=title.step,exclusiveThreshold=title.exclusiveThreshold};
                if(!frozen.TryLevel(title.peakQualifiedMmr,out int level)||level!=title.level)throw new ArgumentException("Frozen title does not match its original rule.");
            }
        }
    }
}
#endif
