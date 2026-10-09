using System;
namespace AnimeHillRacing.Racing3D
{
    public static class LocalSeasonEngine
    {
        public static long Boundary(long ticks,int season)=>new DateTime(ticks,DateTimeKind.Utc).AddMonths(checked(season*3)).Ticks;
        public static long AcceptUtc(LocalLeagueState state,long ticks){long safe=Math.Max(state.installationTimestamp,Math.Max(ticks,state.lastKnownUtcTimestamp));state.lastKnownUtcTimestamp=safe;return safe;}
        public static bool AdvanceChunk(LocalLeagueState state,LocalLeagueConfiguration config,long utc,int maximumRecords,LeagueRankIndex index=null)
        {
            if(utc<state.seasonEndTimestamp)return false;
            if(state.lastProcessedSeasonNumber>=state.currentSeasonNumber)throw new InvalidOperationException("Duplicate season transition.");
            if(state.seasonTransitionCursor<0){state.seasonTransitionCursor=0;state.pendingSeasonArchive=new LeagueSeasonArchive{seasonNumber=state.currentSeasonNumber,population=state.bots.Length,startTimestamp=state.seasonStartTimestamp,endTimestamp=state.seasonEndTimestamp,records=new CompactBotSeason[state.bots.Length]};}
            int end=Math.Min(state.bots.Length,state.seasonTransitionCursor+Math.Max(1,maximumRecords));
            while(state.seasonTransitionCursor<end){int i=state.seasonTransitionCursor;var b=state.bots[i];int reset=config.ResetMmr(b.currentMmr),peak=Math.Max(b.currentMmr,b.seasonPeakMmr);
                state.pendingSeasonArchive.records[i]=new CompactBotSeason{startingMmr=b.seasonStartingMmr,endingMmr=b.currentMmr,peakMmr=peak,nextStartingMmr=reset,races=b.seasonRaces,wins=b.seasonWins,podiums=b.seasonPodiums,bestLap=b.seasonBestLap,selectedTitle=b.selectedTitle,selectedTitleSeason=b.selectedTitleSeason};
                b.peakMmr=Math.Max(b.peakMmr,peak);if(b.seasonRaces>0 && (b.personalitySeed&3)!=0){b.selectedTitle=(ushort)(32+(int)MmrTierUtility.GetTier(peak));b.selectedTitleSeason=state.currentSeasonNumber;}
                b.currentMmr=b.seasonStartingMmr=b.seasonPeakMmr=reset;b.seasonRaces=b.seasonWins=b.seasonPodiums=0;b.seasonBestLap=0;b.uncertainty=CompactLeagueBot.Pack01(Math.Min(1,b.Uncertainty+.12f));
                state.bots[i]=b;index?.Update(i,reset);state.seasonTransitionCursor++;
            }
            if(state.seasonTransitionCursor<state.bots.Length)return true;
            CloseHuman(state.human,state,config);index?.Update(state.bots.Length,state.human.currentMmr);state.archives.Add(state.pendingSeasonArchive);
            while(state.archives.Count>Math.Max(1,Math.Min(20,config.historyLimit)))state.archives.RemoveAt(0);
            state.pendingSeasonArchive=null;state.seasonTransitionCursor=-1;int season=state.currentSeasonNumber;state.lastProcessedSeasonNumber=season;state.currentSeasonNumber=season+1;
            state.seasonStartTimestamp=Boundary(state.installationTimestamp,season);state.seasonEndTimestamp=Boundary(state.installationTimestamp,season+1);return true;
        }
        // Pure harness helper. Production processes bounded chunks and commits
        // each completed archive before starting another missed season.
        public static bool AdvanceOne(LocalLeagueState state,LocalLeagueConfiguration config,long utc)=>utc>=state.seasonEndTimestamp && AdvanceChunk(state,config,utc,state.bots.Length);
        static void CloseHuman(LeagueCompetitor c,LocalLeagueState state,LocalLeagueConfiguration config)
        {
            int reset=config.ResetMmr(c.currentMmr),peak=Math.Max(c.currentMmr,c.seasonPeakMmr);c.peakMmr=Math.Max(c.peakMmr,peak);
            c.history.Add(new RacingSeasonHistory{seasonNumber=state.currentSeasonNumber,startingMmr=c.seasonStartingMmr,endingMmr=c.currentMmr,peakMmr=peak,
                startingRank=MmrTierUtility.GetDisplayName(c.seasonStartingMmr),endingRank=MmrTierUtility.GetDisplayName(c.currentMmr),peakRank=MmrTierUtility.GetDisplayName(peak),
                beyondPeakCounter=MmrTierUtility.GetBeyondCounter(peak),selectedTitleId=c.selectedTitleId,races=c.seasonRaces,wins=c.seasonWins,podiums=c.seasonPodiums,bestLap=c.seasonBestLap,
                nextStartingMmr=reset,seasonStartTimestamp=state.seasonStartTimestamp,seasonEndTimestamp=state.seasonEndTimestamp});
            while(c.history.Count>Math.Max(1,Math.Min(20,config.historyLimit)))c.history.RemoveAt(0);
            if(c.seasonRaces>0)RacingTitleCatalog.Unlock(c,"season:"+state.currentSeasonNumber+":"+(int)MmrTierUtility.GetTier(peak));
            RacingTitleCatalog.TrimSeasonTitles(c,Math.Max(1,state.currentSeasonNumber-config.historyLimit+1));c.currentMmr=c.seasonStartingMmr=c.seasonPeakMmr=reset;
            c.seasonRaces=c.seasonWins=c.seasonPodiums=0;c.seasonBestLap=0;c.uncertainty=Math.Min(1,c.uncertainty+.12f);
        }
    }
}
