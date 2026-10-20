using System;
namespace AnimeHillRacing.Racing3D
{
    // Global quarterly seasons with the existing soft-reset curve. Each
    // boundary is processed exactly once, in order, with a stable receipt
    // "ST:<source>-><destination>:c<contract>". UNRANKED identities keep their
    // Placement progress and 0 MMR (Placement is not re-run every season).
    public static class LocalSeasonEngine
    {
        public static long Boundary(int season)=>GlobalSeasonCalendar.Start(season);
        // Retained signature for legacy fixtures: the installation instant is ignored.
        public static long Boundary(long ignoredInstallTicks,int season)=>GlobalSeasonCalendar.Start(season+1);

        public static long AcceptUtc(LocalLeagueState state,long ticks){long safe=Math.Max(state.installationTimestamp,Math.Max(ticks,state.lastKnownUtcTimestamp));state.lastKnownUtcTimestamp=safe;return safe;}

        static long PopulationRating(LocalLeagueState state){long sum=state.human.currentMmr;foreach(var b in state.bots)sum+=b.currentMmr;return sum;}

        public static bool AdvanceChunk(LocalLeagueState state,LocalLeagueConfiguration config,long utc,int maximumRecords,LeagueRankIndex index=null)
        {
            if(utc<state.seasonEndTimestamp)return false;
            if(state.lastProcessedSeasonNumber>=state.currentSeasonNumber)throw new InvalidOperationException("Duplicate season transition.");
            string id=GlobalSeasonCalendar.TransitionId(state.currentSeasonNumber,state.currentSeasonNumber+1);
            if(state.ledger.HasSeasonTransition(id))throw new InvalidOperationException("Season transition "+id+" was already committed.");
            if(state.seasonTransitionCursor<0){
                var standings=index??new LeagueRankIndex(state,true);
                int position=SeasonQualification.IsOfficial(state.human,state.currentSeasonNumber)?standings.RankOf(state.bots.Length)+1:0;
                int top=SeasonalTitleRules.TopBand(position);
                if(top>0)RacingTitleCatalog.Unlock(state.human,"season-top:"+state.currentSeasonNumber+":"+top);
                state.seasonTransitionCursor=0;
                state.pendingSeasonArchive=new LeagueSeasonArchive{seasonNumber=state.currentSeasonNumber,population=state.bots.Length,startTimestamp=state.seasonStartTimestamp,endTimestamp=state.seasonEndTimestamp,records=new CompactBotSeason[state.bots.Length],ratingBefore=PopulationRating(state)};
            }
            int end=Math.Min(state.bots.Length,state.seasonTransitionCursor+Math.Max(1,maximumRecords));
            while(state.seasonTransitionCursor<end){int i=state.seasonTransitionCursor;var b=state.bots[i];
                int reset=b.Ranked?config.ResetMmr(b.currentMmr):b.currentMmr,peak=Math.Max(b.currentMmr,b.seasonPeakMmr);
                state.pendingSeasonArchive.records[i]=new CompactBotSeason{startingMmr=b.seasonStartingMmr,endingMmr=b.currentMmr,peakMmr=peak,nextStartingMmr=reset,races=b.seasonRaces,wins=b.seasonWins,podiums=b.seasonPodiums,bestLap=b.seasonBestLap,selectedTitle=b.selectedTitle,selectedTitleSeason=b.selectedTitleSeason};
                state.ledger.budget.softResetRemoval+=b.currentMmr-reset;
                b.peakMmr=Math.Max(b.peakMmr,peak);if(b.seasonRaces>0 && b.Ranked && (b.flags&SeasonQualification.QualifiedFlag)!=0 && (b.personalitySeed&3)!=0){b.selectedTitle=(ushort)(32+(int)MmrTierUtility.GetTier(peak));b.selectedTitleSeason=state.currentSeasonNumber;}
                b.currentMmr=b.seasonStartingMmr=b.seasonPeakMmr=reset;b.seasonRaces=b.seasonWins=b.seasonPodiums=0;b.seasonBestLap=0;
                b.flags&=unchecked((byte)~SeasonQualification.QualifiedFlag);
                if(b.Ranked)b.uncertainty=CompactLeagueBot.Pack01(Math.Min(1,b.Uncertainty+.12f));
                state.bots[i]=b;index?.Update(i,reset);state.seasonTransitionCursor++;
            }
            if(state.seasonTransitionCursor<state.bots.Length)return true;
            int humanBefore=state.human.currentMmr;
            CloseHuman(state.human,state,config);state.ledger.budget.softResetRemoval+=humanBefore-state.human.currentMmr;
            index?.Update(state.bots.Length,state.human.currentMmr);
            long ratingBefore=state.pendingSeasonArchive.ratingBefore;
            state.archives.Add(state.pendingSeasonArchive);
            state.pendingSeasonArchive=null;state.seasonTransitionCursor=-1;int season=state.currentSeasonNumber;
            int ranked=state.human.Ranked?1:0;foreach(var b in state.bots)if(b.Ranked)ranked++;
            state.ledger.seasonReceipts.Add(new SeasonTransitionReceipt{transitionId=id,sourceSeason=season,destinationSeason=season+1,contractVersion=GlobalSeasonCalendar.ContractVersion,
                boundaryUtc=state.seasonEndTimestamp,committedUtc=Math.Max(utc,state.seasonEndTimestamp),ratingBefore=ratingBefore,ratingAfter=PopulationRating(state),rankedHumansAndBots=ranked});
            while(state.ledger.seasonReceipts.Count>64)state.ledger.seasonReceipts.RemoveAt(0);
            state.lastProcessedSeasonNumber=season;state.currentSeasonNumber=season+1;state.highestAcceptedSeason=Math.Max(state.highestAcceptedSeason,season+1);
            state.seasonHistoryIncomplete=false;
            state.qualificationSeason=state.currentSeasonNumber;
            state.seasonStartTimestamp=GlobalSeasonCalendar.Start(season+1);state.seasonEndTimestamp=GlobalSeasonCalendar.End(season+1);return true;
        }
        // Pure harness helper. Production processes bounded chunks and commits
        // each completed archive before starting another missed season.
        public static bool AdvanceOne(LocalLeagueState state,LocalLeagueConfiguration config,long utc)=>utc>=state.seasonEndTimestamp && AdvanceChunk(state,config,utc,state.bots.Length);
        static void CloseHuman(LeagueCompetitor c,LocalLeagueState state,LocalLeagueConfiguration config)
        {
            int reset=c.Ranked?config.ResetMmr(c.currentMmr):c.currentMmr,peak=Math.Max(c.currentMmr,c.seasonPeakMmr);c.peakMmr=Math.Max(c.peakMmr,peak);
            c.history.Add(new RacingSeasonHistory{seasonNumber=state.currentSeasonNumber,startingMmr=c.seasonStartingMmr,endingMmr=c.currentMmr,peakMmr=peak,
                startingRank=c.Ranked?SeasonQualification.PendingLabel:MmrTierUtility.UnrankedName,endingRank=c.Ranked?SeasonQualification.DisplayRank(c,state.currentSeasonNumber):MmrTierUtility.UnrankedName,peakRank=SeasonQualification.IsOfficial(c,state.currentSeasonNumber)?MmrTierUtility.GetDisplayName(peak):c.Ranked?SeasonQualification.PendingLabel:MmrTierUtility.UnrankedName,
                qualificationKnown=true,officiallyQualified=SeasonQualification.IsOfficial(c,state.currentSeasonNumber),
                beyondPeakCounter=MmrTierUtility.GetBeyondCounter(peak),selectedTitleId=c.selectedTitleId,races=c.seasonRaces,wins=c.seasonWins,podiums=c.seasonPodiums,bestLap=c.seasonBestLap,
                nextStartingMmr=reset,seasonStartTimestamp=state.seasonStartTimestamp,seasonEndTimestamp=state.seasonEndTimestamp});
            if(c.seasonRaces>0 && SeasonQualification.IsOfficial(c,state.currentSeasonNumber))RacingTitleCatalog.Unlock(c,"season:"+state.currentSeasonNumber+":"+(int)MmrTierUtility.GetTier(peak));
            c.currentMmr=c.seasonStartingMmr=c.seasonPeakMmr=reset;c.qualifiedSeason=0;
            c.seasonRaces=c.seasonWins=c.seasonPodiums=0;c.seasonBestLap=0;if(c.Ranked)c.uncertainty=Math.Min(1,c.uncertainty+.12f);
        }
    }
}
